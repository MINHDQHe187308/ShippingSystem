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

        public async Task UpsertLeadtimeAsync(LeadtimeMasterDTO leadtimeDto)
        {
            if (leadtimeDto == null)
                throw new ArgumentException("LeadtimeDTO cannot be null");

            // Tìm existing dựa trên key (CustomerCode + TransCd)
            var existing = await _context.LeadtimeMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.CustomerCode == leadtimeDto.CustomerCode &&
                                         l.TransCd == leadtimeDto.TransCd);

            if (existing != null)
            {
                // Update
                existing.CollectTimePerPallet = leadtimeDto.CollectTimePerPallet;
                existing.PrepareTimePerPallet = leadtimeDto.PrepareTimePerPallet;
                existing.LoadingTimePerColumn = leadtimeDto.LoadingTimePerColumn;
                existing.UpdateBy = leadtimeDto.UpdateBy;
                _context.LeadtimeMasters.Update(existing);
                _logger.LogInformation("Updated Leadtime for {CustomerCode}-{TransCd}",
                    leadtimeDto.CustomerCode, leadtimeDto.TransCd);
            }
            else
            {
                // Add new
                var newLeadtime = new LeadtimeMaster
                {
                    CustomerCode = leadtimeDto.CustomerCode,
                    TransCd = leadtimeDto.TransCd,
                    CollectTimePerPallet = leadtimeDto.CollectTimePerPallet,
                    PrepareTimePerPallet = leadtimeDto.PrepareTimePerPallet,
                    LoadingTimePerColumn = leadtimeDto.LoadingTimePerColumn,
                    CreateBy = leadtimeDto.CreateBy,
                    UpdateBy = leadtimeDto.UpdateBy
                };
                await _context.LeadtimeMasters.AddAsync(newLeadtime);
                _logger.LogInformation("Added new Leadtime for {CustomerCode}-{TransCd}",
                    leadtimeDto.CustomerCode, leadtimeDto.TransCd);
            }
        }

        public async Task<LeadtimeMaster> GetLeadtimeByCustomerAndTransAsync(string customerCode, string transCd)
        {
            if (string.IsNullOrEmpty(customerCode) || string.IsNullOrEmpty(transCd))
                return null;

            return await _context.LeadtimeMasters
                .FirstOrDefaultAsync(l => l.CustomerCode == customerCode && l.TransCd == transCd);

        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
            _logger.LogDebug("Saved changes to Leadtimes");
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