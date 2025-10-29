using ASP.DTO.DensoDTO;
using ASP.Models.ASPModel;
using ASP.Models.Front;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public class LeadtimeMasterRepository : LeadtimeMasterRepositoryInterface
    {
        private readonly ASPDbContext _context;
        private readonly ILogger<LeadtimeMasterRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public LeadtimeMasterRepository(ASPDbContext context, ILogger<LeadtimeMasterRepository> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<List<LeadtimeMaster>> GetAllLeadtimesByCustomer(string customerCode)
        {
            return await _context.LeadtimeMasters
                .Where(l => l.CustomerCode == customerCode)
                .OrderBy(l => l.TransCd)
                .ToListAsync();
        }

        public async Task<bool> CreateLeadtime(LeadtimeMaster leadtime)
        {
            try
            {
                leadtime.CustomerCode = leadtime.CustomerCode.Trim();
                leadtime.TransCd = leadtime.TransCd.Trim();

                var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
                leadtime.CreateBy = currentUser;
                leadtime.CreatedDate = DateTime.Now;
                leadtime.UpdatedDate = DateTime.Now;

                _context.LeadtimeMasters.Add(leadtime);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating leadtime");
                return false;
            }
        }

        public async Task<(bool Success, string Message)> UpdateLeadtimeByKey(string customerCode, string transCd, LeadtimeMaster leadtime)
        {
            if (string.IsNullOrEmpty(customerCode) || string.IsNullOrEmpty(transCd))
            {
                return (false, "Khóa không hợp lệ");
            }

            try
            {
                var dbLeadtime = await _context.LeadtimeMasters.FirstOrDefaultAsync(l => l.CustomerCode == customerCode && l.TransCd == transCd);
                if (dbLeadtime == null)
                {
                    return (false, "Không tìm thấy leadtime");
                }

                dbLeadtime.CollectTimePerPallet = leadtime.CollectTimePerPallet;
                dbLeadtime.PrepareTimePerPallet = leadtime.PrepareTimePerPallet;
                dbLeadtime.LoadingTimePerColumn = leadtime.LoadingTimePerColumn;
                dbLeadtime.UpdateBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
                dbLeadtime.UpdatedDate = DateTime.Now;

                _context.LeadtimeMasters.Update(dbLeadtime);
                await _context.SaveChangesAsync();
                return (true, "Cập nhật thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating leadtime");
                return (false, $"Lỗi khi cập nhật: {ex.Message}");
            }
        }

        public async Task<bool> RemoveLeadtimeByKey(string customerCode, string transCd)
        {
            try
            {
                var dbLeadtime = await _context.LeadtimeMasters.FirstOrDefaultAsync(l => l.CustomerCode == customerCode && l.TransCd == transCd);
                if (dbLeadtime == null) return false;

                _context.LeadtimeMasters.Remove(dbLeadtime);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting leadtime");
                return false;
            }
        }
    }
}