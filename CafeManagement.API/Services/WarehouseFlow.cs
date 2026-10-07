using System.Data;
using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Services;
public class WarehouseFlow(AppDbContext db)
{
    public static void Text(string? text, int max, string label, bool required = false)
    { if (text == null || text.Length > max || (required && string.IsNullOrWhiteSpace(text))) throw new InvalidOperationException($"{label} không hợp lệ (tối đa {max} ký tự)."); }
    public static void Method(string method) { if (method != "Cash" && method != "Transfer") throw new InvalidOperationException("Chọn tiền mặt hoặc chuyển khoản."); }
    public static void Amount(decimal amount) { if (amount <= 0 || amount > 1000000000000 || decimal.Round(amount, 2) != amount) throw new InvalidOperationException("Số tiền phải dương, tối đa 1.000 tỷ, không quá 2 số lẻ."); }
    public static void Quantity(double amount, bool zero = false) { if (!double.IsFinite(amount) || amount < 0 || (!zero && amount == 0) || amount > 1000000000) throw new InvalidOperationException("Số lượng không hợp lệ."); }
    public async Task<(decimal Factor, string Name)> Unit(Ingredient ingredient, int? unitId)
    {
        if (unitId is null or 0) return (1, ingredient.Unit);
        var unit = await db.IngredientUnits.FirstOrDefaultAsync(u => u.Id == unitId && u.IdIngredient == ingredient.Id) ?? throw new InvalidOperationException("Đơn vị quy đổi không thuộc nguyên liệu.");
        return (unit.Factor, unit.Name);
    }
    public async Task<int> Import(WarehouseImportWrite dto, string user)
    {
        Text(dto.RequestKey, 64, "Mã yêu cầu"); Text(dto.Note, 500, "Ghi chú"); Method(dto.PaymentMethod);
        if (dto.Items.Count == 0 || dto.Items.Count > 200) throw new InvalidOperationException("Phiếu nhập cần từ 1 đến 200 dòng.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        if (dto.RequestKey != "") { var existing = await db.ImportReceipts.FirstOrDefaultAsync(r => r.RequestKey == dto.RequestKey); if (existing != null) return existing.Id; }
        if (!await db.Suppliers.AnyAsync(s => s.Id == dto.IdSupplier && s.IsActive)) throw new InvalidOperationException("Nhà cung cấp không tồn tại hoặc đã ngừng giao dịch.");
        if (!await db.Accounts.AnyAsync(a => a.UserName == user)) throw new InvalidOperationException("Không tìm thấy tài khoản lập phiếu.");
        var prepared = new List<(WarehouseImportLine Input, Ingredient Ingredient, decimal Factor, string Unit, double Base, decimal Cost, decimal Total)>();
        foreach (var row in dto.Items)
        {
            Quantity(row.Quantity); if (row.Price < 0 || row.Price > 1000000000 || Math.Round(row.Price, 2) != row.Price) throw new InvalidOperationException("Đơn giá nhập không hợp lệ.");
            Text(row.LotCode, 100, "Mã lô"); if (row.ExpiryDate?.Date < DateTime.Today) throw new InvalidOperationException("Không nhập lô đã hết hạn.");
            var ingredient = await db.Ingredients.FindAsync(row.IdIngredient) ?? throw new InvalidOperationException("Nguyên liệu không tồn tại.");
            if (!ingredient.IsActive) throw new InvalidOperationException($"{ingredient.Name} đã ngừng sử dụng.");
            var unit = await Unit(ingredient, row.IdUnit); double quantity = row.Quantity * (double)unit.Factor; Quantity(quantity);
            decimal cost = Math.Round(row.Price / unit.Factor, 4); if (cost > 1000000000) throw new InvalidOperationException("Giá vốn đơn vị cơ sở quá lớn.");
            decimal total = Math.Round((decimal)row.Quantity * row.Price, 2, MidpointRounding.AwayFromZero);
            prepared.Add((row, ingredient, unit.Factor, unit.Name, quantity, cost, total));
        }
        decimal totalAmount = prepared.Sum(r => r.Total); if (totalAmount > 1000000000000 || dto.PaidAmount < 0 || dto.PaidAmount > totalAmount || decimal.Round(dto.PaidAmount, 2) != dto.PaidAmount) throw new InvalidOperationException("Tổng tiền hoặc số tiền đã trả không hợp lệ.");
        var receipt = new ImportReceipt { IdSupplier = dto.IdSupplier, UserName = user, Note = dto.Note.Trim(), RequestKey = dto.RequestKey, TotalAmount = totalAmount }; db.ImportReceipts.Add(receipt); await db.SaveChangesAsync();
        foreach (var row in prepared)
        {
            var lot = await new StockLots(db).Add(row.Ingredient, row.Base, row.Cost, "Import", $"Nhập PN{receipt.Id:D6}", user, receipt: receipt, code: row.Input.LotCode, expiry: row.Input.ExpiryDate);

            db.ImportDetails.Add(new ImportDetail { IdImportReceipt = receipt.Id, IdIngredient = row.Ingredient.Id, Count = row.Base, InputQuantity = row.Input.Quantity, UnitName = row.Unit, ConversionFactor = row.Factor, InputUnitPrice = row.Input.Price, IdLot = lot.Id });
        }
        if (dto.PaidAmount > 0) {
            var payment = new CashEntry { IdImportReceipt = receipt.Id, Amount = dto.PaidAmount, PaymentMethod = dto.PaymentMethod, Note = $"Thanh toán lúc nhập PN{receipt.Id:D6}", CreatedBy = user };
            await new ShiftFlow(db).Attach(payment, user); db.CashEntries.Add(payment);
        }
        await db.SaveChangesAsync(); await tx.CommitAsync(); return receipt.Id;
    }
    public async Task<int> Pay(int receiptId, SupplierPaymentWrite dto, string user)
    {
        Text(dto.RequestKey, 64, "Mã yêu cầu"); Text(dto.Note, 500, "Ghi chú"); Amount(dto.Amount); Method(dto.PaymentMethod);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        if (dto.RequestKey != "") { var existing = await db.CashEntries.FirstOrDefaultAsync(e => e.RequestKey == dto.RequestKey); if (existing != null) { if (existing.IdImportReceipt != receiptId) throw new InvalidOperationException("Mã thanh toán đã dùng cho phiếu khác."); return existing.Id; } }
        var receipt = await db.ImportReceipts.FindAsync(receiptId) ?? throw new InvalidOperationException("Phiếu nhập không tồn tại.");
        if (!receipt.HasPaymentTracking) throw new InvalidOperationException("Phiếu nhập cũ chưa ghi nhận số tiền đã trả, không tự suy đoán công nợ.");
        decimal paid = await db.CashEntries.Where(c => c.IdImportReceipt == receiptId).SumAsync(c => c.Direction == "Out" ? c.Amount : -c.Amount);
        if (dto.Amount > receipt.TotalAmount - paid) throw new InvalidOperationException("Số tiền vượt công nợ còn lại.");
        var cash = new CashEntry { IdImportReceipt = receiptId, Amount = dto.Amount, PaymentMethod = dto.PaymentMethod, Note = string.IsNullOrWhiteSpace(dto.Note) ? $"Thanh toán công nợ PN{receiptId:D6}" : dto.Note.Trim(), RequestKey = dto.RequestKey, CreatedBy = user };
        await new ShiftFlow(db).Attach(cash, user);
        db.CashEntries.Add(cash); await db.SaveChangesAsync(); await tx.CommitAsync(); return cash.Id;
    }
    public async Task<int> Operate(WarehouseOperationWrite dto, string user)
    {
        if (!new[] { "Export", "Disposal", "Count" }.Contains(dto.Kind)) throw new InvalidOperationException("Loại phiếu kho không hợp lệ.");
        Text(dto.RequestKey, 64, "Mã yêu cầu"); Text(dto.Note, 500, "Lý do", true);
        if (dto.Items.Count == 0 || dto.Items.Count > 200 || dto.Items.Select(i => i.IdIngredient).Distinct().Count() != dto.Items.Count) throw new InvalidOperationException("Phiếu cần từ 1 đến 200 nguyên liệu, không trùng dòng.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        if (dto.RequestKey != "") { var existing = await db.WarehouseDocuments.FirstOrDefaultAsync(d => d.RequestKey == dto.RequestKey); if (existing != null) return existing.Id; }
        var document = new WarehouseDocument { Kind = dto.Kind, Note = dto.Note.Trim(), RequestKey = dto.RequestKey, CreatedBy = user }; db.WarehouseDocuments.Add(document);
        foreach (var row in dto.Items)
        {
            Quantity(row.Quantity, dto.Kind == "Count"); var ingredient = await db.Ingredients.FindAsync(row.IdIngredient) ?? throw new InvalidOperationException("Nguyên liệu không tồn tại.");
            var unit = await Unit(ingredient, row.IdUnit); double quantity = row.Quantity * (double)unit.Factor; Quantity(quantity, dto.Kind == "Count");
            var before = ingredient.Quantity; var cost = ingredient.UnitCost;
            if (dto.Kind == "Count")
            {
                if (!row.ExpectedQuantity.HasValue || !double.IsFinite(row.ExpectedQuantity.Value) || Math.Abs(row.ExpectedQuantity.Value - before) > 0.000001) throw new InvalidOperationException($"Tồn {ingredient.Name} đã thay đổi sau khi mở kiểm kê. Hãy tải lại số liệu.");
                if (row.IdLot.HasValue) throw new InvalidOperationException("Kiểm kê dùng tổng tồn nguyên liệu.");
                var delta = quantity - before;
                if (delta > 0.0000001) await new StockLots(db).Add(ingredient, delta, cost, "Count", dto.Note, user, document);
                else if (delta < -0.0000001) await new StockLots(db).Consume(ingredient, -delta, "Count", dto.Note, user, document: document, allowExpired: true);
            }
            else await new StockLots(db).Consume(ingredient, quantity, dto.Kind, dto.Note, user, document: document, lotId: row.IdLot, allowExpired: dto.Kind == "Disposal");
            document.Lines.Add(new WarehouseDocumentLine { IdIngredient = ingredient.Id, BeforeQuantity = before, AfterQuantity = ingredient.Quantity, UnitCost = cost });
        }
        await db.SaveChangesAsync(); await tx.CommitAsync(); return document.Id;
    }
}


