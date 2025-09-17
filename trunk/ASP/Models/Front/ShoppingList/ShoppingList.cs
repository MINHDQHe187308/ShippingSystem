using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ASP.Models.Admin;
namespace ASP.Models.Front.ShoppingList
{
    public partial class ShoppingList : BaseEntity  
    {
        [Key]
        public Guid Uid { get; set; }
        public Guid Odid { get; set; }
        public long CollectionId { get; set; }
        public long PalletId { get; set; }
        public int PalletNo { get; set; }
        public short CollectionStatus { get; set; }
        public DateTime CollectedDate { get; set; }
    
    }
}
