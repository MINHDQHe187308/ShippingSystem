using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ASP.Models.Admin;
namespace ASP.Models.Front
{
    public partial class ShoppingList : BaseEntity
    {
        [Key]
        public Guid UId { get; set; }
        
        public Guid ODId { get; set; }

        [ForeignKey(nameof(ODId))]
        public OrderDetail OrderDetail { get; set; } = null!;

        public long CollectionId { get; set; }
        
        public long PalletId { get; set; }
       
        public int PalletNo { get; set; }
      
        public short CollectionStatus { get; set; }
      
        public DateTime? CollectedDate { get; set; }
      
        public ICollection<ThreePointCheck> ThreePointChecks { get; set; }
        = new List<ThreePointCheck>();

    }
}
