using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ASP.Models.Admin; 
namespace ASP.Models.Front.OrderDetail
{
    public partial class OrderDetail : BaseEntity
    {
        [Key]
        public Guid Uid { get; set; }
        public Guid Oid { get; set; }
        public long ShippingId { get; set; }
        public long BookContDetailId { get; set; }
        public int ContNo { get; set; }
        public string PartNo { get; set; } = null!;
        public int PalletSize { get; set; }
        public int Quantity { get; set; }
        public int TotalPallet { get; set; }
        public string Warehouse { get; set; } = null!;
        public short BookContStatus { get; set; }
      
    }
}
