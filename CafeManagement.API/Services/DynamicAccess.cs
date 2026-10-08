using CafeManagement.API.Data;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace CafeManagement.API.Services;
public record AccessRule(string Code, string Name, string Group, string? Requires = null);
public static class DynamicAccess
{
    public static readonly AccessRule[] Catalog = [
        new("AI_VIEW", "Dùng trợ lý tình hình quán (theo quyền dữ liệu)", "Trợ lý AI"),
        new("DASHBOARD_VIEW", "Xem tổng quan", "Tổng quan"),
        new("POS_VIEW", "Mở bán hàng / xem đơn đang phục vụ", "Bán hàng"),
        new("POS_ORDER", "Thêm món / tạo đơn mang về / gắn khách", "Bán hàng", "POS_VIEW"),
        new("POS_SEND", "Báo bếp", "Bán hàng", "POS_VIEW"),
        new("POS_CHECKOUT", "Thanh toán (tự báo bếp nếu còn món)", "Bán hàng", "POS_VIEW"),
        new("POS_DISCOUNT", "Giảm giá / đổi điểm khi thanh toán", "Bán hàng", "POS_CHECKOUT"),
        new("POS_CANCEL", "Hủy món / đóng đơn trống", "Bán hàng", "POS_VIEW"),
        new("POS_TRANSFER", "Chuyển / gộp / tách bàn", "Bán hàng", "POS_VIEW"),
        new("KITCHEN_VIEW", "Xem phiếu Bếp / Bar", "Bếp / Bar"),
        new("KITCHEN_UPDATE", "Bắt đầu làm / hoàn thành phiếu", "Bếp / Bar", "KITCHEN_VIEW"),
        new("MENU_VIEW", "Xem thực đơn / công thức", "Thực đơn"),
        new("MENU_CREATE", "Thêm món", "Thực đơn", "MENU_VIEW"),
        new("MENU_EDIT", "Sửa món / giá / công thức / trạng thái", "Thực đơn", "MENU_VIEW"),
        new("MENU_DELETE", "Xóa món", "Thực đơn", "MENU_VIEW"),
        new("MENU_IMPORT", "Nhập thực đơn từ Excel", "Thực đơn", "MENU_VIEW"),
        new("MENU_GROUPS", "Quản lý nhóm / loại món", "Thực đơn", "MENU_VIEW"),
        new("INVENTORY_VIEW", "Xem kho / nhà cung cấp / phiếu", "Kho hàng"),
        new("INVENTORY_EDIT", "Thêm / sửa nguyên liệu và nhóm", "Kho hàng", "INVENTORY_VIEW"),
        new("INVENTORY_SUPPLIER", "Thêm / sửa nhà cung cấp", "Kho hàng", "INVENTORY_VIEW"),
        new("INVENTORY_IMPORT", "Nhập hàng (có thể ghi chi ban đầu)", "Kho hàng", "INVENTORY_VIEW"),
        new("INVENTORY_EXPORT", "Xuất kho", "Kho hàng", "INVENTORY_VIEW"),
        new("INVENTORY_COUNT", "Kiểm kê", "Kho hàng", "INVENTORY_VIEW"),
        new("INVENTORY_DISPOSAL", "Hủy nguyên liệu", "Kho hàng", "INVENTORY_VIEW"),
        new("INVENTORY_PAY", "Trả nợ nhà cung cấp / ghi chi", "Kho hàng", "INVENTORY_VIEW"),
        new("TABLES_VIEW", "Xem phòng / bàn / nhật ký", "Phòng / Bàn"),
        new("TABLES_CREATE", "Thêm bàn / khu vực", "Phòng / Bàn", "TABLES_VIEW"),
        new("TABLES_EDIT", "Sửa bàn / khu vực / trạng thái", "Phòng / Bàn", "TABLES_VIEW"),
        new("TABLES_DELETE", "Xóa bàn / khu vực", "Phòng / Bàn", "TABLES_VIEW"),
        new("TABLES_IMPORT", "Nhập phòng / bàn từ Excel", "Phòng / Bàn", "TABLES_VIEW"),
        new("ORDERS_VIEW", "Xem lịch sử / chi tiết hóa đơn", "Đơn hàng"),
        new("ORDERS_REFUND", "Hủy hóa đơn đã trả / hoàn tiền", "Đơn hàng", "ORDERS_VIEW"),
        new("CUSTOMERS_VIEW", "Xem hồ sơ / lịch sử khách hàng", "Khách hàng"),
        new("CUSTOMERS_CREATE", "Thêm khách (kể cả tại bán hàng)", "Khách hàng"),
        new("CUSTOMERS_EDIT", "Sửa / ngừng khách hàng", "Khách hàng", "CUSTOMERS_VIEW"),
        new("CUSTOMERS_GROUPS", "Quản lý nhóm khách hàng", "Khách hàng", "CUSTOMERS_VIEW"),
        new("CUSTOMERS_IMPORT", "Nhập khách hàng từ Excel", "Khách hàng", "CUSTOMERS_VIEW"),
        new("STAFF_VIEW", "Xem nhân viên / tài khoản", "Nhân viên"),
        new("STAFF_CREATE", "Thêm hồ sơ nhân viên", "Nhân viên", "STAFF_VIEW"),
        new("STAFF_EDIT", "Sửa hồ sơ nhân viên", "Nhân viên", "STAFF_VIEW"),
        new("STAFF_STATUS", "Cho nghỉ / đi làm lại", "Nhân viên", "STAFF_VIEW"),
        new("STAFF_ACCOUNTS", "Cấp / sửa / khóa tài khoản trong phạm vi quyền", "Nhân viên", "STAFF_VIEW"),
        new("CASHBOOK_VIEW", "Xem sổ quỹ", "Sổ quỹ"),
        new("CASHBOOK_CREATE", "Lập phiếu thu / chi", "Sổ quỹ", "CASHBOOK_VIEW"),
        new("REPORT_VIEW", "Xem báo cáo", "Báo cáo"),
        new("SHIFT_SELF", "Xem / mở / chốt ca của mình", "Ca làm việc"),
        new("SHIFT_VIEW", "Xem ca và giao dịch của mọi nhân viên", "Ca làm việc"),
        new("SHIFT_CLOSE_OTHER", "Chốt ca của nhân viên", "Ca làm việc", "SHIFT_VIEW")
    ];
    public static HashSet<string> Normalize(IEnumerable<string> codes)
    {
        var result = codes.Where(c => Catalog.Any(x => x.Code == c)).ToHashSet(StringComparer.Ordinal);
        bool changed;
        do { changed = false; foreach (var rule in Catalog.Where(r => r.Requires != null)) if (result.Contains(rule.Code) && !result.Contains(rule.Requires!)) changed |= result.Remove(rule.Code); } while (changed);
        return result;
    }
    public static bool Has(ClaimsPrincipal user, string code) => user.IsInRole("Admin") || user.HasClaim("Permission", code);
    public static async Task<HashSet<string>> Effective(AppDbContext db, string username)
    {
        var account = await db.Accounts.AsNoTracking().Include(a => a.Role).SingleOrDefaultAsync(a => a.UserName == username);
        if (account == null || !account.IsActive) return [];
        if (account.Role?.Name == "Admin") return Catalog.Select(r => r.Code).ToHashSet();
        var codes = (await db.RolePermissions.Where(r => r.IdRole == account.IdRole).Select(r => r.Permission.Code).ToListAsync()).ToHashSet();
        foreach (var item in await db.AccountPermissions.Where(p => p.UserName == username).Select(p => new { p.Permission.Code, p.Allowed }).ToListAsync())
            if (item.Allowed) codes.Add(item.Code); else codes.Remove(item.Code);
        return Normalize(codes);
    }
    // Defaults are applied once during migration/seed, never on normal permission reads.
    public static async Task Seed(AppDbContext db)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var existing = await db.Permissions.ToListAsync();
        foreach (var rule in Catalog) { var row = existing.FirstOrDefault(x => x.Code == rule.Code); if (row == null) db.Permissions.Add(new() { Code = rule.Code, Name = rule.Name }); else row.Name = rule.Name; }
        await db.SaveChangesAsync();
        var all = await db.Permissions.ToListAsync();
        foreach (var role in await db.Roles.Where(r => !r.AccessConfigured).ToListAsync())
        {
            var defaults = role.Name == "Admin" ? Catalog.Select(r => r.Code) : role.Name == "Cashier" ? new[] { "POS_VIEW", "POS_ORDER", "POS_SEND", "POS_CHECKOUT", "POS_DISCOUNT", "POS_CANCEL", "POS_TRANSFER", "CUSTOMERS_CREATE", "SHIFT_SELF" } : role.Name == "Kitchen" ? new[] { "KITCHEN_VIEW", "KITCHEN_UPDATE" } : Array.Empty<string>();
            var assigned = await db.RolePermissions.Where(x => x.IdRole == role.Id).ToListAsync();
            db.RolePermissions.RemoveRange(assigned);
            foreach (var code in defaults) db.RolePermissions.Add(new() { IdRole = role.Id, IdPermission = all.Single(p => p.Code == code).Id });
            role.AccessConfigured = true;
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public static string[] Required(string controller, string action, string method) => (controller, action) switch
    {
        ("Access", _) => ["ADMIN_ONLY"],
        ("QrOrders", "List" or "Decision") => ["POS_ORDER"],
        ("Dashboard", _) => ["DASHBOARD_VIEW"], ("Report", _) => ["REPORT_VIEW"],
        ("Kitchen", "Pending") => ["KITCHEN_VIEW"], ("Kitchen", "Status") => ["KITCHEN_UPDATE"], ("Kitchen", "Send") => ["POS_SEND"],
        ("Bill", "GetActiveBillByTable") => ["POS_VIEW"], ("Bill", "AddItemToBill" or "UpdateGuests" or "SetCustomer") => ["POS_ORDER"],
        ("Bill", "CancelItem" or "CloseEmpty") => ["POS_CANCEL"], ("Bill", "Checkout") => ["POS_CHECKOUT"],
        ("Orders", "Takeaway") => ["POS_ORDER"], ("Orders", "Refund") => ["ORDERS_REFUND"], ("Orders", "Detail") => ["ORDERS_VIEW", "POS_VIEW"], ("Orders", "List") => ["ORDERS_VIEW"],
        ("Food", "GetAll") => ["MENU_VIEW", "POS_VIEW"], ("Food", "Create") => ["MENU_CREATE"], ("Food", "Update" or "Status" or "Favorite") => ["MENU_EDIT"], ("Food", "Delete") => ["MENU_DELETE"], ("Food", "Import") => ["MENU_IMPORT"],
        ("FoodCategory" or "ItemType", "GetAll") => ["MENU_VIEW", "POS_VIEW"], ("FoodCategory" or "ItemType", "Create" or "Rename" or "Delete") => ["MENU_GROUPS"],
        ("Inventory", "GetIngredients") => ["MENU_VIEW", "INVENTORY_VIEW"], ("Inventory", "GetRecipes") => ["MENU_VIEW"], ("Inventory", "SaveRecipe") => ["MENU_EDIT"],
        ("Inventory", "Movements") => ["INVENTORY_VIEW"], ("Inventory", "CreateIngredient" or "EditIngredient") => ["INVENTORY_EDIT"], ("Inventory", "ImportInventory") => ["INVENTORY_IMPORT"],
        ("Warehouse", "Ingredients" or "Groups" or "Suppliers" or "Receipts" or "Documents" or "Movements") => ["INVENTORY_VIEW"],
        ("Warehouse", "CreateIngredient" or "EditIngredient" or "StopIngredient" or "AddGroup" or "EditGroup" or "DeleteGroup" or "Excel") => ["INVENTORY_EDIT"],
        ("Warehouse", "AddSupplier" or "EditSupplier") => ["INVENTORY_SUPPLIER"], ("Warehouse", "Import") => ["INVENTORY_IMPORT"], ("Warehouse", "Pay") => ["INVENTORY_PAY"],
        ("Warehouse", "Document") => ["INVENTORY_EXPORT", "INVENTORY_COUNT", "INVENTORY_DISPOSAL"],
        ("TableFood", "GetAll" or "GetAreas") => ["TABLES_VIEW", "POS_VIEW", "ORDERS_VIEW"], ("TableFood", "Operations") => ["TABLES_VIEW"],
        ("TableFood", "Create" or "CreateArea") => ["TABLES_CREATE"], ("TableFood", "Edit" or "EditArea" or "UpdateStatus") => ["TABLES_EDIT"], ("TableFood", "Delete" or "DeleteArea") => ["TABLES_DELETE"], ("TableFood", "Transfer") => ["POS_TRANSFER"], ("TableFood", "Excel") => ["TABLES_IMPORT"],
        ("Customer", "Search") => ["CUSTOMERS_VIEW", "POS_VIEW"], ("Customer", "Groups") => ["CUSTOMERS_VIEW", "CUSTOMERS_CREATE", "POS_VIEW"], ("Customer", "List" or "Detail") => ["CUSTOMERS_VIEW"],
        ("Customer", "Create") => ["CUSTOMERS_CREATE"], ("Customer", "Update" or "Status") => ["CUSTOMERS_EDIT"], ("Customer", "CreateGroup" or "UpdateGroup" or "DeleteGroup") => ["CUSTOMERS_GROUPS"], ("Customer", "Import") => ["CUSTOMERS_IMPORT"],
        ("Employee", "List" or "Activities") => ["STAFF_VIEW"], ("Employee", "Create") => ["STAFF_CREATE"], ("Employee", "Update") => ["STAFF_EDIT"], ("Employee", "Status") => ["STAFF_STATUS"], ("Employee", "Login") => ["STAFF_ACCOUNTS"],
        ("Account", "Roles" or "GetAll") => ["STAFF_VIEW"], ("Account", "Create" or "Update" or "UpdateStatus" or "Delete") => ["STAFF_ACCOUNTS"],
        ("Cashbook", "Get") => ["CASHBOOK_VIEW"], ("Cashbook", "Create") => ["CASHBOOK_CREATE"],
        ("Shift", "Current") => ["POS_VIEW", "SHIFT_SELF", "SHIFT_VIEW"],
        ("Shift", "List" or "Detail") => ["SHIFT_SELF", "SHIFT_VIEW"],
        ("Shift", "Open") => ["SHIFT_SELF"], ("Shift", "Close") => ["SHIFT_SELF", "SHIFT_CLOSE_OTHER"],
        ("Assistant", "Capabilities" or "Ask") => ["AI_VIEW"],
        _ => [] // Unknown actions fail closed.
    };
}
