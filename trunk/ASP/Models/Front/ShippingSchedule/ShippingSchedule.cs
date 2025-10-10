using System;
using System.ComponentModel.DataAnnotations;
using ASP.Models.Admin;

namespace ASP.Models.Front
{
    public partial class ShippingSchedule : BaseEntity
    {
        [Key]
        [MaxLength(5)]
        public string CustomerCode { get; set; } = null!;

        [Key] 
        [MaxLength(5)]
        public string TransCd { get; set; } = null!;

        [Key]
        public DayOfWeek Weekday { get; set; }

        public TimeOnly CutOffTime { get; set; }  

        [MaxLength(255)]
        public string Description { get; set; } = null!;

        [MaxLength(10)]
        public string CreatedBy { get; set; } = null!;
        [MaxLength(10)]
        public string? UpdatedBy { get; set; }
    }
}