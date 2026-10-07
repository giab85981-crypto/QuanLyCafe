using Microsoft.EntityFrameworkCore;
using CafeManagement.API.Entities;

namespace CafeManagement.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<IngredientGroup> IngredientGroups { get; set; }
        public DbSet<QrOrderRequest> QrOrderRequests { get; set; }
        public DbSet<IngredientUnit> IngredientUnits { get; set; }
        public DbSet<StockLot> StockLots { get; set; }
        public DbSet<WarehouseDocument> WarehouseDocuments { get; set; }
        public DbSet<WarehouseDocumentLine> WarehouseDocumentLines { get; set; }
        public DbSet<CashEntry> CashEntries { get; set; }
        public DbSet<CashierShift> CashierShifts { get; set; }
        public DbSet<Area> Areas { get; set; } = null!;
        public DbSet<TableOperation> TableOperations { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AccountPermission> AccountPermissions { get; set; }
        public DbSet<AccessAudit> AccessAudits { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeActivity> EmployeeActivities { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<FoodCategory> FoodCategories { get; set; }
        public DbSet<Food> Foods { get; set; }
        public DbSet<ItemType> ItemTypes { get; set; }
        public DbSet<FoodVariant> FoodVariants { get; set; }
        public DbSet<FoodTopping> FoodToppings { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<TableFood> TableFoods { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<ImportReceipt> ImportReceipts { get; set; }
        public DbSet<ImportDetail> ImportDetails { get; set; }
        public DbSet<CustomerGroup> CustomerGroups { get; set; }
        public DbSet<CustomerPointEntry> CustomerPointEntries { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillInfo> BillInfos { get; set; }
        public DbSet<KitchenOrder> KitchenOrders { get; set; }
        public DbSet<KitchenOrderDetail> KitchenOrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<QrOrderRequest>().HasIndex(r => r.RequestKey).IsUnique();
            modelBuilder.Entity<QrOrderRequest>().HasIndex(r => new { r.Status, r.CreatedAt });
            modelBuilder.Entity<QrOrderRequest>().HasOne(r => r.Table).WithMany().HasForeignKey(r => r.IdTable).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<QrOrderRequest>().HasOne(r => r.Bill).WithMany().HasForeignKey(r => r.IdBill).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashierShift>().HasOne(s => s.Account).WithMany().HasForeignKey(s => s.UserName).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashierShift>().HasIndex(s => s.UserName).IsUnique().HasFilter("[ClosedAt] IS NULL");
            modelBuilder.Entity<CashierShift>().HasIndex(s => s.RequestKey).IsUnique();
            modelBuilder.Entity<CashEntry>().HasOne(c => c.Shift).WithMany().HasForeignKey(c => c.IdShift).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>().HasOne(b => b.Shift).WithMany().HasForeignKey(b => b.IdShift).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AccountPermission>().HasKey(x => new { x.UserName, x.IdPermission });
            modelBuilder.Entity<AccountPermission>().HasOne(x => x.Account).WithMany().HasForeignKey(x => x.UserName).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<AccountPermission>().HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.IdPermission).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasIndex(e => e.UserName).IsUnique().HasFilter("[UserName] IS NOT NULL");
            modelBuilder.Entity<Employee>().HasOne(e => e.Account).WithMany().HasForeignKey(e => e.UserName).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EmployeeActivity>().HasOne<Employee>().WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Customer>().HasIndex(c => c.Code).IsUnique().HasFilter("[Code] <> ''");
            modelBuilder.Entity<Customer>().HasIndex(c => c.Phone).IsUnique().HasFilter("[Phone] <> ''");
            modelBuilder.Entity<Customer>().HasOne(c => c.Group).WithMany().HasForeignKey(c => c.IdGroup).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CustomerGroup>().HasIndex(g => g.Name).IsUnique();
            modelBuilder.Entity<CustomerPointEntry>().HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.IdCustomer).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CustomerPointEntry>().HasOne(e => e.Bill).WithMany().HasForeignKey(e => e.IdBill).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CustomerPointEntry>().HasIndex(e => new { e.IdBill, e.Kind }).IsUnique();
            modelBuilder.Entity<TableOperation>().HasIndex(o => o.RequestKey).IsUnique();
            modelBuilder.Entity<IngredientGroup>().HasIndex(g => g.Name).IsUnique();
            modelBuilder.Entity<Ingredient>().HasIndex(i => i.Code).IsUnique().HasFilter("[Code] <> ''");
            modelBuilder.Entity<Ingredient>().HasOne(i => i.Group).WithMany().HasForeignKey(i => i.IdGroup).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<IngredientUnit>().HasOne(u => u.Ingredient).WithMany(i => i.Units).HasForeignKey(u => u.IdIngredient);
            modelBuilder.Entity<IngredientUnit>().HasIndex(u => new { u.IdIngredient, u.Name }).IsUnique();
            modelBuilder.Entity<StockLot>().HasOne(l => l.Ingredient).WithMany(i => i.Lots).HasForeignKey(l => l.IdIngredient).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockLot>().HasOne(l => l.ImportReceipt).WithMany().HasForeignKey(l => l.IdImportReceipt).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WarehouseDocumentLine>().HasOne(l => l.Document).WithMany(d => d.Lines).HasForeignKey(l => l.IdDocument);
            modelBuilder.Entity<WarehouseDocumentLine>().HasOne(l => l.Ingredient).WithMany().HasForeignKey(l => l.IdIngredient).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ImportDetail>().HasOne(d => d.Lot).WithMany().HasForeignKey(d => d.IdLot).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockMovement>().HasOne(m => m.Lot).WithMany().HasForeignKey(m => m.IdLot).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockMovement>().HasOne(m => m.Document).WithMany().HasForeignKey(m => m.IdDocument).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockMovement>().HasOne(m => m.ImportReceipt).WithMany().HasForeignKey(m => m.IdImportReceipt).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashEntry>().HasOne(c => c.ImportReceipt).WithMany().HasForeignKey(c => c.IdImportReceipt).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashEntry>().HasOne(c => c.Bill).WithMany().HasForeignKey(c => c.IdBill).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>().HasIndex(b => b.CreationKey).IsUnique().HasFilter("[CreationKey] <> ''");
            modelBuilder.Entity<Bill>().HasOne(b => b.TableFood).WithMany(t => t.Bills).HasForeignKey(b => b.IdTable).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashEntry>().HasIndex(c => new { c.IdBill, c.Direction }).IsUnique().HasFilter("[IdBill] IS NOT NULL");
            modelBuilder.Entity<CashEntry>().HasIndex(c => c.RequestKey).IsUnique().HasFilter("[RequestKey] <> ''");
            modelBuilder.Entity<ImportReceipt>().HasIndex(r => r.RequestKey).IsUnique().HasFilter("[RequestKey] <> ''");
            modelBuilder.Entity<WarehouseDocument>().HasIndex(d => d.RequestKey).IsUnique().HasFilter("[RequestKey] <> ''");
            modelBuilder.Entity<ItemType>().HasIndex(t => t.Name).IsUnique();
            modelBuilder.Entity<Food>().HasIndex(f => f.Code).IsUnique().HasFilter("[Code] <> ''");
            modelBuilder.Entity<FoodVariant>().HasOne(v => v.Food).WithMany(f => f.Variants).HasForeignKey(v => v.IdFood);
            modelBuilder.Entity<StockMovement>().HasOne(m => m.Ingredient).WithMany().HasForeignKey(m => m.IdIngredient).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FoodTopping>().HasKey(t => new { t.IdFood, t.IdTopping });
            modelBuilder.Entity<FoodTopping>().HasOne(t => t.Food).WithMany(f => f.AllowedToppings).HasForeignKey(t => t.IdFood).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FoodTopping>().HasOne(t => t.Topping).WithMany().HasForeignKey(t => t.IdTopping).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Recipe>().HasOne(r => r.Variant).WithMany(v => v.Recipes).HasForeignKey(r => r.IdVariant).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillInfo>().HasOne(b => b.Variant).WithMany().HasForeignKey(b => b.IdVariant).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<KitchenOrderDetail>().HasOne(d => d.BillInfo).WithMany().HasForeignKey(d => d.IdBillInfo).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockMovement>().HasOne(m => m.KitchenDetail).WithMany().HasForeignKey(m => m.IdKitchenDetail).OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ N-N cho RolePermission
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.IdRole, rp.IdPermission });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.IdRole);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.IdPermission);

            // Bắt buộc EF Core đặt tên tất cả các bảng trùng với tên Class Entity (dạng số ít)
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetTableName(entityType.ClrType.Name);
            }
        }
    }
}
