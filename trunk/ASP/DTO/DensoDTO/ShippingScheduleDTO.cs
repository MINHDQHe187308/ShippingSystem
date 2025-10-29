using System;

namespace ASP.DTO.DensoDTO
{
    public class ShippingScheduleDTO
    {
        public string CustomerCode { get; set; } = null!;
        public string TransCd { get; set; } = null!;
        public DayOfWeek Weekday { get; set; }
        public TimeOnly CutOffTime { get; set; }
        public string Description { get; set; } = null!;
        public string CreatedBy { get; set; } = null!;
        public string? UpdatedBy { get; set; }
    }
}