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
        Task SaveChangesAsync();
    }
}