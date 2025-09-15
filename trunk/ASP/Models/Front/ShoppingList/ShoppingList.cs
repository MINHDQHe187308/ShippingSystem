using System;
using System.Collections.Generic;

namespace ASP.Models.Front.ShoppingList
{
    public partial class ShoppingList
    {
        public Guid Uid { get; set; }
        public Guid Odid { get; set; }
        public long CollectionId { get; set; }
        public long PalletId { get; set; }
        public int PalletNo { get; set; }
        public short CollectionStatus { get; set; }
        public DateTime CollectedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
