using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeManagement.API.Entities
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;
        public int Points { get; set; } = 0;
        [MaxLength(20)] public string Code { get; set; } = "";
        [MaxLength(150)] public string Email { get; set; } = "";
        [MaxLength(300)] public string Address { get; set; } = "";
        [MaxLength(500)] public string Note { get; set; } = "";
        [MaxLength(10)] public string Gender { get; set; } = "";
        public DateTime? Birthday { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public int? IdGroup { get; set; }
        public CustomerGroup? Group { get; set; }

        public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }

    public class Bill
    {
        public int? IdShift { get; set; }
        public CashierShift? Shift { get; set; }
        [Key]
        public int Id { get; set; }
        public DateTime DateCheckIn { get; set; } = DateTime.Now;
        public DateTime? DateCheckOut { get; set; }

        public int? IdTable { get; set; }
        [ForeignKey("IdTable")]
        public TableFood? TableFood { get; set; }

        public int Status { get; set; } = 0; // 0 serving, 1 paid, 2 closed/merged, 3 refunded
        [MaxLength(20)] public string OrderType { get; set; } = "DineIn";
        [MaxLength(100)] public string TableNameSnapshot { get; set; } = "";
        [MaxLength(100)] public string CreatedBy { get; set; } = "";
        [MaxLength(100)] public string PaidBy { get; set; } = "";
        [MaxLength(20)] public string PaymentMethod { get; set; } = "Unknown";
        [MaxLength(500)] public string Note { get; set; } = "";
        [MaxLength(64)] public string CreationKey { get; set; } = "";
        [MaxLength(500)] public string CancellationReason { get; set; } = "";
        [MaxLength(100)] public string CancelledBy { get; set; } = "";
        public DateTime? CancelledAt { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal RefundAmount { get; set; }
        [MaxLength(20)] public string RefundMethod { get; set; } = "";
        public int Discount { get; set; } = 0;
        public bool HasRecordedPayment { get; set; }
        public int PointsEarned { get; set; }
        public int PointsRedeemed { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal PointDiscount { get; set; }
        [MaxLength(150)] public string CustomerNameSnapshot { get; set; } = "";
        [MaxLength(20)] public string CustomerPhoneSnapshot { get; set; } = "";
        // Null means an old bill did not record the number of guests.
        public int? GuestCount { get; set; } = 1;

        // Bổ sung TotalPrice để lưu tổng tiền hóa đơn
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; } = 0;

        public int? IdCustomer { get; set; }
        [ForeignKey("IdCustomer")]
        public Customer? Customer { get; set; }

        public ICollection<BillInfo> BillInfos { get; set; } = new List<BillInfo>();
        public ICollection<KitchenOrder> KitchenOrders { get; set; } = new List<KitchenOrder>();
    }

    public class BillInfo
    {
        [Key]
        public int Id { get; set; }

        public int IdBill { get; set; }
        [ForeignKey("IdBill")]
        public Bill Bill { get; set; } = null!;

        public int IdFood { get; set; }
        [ForeignKey("IdFood")]
        public Food Food { get; set; } = null!;

        public int Count { get; set; }
        [MaxLength(150)] public string FoodNameSnapshot { get; set; } = "";
        public int SentCount { get; set; }
        public int? IdVariant { get; set; }
        [ForeignKey("IdVariant")]
        public FoodVariant? Variant { get; set; }
        public string OptionLabel { get; set; } = string.Empty;
        public string OptionsJson { get; set; } = "[]";
        public string IngredientsJson { get; set; } = "[]";

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        [Column("costPrice")]
        public double CostPrice { get; set; } // Khớp với cột costPrice FLOAT trong SQL
    }

    public class KitchenOrder
    {
        [Key]
        public int Id { get; set; }

        public int IdBill { get; set; }
        [ForeignKey("IdBill")]
        public Bill Bill { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";

        public ICollection<KitchenOrderDetail> KitchenOrderDetails { get; set; } = new List<KitchenOrderDetail>();
    }

    public class KitchenOrderDetail
    {
        [Key]
        public int Id { get; set; }

        public int IdKitchenOrder { get; set; }
        [ForeignKey("IdKitchenOrder")]
        public KitchenOrder KitchenOrder { get; set; } = null!;

        public int IdFood { get; set; }
        [ForeignKey("IdFood")]
        public Food Food { get; set; } = null!;

        public int Count { get; set; }
        [MaxLength(300)] public string CancellationNote { get; set; } = "";
        public int CancelledCount { get; set; }
        public int? IdBillInfo { get; set; }
        [ForeignKey("IdBillInfo")]
        public BillInfo? BillInfo { get; set; }
        public string OptionLabel { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
    }
}
