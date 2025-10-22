using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ASP.Models.Admin;

namespace ASP.Models.Front
{
    public partial class Customer : BaseEntity
    {
        [Key]
        [MaxLength(5)]
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string Descriptions { get; set; } = null!;
        public string CreateBy { get; set; } = null!;
        public string? UpdateBy { get; set; }

      
        public virtual ICollection<LeadtimeMaster> LeadtimeMasters { get; set; } = new List<LeadtimeMaster>();
        public virtual ICollection<ShippingSchedule> ShippingSchedules { get; set; } = new List<ShippingSchedule>();
    }
}