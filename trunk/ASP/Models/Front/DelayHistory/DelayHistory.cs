using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ASP.Models.Admin;
namespace ASP.Models.Front
{  
    public partial class DelayHistory: BaseEntity
    {
        [Key]
        public Guid UId { get; set; }
        public Guid OId { get; set; }

        [ForeignKey(nameof(OId))]
        public Order Order { get; set; } = null!;
        public short DelayType { get; set; }
     
        [MaxLength(255)]
        public string Reason { get; set; } = null!;
      
        public DateTime StartTime { get; set; }
       
        public DateTime ChangeTime { get; set; }
      
        public double DelayTime { get; set; }
       
    }
}
