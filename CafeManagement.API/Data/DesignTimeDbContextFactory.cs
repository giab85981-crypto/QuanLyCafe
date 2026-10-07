using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace CafeManagement.API.Data;
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("CAFE_DATABASE") ?? @"Server=(localdb)\MSSQLLocalDB;Database=QuanLyCafe_V2;Trusted_Connection=True;TrustServerCertificate=True;").Options);
}
