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

        public async Task UpsertShippingScheduleAsync(ShippingScheduleDTO scheduleDto)
        {
            if (scheduleDto == null)
                throw new ArgumentException("ShippingScheduleDTO cannot be null");

            // Tìm existing dựa trên composite key (CustomerCode + TransCd + Weekday)
            var existing = await _context.ShippingSchedules
                .AsNoTracking()  // Không track để tránh conflict
                .FirstOrDefaultAsync(s => s.CustomerCode == scheduleDto.CustomerCode &&
                                         s.TransCd == scheduleDto.TransCd &&
                                         s.Weekday == scheduleDto.Weekday);

            if (existing != null)
            {
                // Update existing
                existing.CutOffTime = scheduleDto.CutOffTime;
                existing.Description = scheduleDto.Description;
                existing.UpdatedBy = scheduleDto.UpdatedBy;
                _context.ShippingSchedules.Update(existing);
                _logger.LogInformation("Updated ShippingSchedule for {CustomerCode}-{TransCd}-{Weekday}",
                    scheduleDto.CustomerCode, scheduleDto.TransCd, scheduleDto.Weekday);
            }
            else
            {
                // Add new
                var newSchedule = new ShippingSchedule
                {
                    CustomerCode = scheduleDto.CustomerCode,
                    TransCd = scheduleDto.TransCd,
                    Weekday = scheduleDto.Weekday,
                    CutOffTime = scheduleDto.CutOffTime,
                    Description = scheduleDto.Description,
                    CreatedBy = scheduleDto.CreatedBy,
                    UpdatedBy = scheduleDto.UpdatedBy
                };
                await _context.ShippingSchedules.AddAsync(newSchedule);
                _logger.LogInformation("Added new ShippingSchedule for {CustomerCode}-{TransCd}-{Weekday}",
                    scheduleDto.CustomerCode, scheduleDto.TransCd, scheduleDto.Weekday);
            }
        }

        public async Task<List<ShippingSchedule>> GetShippingSchedulesByCustomerAsync(string customerCode, string transCd = null)
        {
            if (string.IsNullOrEmpty(customerCode))
                throw new ArgumentException("CustomerCode cannot be null or empty");

            var query = _context.ShippingSchedules
                .Where(s => s.CustomerCode == customerCode);

            if (!string.IsNullOrEmpty(transCd))
            {
                query = query.Where(s => s.TransCd == transCd);
            }

            return await query.ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
            _logger.LogDebug("Saved changes to ShippingSchedules");
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