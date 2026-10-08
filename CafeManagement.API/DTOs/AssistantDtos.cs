using System.ComponentModel.DataAnnotations;
namespace CafeManagement.API.DTOs;
public class AssistantQuestion {
    [Required, StringLength(500, MinimumLength = 3)] public string Question { get; set; } = "";
}
public record AssistantPlan(string Topic, string Period, bool Compare);
public record AssistantCard(string Label, string Value);
public record AssistantRow(string Name, string Value, string Detail);
public record AssistantSection(string Title, string Source, string Path, List<AssistantCard> Cards, List<AssistantRow> Rows, string Note);
public record AssistantAnswer(string Answer, string Mode, string? Warning, DateTimeOffset SnapshotAt, string Period, List<AssistantSection> Sections, string? Insight = null);
