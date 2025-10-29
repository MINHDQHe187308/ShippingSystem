using ASP.DTO.DensoDTO;
using ASP.Models.Front;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public interface DelayHistoryRepositoryInterface
    {
        Task<IEnumerable<DelayHistory>> GetByOrderIdAsync(Guid orderId);
        Task CreateAsync(DelayHistory delayHistory);
    }
}