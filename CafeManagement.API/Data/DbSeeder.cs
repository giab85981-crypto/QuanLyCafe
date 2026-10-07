using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            await context.Database.MigrateAsync();

            if (!await context.ItemTypes.AnyAsync())
            {
                var typeNames = await context.Foods.Where(f => f.ItemType != "")
                    .Select(f => f.ItemType).Distinct().ToListAsync();
                typeNames.AddRange(new[] { "Món chế biến", "Hàng hóa", "Dịch vụ" });
                context.ItemTypes.AddRange(typeNames.Distinct().Select(name => new ItemType { Name = name }));
                await context.SaveChangesAsync();
            }

            if (!await context.Permissions.AnyAsync())
            {
                var permissions = new List<Permission>
                {
                    new Permission { Code = "POS_ORDER", Name = "Gọi món tại bàn" },
                    new Permission { Code = "POS_CHECKOUT", Name = "Thanh toán hóa đơn" },
                    new Permission { Code = "KITCHEN_VIEW", Name = "Xem màn hình Bếp/Bar" },
                    new Permission { Code = "FOOD_MANAGE", Name = "Quản lý Thực đơn" },
                    new Permission { Code = "INVENTORY_MANAGE", Name = "Quản lý Kho & Nhập hàng" },
                    new Permission { Code = "USER_MANAGE", Name = "Quản lý Nhân viên & Phân quyền" },
                    new Permission { Code = "REPORT_VIEW", Name = "Xem báo cáo doanh thu" }
                };
                await context.Permissions.AddRangeAsync(permissions);
                await context.SaveChangesAsync();
            }

            if (!await context.Roles.AnyAsync())
            {
                var adminRole = new Role { Name = "Admin", Description = "Quản trị viên hệ thống" };
                var cashierRole = new Role { Name = "Cashier", Description = "Nhân viên thu ngân" };
                var kitchenRole = new Role { Name = "Kitchen", Description = "Nhân viên Bếp / Pha chế" };

                await context.Roles.AddRangeAsync(adminRole, cashierRole, kitchenRole);
                await context.SaveChangesAsync();

                var allPermissions = await context.Permissions.ToListAsync();
                foreach (var perm in allPermissions)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = adminRole.Id,
                        IdPermission = perm.Id
                    });
                }

                await context.SaveChangesAsync();
            }

            if (!await context.Accounts.AnyAsync())
            {
                var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");

                var adminAccount = new Account
                {
                    UserName = "admin",
                    DisplayName = "Quản Trị Viên",
                    PassWord = passwordHash,
                    IdRole = adminRole.Id
                };

                await context.Accounts.AddAsync(adminAccount);
                await context.SaveChangesAsync();
            }

            await CafeManagement.API.Services.DynamicAccess.Seed(context);

            var unlinked = await context.Accounts.Where(a => !context.Employees.Any(e => e.UserName == a.UserName)).ToListAsync();
            foreach (var account in unlinked) context.Employees.Add(new Employee { Name = account.DisplayName, UserName = account.UserName, IsActive = account.IsActive });
            if (unlinked.Count > 0) await context.SaveChangesAsync();

            if (!await context.FoodCategories.AnyAsync())
            {
                var catCoffee = new FoodCategory { Name = "Cà Phê" };
                var catTea = new FoodCategory { Name = "Trà & Trà Sữa" };

                await context.FoodCategories.AddRangeAsync(catCoffee, catTea);
                await context.SaveChangesAsync();

                var foods = new List<Food>
                {
                    new Food { Name = "Cà Phê Đen", Price = 25000, IdCategory = catCoffee.Id },
                    new Food { Name = "Cà Phê Sữa", Price = 29000, IdCategory = catCoffee.Id },
                    new Food { Name = "Bạc Xỉu", Price = 32000, IdCategory = catCoffee.Id },
                    new Food { Name = "Trà Đào Sả", Price = 35000, IdCategory = catTea.Id }
                };

                await context.Foods.AddRangeAsync(foods);
                await context.SaveChangesAsync();
            }

            if (!await context.TableFoods.AnyAsync())
            {
                var tables = new List<TableFood>();
                for (int i = 1; i <= 10; i++)
                {
                    tables.Add(new TableFood { Name = $"Bàn {i:D2}", Status = "Trống" });
                }
                await context.TableFoods.AddRangeAsync(tables);
                await context.SaveChangesAsync();
            }
        }
    }
}
