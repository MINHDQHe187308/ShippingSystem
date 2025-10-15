using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ASP.Models.Admin;
using ASP.Models.Front;

namespace ASP.Models.Front
{
    public partial class Order : BaseEntity
    {
        [Key]
        public Guid UId { get; set; }

        [MaxLength(50)]
        public string PCOrderId { get; set; } = null!;

        public DateTime ShipDate { get; set; }

        [MaxLength(5)]
        public string CustomerCode { get; set; } = null!;

        [MaxLength(3)]
        public string TransCd { get; set; } = null!;

        public short TransMethod { get; set; }

        public short ContSize { get; set; }

        public int TotalColumn { get; set; }

        [MaxLength(255)]
        public string PartList { get; set; } = null!;

        public int TotalPallet { get; set; }

        public short OrderStatus { get; set; }

        public DateTime OrderCreateDate { get; set; }

        public DateTime StartTime { get; set; } 
       
        public DateTime EndTime { get; set; }   

        public DateTime? AcStartTime { get; set; }  
       
        public DateTime? AcEndTime { get; set; }
      
        public DateTime? DelayStartTime { get; set; } 

        public double? DelayTime { get; set; }
        
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        public ICollection<DelayHistory> DelayHistories { get; set; } = new List<DelayHistory>();
    }
}