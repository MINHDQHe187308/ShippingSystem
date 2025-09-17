using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ASP.Models.Admin;
namespace ASP.Models.Front.ThreePointCheck
{
    public partial class ThreePointCheck  : BaseEntity  
    {
        [Key]
        public Guid Uid { get; set; }
        public Guid Spid { get; set; }
        public string PalletMarkQrContent { get; set; } = null!;
        public string PalletNoQrContent { get; set; } = null!;
        public string CasemarkQrContent { get; set; } = null!;
        public DateTime IssuedDate { get; set; }
    
    }
}
