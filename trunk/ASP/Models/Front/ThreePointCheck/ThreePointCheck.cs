using System;
using System.Collections.Generic;

namespace ASP.Models.Front.ThreePointCheck
{
    public partial class ThreePointCheck
    {
        public Guid Uid { get; set; }
        public Guid Spid { get; set; }
        public string PalletMarkQrContent { get; set; } = null!;
        public string PalletNoQrContent { get; set; } = null!;
        public string CasemarkQrContent { get; set; } = null!;
        public DateTime IssuedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
