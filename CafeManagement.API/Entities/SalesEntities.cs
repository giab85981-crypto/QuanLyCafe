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

        public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }

    public class Bill
    {
        [Key]
        public int Id { get; set; }
        public DateTime DateCheckIn { get; set; } = DateTime.Now;
        public DateTime? DateCheckOut { get; set; }

        public int IdTable { get; set; }
        [ForeignKey("IdTable")]
        public TableFood TableFood { get; set; } = null!;

        public int Status { get; set; } = 0; // 0: Chưa thanh toán, 1: Đã thanh toán
        public int Discount { get; set; } = 0;

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
        public string Status { get; set; } = "Pending";
    }
}