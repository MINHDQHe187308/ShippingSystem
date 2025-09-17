using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ASP.Models.Admin;
namespace ASP.Models.Front.Order
{
    public partial class Order : BaseEntity
    {
        [Key]
        public Guid Uid { get; set; }
        public string PoorderId { get; set; } = null!;
        public DateTime ShipDate { get; set; }
        public string CustomerCode { get; set; } = null!;
        public string TransCd { get; set; } = null!;
        public short TransMethod { get; set; }
        public short ContSize { get; set; }
        public int TotalColumn { get; set; }
        public string PartList { get; set; } = null!;
        public int TotalPallet { get; set; }
        public short OrderStatus { get; set; }
        public DateTime OrderCreateDate { get; set; }
        public DateTime PlanAsyTime { get; set; }
        public DateTime PlanDocumentsTime { get; set; }
        public DateTime PlanDeliveryTime { get; set; }
        public DateTime? AcAsyTime { get; set; }
        public DateTime? AcDocumentsTime { get; set; }
        public DateTime? AcDeliveryTime { get; set; }
   
     
    }
}
