using ASP.DTO.DensoDTO;
using ASP.Models.Front;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public interface ShippingScheduleRepositoryInterface
    {
        Task UpsertShippingScheduleAsync(ShippingScheduleDTO scheduleDto);
        Task<List<ShippingSchedule>> GetShippingSchedulesByCustomerAsync(string customerCode, string transCd = null);
        Task<List<ShippingSchedule>> GetAllShippingSchedulesByCustomer(string customerCode);
        Task<bool> CreateShippingSchedule(ShippingSchedule schedule);
        Task<(bool Success, string Message)> UpdateShippingScheduleByKey(string customerCode, string transCd, DayOfWeek weekday, ShippingSchedule schedule);
        Task<bool> RemoveShippingScheduleByKey(string customerCode, string transCd, DayOfWeek weekday);
        Task SaveChangesAsync();
    }
}