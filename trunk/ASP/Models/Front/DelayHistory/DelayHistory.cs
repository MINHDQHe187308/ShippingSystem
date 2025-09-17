using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ASP.Models.Admin;
namespace ASP.Models.Front.DelayHistory
{
    public partial class DelayHistory: BaseEntity
    {
        [Key]
        public Guid Uid { get; set; }
        public Guid Old { get; set; }
        public short DelayType { get; set; }
        public string Reason { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime ChangeTime { get; set; }
        public double DelayTime { get; set; }
       
    }
}
