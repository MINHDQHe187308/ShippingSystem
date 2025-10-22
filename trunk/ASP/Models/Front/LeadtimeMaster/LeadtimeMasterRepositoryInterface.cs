using ASP.DTO.DensoDTO;
using ASP.Models.Front;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public interface LeadtimeMasterRepositoryInterface
    {
        Task UpsertLeadtimeAsync(LeadtimeMasterDTO leadtimemasterDTO);
        Task<LeadtimeMaster> GetLeadtimeByCustomerAndTransAsync(string customerCode, string transCd);
        Task<List<LeadtimeMaster>> GetAllLeadtimesByCustomer(string customerCode);
        Task<bool> CreateLeadtime(LeadtimeMaster leadtime);
        Task<(bool Success, string Message)> UpdateLeadtimeByKey(string customerCode, string transCd, LeadtimeMaster leadtime);
        Task<bool> RemoveLeadtimeByKey(string customerCode, string transCd);
        Task SaveChangesAsync();
    }
}
