using CafeManagement.API.Data;
using CafeManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Services;
// Call within the same serializable transaction as checkout/refund.
public class CustomerLoyalty(AppDbContext db)
{
    void Entry(Customer c, Bill b, string kind, int delta, string actor)
    {
        c.Points = checked(c.Points + delta);
        db.CustomerPointEntries.Add(new() { IdCustomer = c.Id, IdBill = b.Id, Kind = kind, Delta = delta, Balance = c.Points, CreatedBy = actor });
    }
    public async Task Checkout(Bill bill, int? idCustomer, int redeem, string actor)
    {
        if (redeem < 0) throw new InvalidOperationException("Điểm đổi không được âm.");
        Customer? customer = null;
        if (idCustomer.HasValue) customer = await db.Customers.FromSqlInterpolated($"SELECT * FROM Customer WITH (UPDLOCK, ROWLOCK) WHERE Id = {idCustomer.Value} AND IsActive = 1").SingleOrDefaultAsync() ?? throw new InvalidOperationException("Khách hàng không tồn tại hoặc đã ngừng hoạt động.");
        if (redeem > 0 && customer == null) throw new InvalidOperationException("Chọn khách hàng trước khi đổi điểm.");
        if (customer != null && (redeem > Math.Max(0, customer.Points) || redeem > decimal.Floor(bill.TotalPrice / 100m))) throw new InvalidOperationException("Điểm đổi vượt số điểm hiện có hoặc số tiền hóa đơn.");
        bill.IdCustomer = customer?.Id;
        bill.CustomerNameSnapshot = customer?.Name ?? ""; bill.CustomerPhoneSnapshot = customer?.Phone ?? "";
        bill.PointsRedeemed = redeem; bill.PointDiscount = redeem * 100m;
        bill.TotalPrice -= bill.PointDiscount; bill.HasRecordedPayment = true;
        bill.PointsEarned = customer == null ? 0 : checked((int)decimal.Floor(bill.TotalPrice / 10000m));
        if (customer == null) return;
        if (redeem > 0) Entry(customer, bill, "Redeem", -redeem, actor);
        if (bill.PointsEarned > 0) Entry(customer, bill, "Earn", bill.PointsEarned, actor);
    }
    public async Task Refund(Bill bill, string actor)
    {
        if (!bill.IdCustomer.HasValue || (bill.PointsEarned == 0 && bill.PointsRedeemed == 0)) return;
        var c = await db.Customers.FromSqlInterpolated($"SELECT * FROM Customer WITH (UPDLOCK, ROWLOCK) WHERE Id = {bill.IdCustomer.Value}").SingleAsync();
        // Points earned on a refunded bill may already be spent. Retain a negative
        // balance rather than granting free points; future purchases offset it.
        Entry(c, bill, "Refund", bill.PointsRedeemed - bill.PointsEarned, actor);
    }
}
