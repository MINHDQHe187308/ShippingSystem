using ASP.DTO.DensoDTO;
using ASP.Models.ASPModel;
using ASP.Models.Front;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public class ShippingScheduleRepository : ShippingScheduleRepositoryInterface
    {
        private readonly ASPDbContext _context;
        private readonly ILogger<ShippingScheduleRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ShippingScheduleRepository(ASPDbContext context, ILogger<ShippingScheduleRepository> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

       
        public async Task<List<ShippingSchedule>> GetAllShippingSchedulesByCustomer(string customerCode)
        {
            return await _context.ShippingSchedules
                .Where(s => s.CustomerCode == customerCode)
                .OrderBy(s => s.TransCd)
                .ThenBy(s => s.Weekday)
                .ToListAsync();
        }

        public async Task<bool> CreateShippingSchedule(ShippingSchedule schedule)
        {
            try
            {
                schedule.CustomerCode = schedule.CustomerCode.Trim();
                schedule.TransCd = schedule.TransCd.Trim();
                schedule.Description = schedule.Description?.Trim() ?? string.Empty;

                var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
                schedule.CreatedBy = currentUser;
                schedule.CreatedDate = DateTime.Now;
                schedule.UpdatedDate = DateTime.Now;

                _context.ShippingSchedules.Add(schedule);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shipping schedule");
                return false;
            }
        }

        public async Task<(bool Success, string Message)> UpdateShippingScheduleByKey(string customerCode, string transCd, DayOfWeek weekday, ShippingSchedule schedule)
        {
            if (string.IsNullOrEmpty(customerCode) || string.IsNullOrEmpty(transCd))
            {
                return (false, "Khóa không hợp lệ");
            }

            try
            {
                var dbSchedule = await _context.ShippingSchedules.FirstOrDefaultAsync(s => s.CustomerCode == customerCode && s.TransCd == transCd && s.Weekday == weekday);
                if (dbSchedule == null)
                {
                    return (false, "Không tìm thấy shipping schedule");
                }

                dbSchedule.CutOffTime = schedule.CutOffTime;
                dbSchedule.Description = schedule.Description?.Trim() ?? string.Empty;
                dbSchedule.UpdatedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
                dbSchedule.UpdatedDate = DateTime.Now;

                _context.ShippingSchedules.Update(dbSchedule);
                await _context.SaveChangesAsync();
                return (true, "Cập nhật thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shipping schedule");
                return (false, $"Lỗi khi cập nhật: {ex.Message}");
            }
        }

        public async Task<bool> RemoveShippingScheduleByKey(string customerCode, string transCd, DayOfWeek weekday)
        {
            try
            {
                var dbSchedule = await _context.ShippingSchedules.FirstOrDefaultAsync(s => s.CustomerCode == customerCode && s.TransCd == transCd && s.Weekday == weekday);
                if (dbSchedule == null) return false;

                _context.ShippingSchedules.Remove(dbSchedule);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shipping schedule");
                return false;
            }
        }
    }
}