using System.Data;
using System.Text.Json;
using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Services;
public class TableFlow(AppDbContext db)
{
    public async Task<int> Transfer(TableTransferWrite dto, string user)
    {
        if (!new[] { "Move", "Merge", "Split" }.Contains(dto.Kind) || !Guid.TryParse(dto.RequestKey, out _) || dto.Note == null || dto.Note.Length > 500) throw new InvalidOperationException("Yêu cầu thao tác bàn không hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        var retry = await db.TableOperations.SingleOrDefaultAsync(o => o.RequestKey == dto.RequestKey);
        if (retry != null) { if (retry.SourceBillId != dto.SourceBillId || retry.TargetTableId != dto.TargetTableId || retry.Kind != dto.Kind) throw new InvalidOperationException("Mã yêu cầu đã dùng cho thao tác khác."); return retry.TargetBillId; }
        var source = await db.Bills.Include(b => b.TableFood).Include(b => b.BillInfos).ThenInclude(i => i.Food).AsSplitQuery().SingleOrDefaultAsync(b => b.Id == dto.SourceBillId && b.Status == 0) ?? throw new InvalidOperationException("Đơn nguồn đã thay đổi hoặc thanh toán. Hãy tải lại.");
        if (source.OrderType != "DineIn" || source.TableFood == null || !source.IdTable.HasValue) throw new InvalidOperationException("Đơn mang về không dùng chuyển bàn.");
        var targetTable = await db.TableFoods.Include(t => t.Area).SingleOrDefaultAsync(t => t.Id == dto.TargetTableId) ?? throw new InvalidOperationException("Không tìm thấy bàn đích.");
        if (source.IdTable == targetTable.Id || !targetTable.IsActive || targetTable.Area?.IsActive == false) throw new InvalidOperationException("Chọn bàn đích khác và đang hoạt động.");
        var target = await db.Bills.Include(b => b.BillInfos).SingleOrDefaultAsync(b => b.IdTable == targetTable.Id && b.Status == 0);
        if ((target?.Id ?? 0) != dto.ExpectedTargetBillId) throw new InvalidOperationException("Đơn bàn đích đã thay đổi. Hãy mở lại thao tác.");
        if (dto.Kind == "Merge" && target == null) throw new InvalidOperationException("Gộp cần chọn bàn đang có đơn; chuyển bàn dùng cho bàn trống.");
        if (target != null && (target.Discount != source.Discount || (source.IdCustomer.HasValue && target.IdCustomer.HasValue && source.IdCustomer != target.IdCustomer))) throw new InvalidOperationException("Hai đơn có khách hàng hoặc giảm giá khác nhau. Cần thống nhất trước khi gộp.");
        var active = source.BillInfos.Where(i => i.Count > 0).ToList();
        foreach (var line in active) line.UnitPrice ??= line.Food.Price;
        if (active.Count == 0) throw new InvalidOperationException("Đơn nguồn chưa có món.");
        if (dto.Items == null || dto.Items.Count == 0 || dto.Items.Count > 200 || dto.Items.Any(i => i == null) || dto.Items.Select(i => i.IdBillInfo).Distinct().Count() != dto.Items.Count) throw new InvalidOperationException("Chọn món và số lượng cần chuyển, không trùng dòng.");
        var selected = new List<(BillInfo Line, int Count)>();
        foreach (var item in dto.Items)
        {
            var line = active.FirstOrDefault(i => i.Id == item.IdBillInfo);
            if (line == null || item.Count <= 0 || item.Count > line.Count || item.ExpectedCount != line.Count || item.ExpectedSentCount != line.SentCount) throw new InvalidOperationException("Món hoặc trạng thái bếp đã thay đổi. Hãy mở lại thao tác.");
            selected.Add((line, item.Count));
        }
        bool whole = selected.Sum(s => s.Count) == active.Sum(i => i.Count);
        if (dto.Kind != "Split" && !whole) throw new InvalidOperationException("Chuyển/gộp cần chuyển toàn bộ đơn.");
        if (dto.Kind == "Split" && whole) throw new InvalidOperationException("Tách cần để lại ít nhất một phần ở bàn nguồn; dùng chuyển/gộp để chuyển toàn bộ.");
        if (dto.GuestCountToMove.HasValue && (!source.GuestCount.HasValue || dto.GuestCountToMove <= 0 || dto.GuestCountToMove >= source.GuestCount)) throw new InvalidOperationException("Số khách tách cần nhỏ hơn tổng khách nguồn.");
        var audit = new TableOperation { RequestKey = dto.RequestKey, Kind = dto.Kind, SourceBillId = source.Id, SourceTableId = source.IdTable.Value, TargetTableId = targetTable.Id, SourceName = source.TableFood.Name, TargetName = targetTable.Name, CreatedBy = user, Note = dto.Note.Trim(), ItemsJson = JsonSerializer.Serialize(selected.Select(s => new { s.Line.Id, Name = s.Line.Food.Name, s.Line.OptionLabel, s.Count, Price = s.Line.UnitPrice ?? s.Line.Food.Price })) };
        if (whole && target == null)
        {
            source.TableFood.Status = "Trống"; source.TableFood = targetTable; source.IdTable = targetTable.Id; source.TableNameSnapshot = targetTable.Name; target = source;
        }
        else
        {
            if (target == null) { target = new Bill { TableFood = targetTable, TableNameSnapshot = targetTable.Name, CreatedBy = user, Discount = source.Discount, IdCustomer = source.IdCustomer, GuestCount = 0 }; db.Bills.Add(target); await db.SaveChangesAsync(); }
            if (whole) target.GuestCount = source.GuestCount.HasValue && target.GuestCount.HasValue ? source.GuestCount + target.GuestCount : null;
            else if (dto.GuestCountToMove.HasValue) { source.GuestCount -= dto.GuestCountToMove; target.GuestCount = target.GuestCount.HasValue ? target.GuestCount + dto.GuestCountToMove : null; }
            else { source.GuestCount = null; target.GuestCount = null; }
            if (target.GuestCount > 1000) throw new InvalidOperationException("Số khách sau gộp vượt 1.000.");
            target.IdCustomer ??= source.IdCustomer;
            var targetOrders = new Dictionary<int, KitchenOrder>();
            foreach (var (line, count) in selected)
            {
                // Keep each price/recipe snapshot as a separate destination line.
                var moved = new BillInfo { Bill = target, IdFood = line.IdFood, FoodNameSnapshot = line.FoodNameSnapshot, IdVariant = line.IdVariant, Count = count, UnitPrice = line.UnitPrice ?? line.Food.Price, CostPrice = line.CostPrice, OptionsJson = line.OptionsJson, IngredientsJson = line.IngredientsJson, OptionLabel = line.OptionLabel };
                db.BillInfos.Add(moved);
                int sent = Math.Max(0, count - (line.Count - line.SentCount));
                moved.SentCount = sent;
                int remaining = sent;
                var details = await db.KitchenOrderDetails.Include(d => d.KitchenOrder).Where(d => d.IdBillInfo == line.Id && d.Count > d.CancelledCount).OrderByDescending(d => d.Id).ToListAsync();
                foreach (var detail in details)
                {
                    if (remaining == 0) break;
                    int take = Math.Min(remaining, detail.Count - detail.CancelledCount);
                    if (!targetOrders.TryGetValue(detail.IdKitchenOrder, out var order)) { order = new KitchenOrder { Bill = target, CreatedAt = detail.KitchenOrder.CreatedAt, Status = detail.KitchenOrder.Status }; db.KitchenOrders.Add(order); targetOrders.Add(detail.IdKitchenOrder, order); }
                    if (take == detail.Count && detail.CancelledCount == 0) { detail.BillInfo = moved; detail.KitchenOrder = order; }
                    else
                    {
                        var movedDetail = new KitchenOrderDetail { KitchenOrder = order, BillInfo = moved, IdFood = detail.IdFood, Count = take, Status = detail.Status, OptionLabel = detail.OptionLabel };
                        db.KitchenOrderDetails.Add(movedDetail);
                        var sources = await db.StockMovements.Where(m => m.IdKitchenDetail == detail.Id && m.Kind == "Kitchen").ToListAsync();
                        foreach (var stock in sources)
                        {
                            var portion = stock.Quantity * take / detail.Count;
                            stock.Quantity -= portion;
                            // Reassign the original consumption, without changing stock totals or lots.
                            db.StockMovements.Add(new StockMovement { IdIngredient = stock.IdIngredient, IdLot = stock.IdLot, KitchenDetail = movedDetail, Quantity = portion, UnitCost = stock.UnitCost, Kind = stock.Kind, CreatedAt = stock.CreatedAt, CreatedBy = stock.CreatedBy, Note = stock.Note });
                        }
                        detail.Count -= take;
                    }
                    remaining -= take;
                }
                if (remaining != 0) throw new InvalidOperationException("Đơn bếp cũ thiếu lịch sử món; chưa thể tách an toàn.");
                line.Count -= count; line.SentCount -= sent;
            }
            if (whole) { source.Status = 2; source.DateCheckOut = DateTime.Now; source.TableFood.Status = "Trống"; }
        }
        targetTable.Status = "Có người"; audit.TargetBillId = target.Id;
        db.TableOperations.Add(audit); await db.SaveChangesAsync(); await tx.CommitAsync(); return target.Id;
    }
}
