using System;
using System.Collections.Generic;

namespace ASP.Models.Front.DelayHistory
{
    public partial class DelayHistory
    {
        public Guid Uid { get; set; }
        public Guid Old { get; set; }
        public short DelayType { get; set; }
        public string Reason { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime ChangeTime { get; set; }
        public double DelayTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
