using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ASP.Models.Admin;

namespace ASP.Models.Front
{
    public partial class ThreePointCheck : BaseEntity
    {
        [Key]
        public Guid UId { get; set; }

        public Guid SPId { get; set; }

        [ForeignKey(nameof(SPId))]
        public ShoppingList ShoppingList { get; set; } = null!;  

        [MaxLength(125)]
        public string PalletMarkQrContent { get; set; } = null!;

        [MaxLength(50)]
        public string PalletNoQrContent { get; set; } = null!;

        [MaxLength(255)]
        public string CasemarkQrContent { get; set; } = null!;

        public DateTime IssuedDate { get; set; }
    }
}