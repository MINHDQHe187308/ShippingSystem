using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ASP.Models.Admin;

namespace ASP.Models.Front
{
    public partial class OrderDetail : BaseEntity
    {
        [Key]
        public Guid UId { get; set; }

        public Guid OId { get; set; } 

        [ForeignKey(nameof(OId))]
        public Order Order { get; set; } = null!;

        public long ShippingId { get; set; }

        public long BookContDetailId { get; set; }  

        public int ContNo { get; set; }  

        [MaxLength(15)]
        public string PartNo { get; set; } = null!;  

        public int PalletSize { get; set; } 

        public int Quantity { get; set; }  

        public int TotalPallet { get; set; }  

        [MaxLength(10)]
        public string Warehouse { get; set; } = null!; 

        public short BookContStatus { get; set; }  

       
        public short Status { get; set; } 

        public ICollection<ShoppingList> ShoppingLists { get; set; } = new List<ShoppingList>();  
    }
}