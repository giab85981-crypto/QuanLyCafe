using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace CafeManagement.API.Data;

// Resolve a fresh pipe at startup: LocalDB changes its pipe when it restarts.
// Used only for Windows Development; no database is created, moved or shared.
public static class DevelopmentLocalDb
{
    public static async Task<string> ResolveAsync(string connectionString)
    {
        var connection = new SqlConnectionStringBuilder(connectionString);
        const string prefix = "(localdb)\\";
        if (!OperatingSystem.IsWindows() || !connection.DataSource.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var instance = connection.DataSource[prefix.Length..];
        // Visual Studio and SqlLocalDB can observe stale registration while SQL is
        // already running. Verify the instance before attempting another startup.
        var pipe = RunningInstancePipe(instance);
        if (!await IsExpectedInstanceAsync(connection, instance, pipe))
            pipe = await LoggedInstancePipeAsync(connection, instance);
        if (pipe != null)
        {
            connection.DataSource = pipe;
            return connection.ConnectionString;
        }
        var toolRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft SQL Server");
        var executable = Directory.Exists(toolRoot) ? Directory.GetDirectories(toolRoot)
            .Where(path => int.TryParse(Path.GetFileName(path), out _))
            .OrderByDescending(path => int.Parse(Path.GetFileName(path)))
            .Select(path => Path.Combine(path, "Tools", "Binn", "SqlLocalDB.exe"))
            .FirstOrDefault(File.Exists) : null;
        executable ??= "SqlLocalDB.exe";

        var output = "";
        string? startupError = null;
        try { output = await RunAsync(executable, "info", instance); }
        catch (InvalidOperationException ex) { startupError = ex.Message; }
        pipe = FindPipe(output) ?? RunningInstancePipe(instance);
        if (!await IsExpectedInstanceAsync(connection, instance, pipe)) pipe = null;
        if (pipe == null)
        {
            try { await RunAsync(executable, "start", instance); }
            catch (InvalidOperationException ex) { startupError = ex.Message; }
            // SQL may be ready even if the CLI failed to update its registration.
            for (var attempt = 0; attempt < 3 && pipe == null; attempt++)
            {
                pipe = await LoggedInstancePipeAsync(connection, instance);
                if (pipe != null) break;
                try { output = await RunAsync(executable, "info", instance); }
                catch (InvalidOperationException ex) { startupError ??= ex.Message; }
                var candidate = FindPipe(output) ?? RunningInstancePipe(instance);
                if (await IsExpectedInstanceAsync(connection, instance, candidate)) pipe = candidate;
                if (pipe == null && attempt < 2) await Task.Delay(250);
            }
        }
        if (pipe == null)
            throw new InvalidOperationException($"Không kết nối được LocalDB '{instance}'. Chi tiết khởi động: {startupError ?? "không nhận được địa chỉ kết nối"}. Đầu ra SqlLocalDB: {output.Trim()}");
        connection.DataSource = pipe;
        return connection.ConnectionString;
    }

    public static string? FindPipe(string output)
    {
        // CLI output encoding can differ when launched under the VS debugger.
        foreach (var line in output.Replace("\0", "").Split('\n'))
        {
            var index = line.IndexOf(@"\\.\pipe\LOCALDB#", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var pipe = line[index..].Trim();
                if (pipe.EndsWith(@"\tsql\query", StringComparison.OrdinalIgnoreCase)) return "np:" + pipe;
            }
        }
        return null;
    }

    private static string InstanceLogPath(string instance) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "Microsoft SQL Server Local DB", "Instances", instance, "error.log");

    public static string? FindPipeInLog(string output)
    {
        const string marker = @"\\.\pipe\LOCALDB#";
        const string suffix = @"\tsql\query";
        var start = output.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;
        var end = output.IndexOf(suffix, start, StringComparison.OrdinalIgnoreCase);
        if (end < 0) return null;
        var candidate = output[start..(end + suffix.Length)];
        // Accept only a LocalDB pipe, never an arbitrary location from log text.
        var name = candidate[marker.Length..^suffix.Length];
        return name.Length > 0 && name.All(char.IsAsciiLetterOrDigit) ? "np:" + candidate : null;
    }

    private static async Task<string?> LoggedInstancePipeAsync(SqlConnectionStringBuilder connection, string instance)
    {
        try
        {
            using var stream = new FileStream(InstanceLogPath(instance), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream); // Detect UTF-16 SQL logs from their BOM.
            var output = await reader.ReadToEndAsync();
            var match = System.Text.RegularExpressions.Regex.Matches(output, @"Server process ID is (\d+)\.").LastOrDefault();
            if (match == null || !int.TryParse(match.Groups[1].Value, out var pid)) return null;
            using var process = Process.GetProcessById(pid);
            if (process.HasExited || !process.ProcessName.Equals("sqlservr", StringComparison.OrdinalIgnoreCase)) return null;
            var pipe = FindPipeInLog(output[match.Index..]);
            return await IsExpectedInstanceAsync(connection, instance, pipe) ? pipe : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.Security.SecurityException)
        { return null; }
    }

    private static async Task<bool> IsExpectedInstanceAsync(SqlConnectionStringBuilder connection, string instance, string? pipe)
    {
        if (pipe == null) return false;
        try
        {
            // Probe master so a new application database can still be migrated.
            var probe = new SqlConnectionStringBuilder(connection.ConnectionString) { DataSource = pipe, InitialCatalog = "master", ConnectTimeout = 2, Pooling = false };
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            await using var sql = new SqlConnection(probe.ConnectionString);
            await sql.OpenAsync(timeout.Token);
            await using var command = sql.CreateCommand();
            command.CommandText = "SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('ErrorLogFileName'))";
            command.CommandTimeout = 2;
            var actual = await command.ExecuteScalarAsync(timeout.Token) as string;
            return actual != null && string.Equals(Path.GetFullPath(actual), Path.GetFullPath(InstanceLogPath(instance)), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is SqlException or OperationCanceledException or ArgumentException or InvalidOperationException)
        { return false; }
    }

    private static string? RunningInstancePipe(string instance)
    {
        if (!OperatingSystem.IsWindows()) return null;
        var expectedDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Microsoft SQL Server Local DB", "Instances", instance);
        using var instances = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Microsoft SQL Server\UserInstances");
        if (instances == null) return null;
        foreach (var name in instances.GetSubKeyNames())
        {
            using var entry = instances.OpenSubKey(name);
            if (entry?.GetValue("DataDirectory") is not string directory
                || !string.Equals(Path.GetFullPath(directory).TrimEnd('\\'), expectedDirectory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)
                || entry.GetValue("InstanceName") is not string server
                || !server.StartsWith("LOCALDB#", StringComparison.OrdinalIgnoreCase)
                || entry.GetValue("InstanceProcessId") is not int pid) continue;
            try
            {
                using var process = Process.GetProcessById(pid);
                if (!process.HasExited && process.ProcessName.Equals("sqlservr", StringComparison.OrdinalIgnoreCase))
                    return FindPipe($@"\\.\pipe\{server}\tsql\query");
            }
            catch (ArgumentException) { /* Instance has exited; ask SqlLocalDB to start it. */ }
            catch (InvalidOperationException) { /* Instance stopped while checking. */ }
        }
        return null;
    }

    private static async Task<string> RunAsync(string executable, string command, string instance)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            }
        };
        process.StartInfo.ArgumentList.Add(command);
        process.StartInfo.ArgumentList.Add(instance);
        process.Start();
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill();
            throw new TimeoutException("LocalDB không phản hồi trong 30 giây.");
        }
        var error = await stderr;
        if (process.ExitCode != 0) throw new InvalidOperationException($"SqlLocalDB {command} thất bại: {error.Trim()}");
        return await stdout;
    }
}
