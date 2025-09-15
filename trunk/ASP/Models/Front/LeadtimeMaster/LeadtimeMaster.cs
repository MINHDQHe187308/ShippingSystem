using System;
using System.Collections.Generic;

namespace ASP.Models.Front.LeadtimeMaster
{
    public partial class LeadtimeMaster
    {
        public string CustomerCode { get; set; } = null!;
        public string TransCd { get; set; } = null!;
        public double CollectTimePerPallet { get; set; }
        public double PrepareTimePerPallet { get; set; }
        public double LoadingTimePerColumn { get; set; }
        public string CreateBy { get; set; } = null!;
        public string UpdateBy { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
