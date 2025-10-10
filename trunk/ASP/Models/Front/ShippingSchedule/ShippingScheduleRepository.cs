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

        public ShippingScheduleRepository(ASPDbContext context, ILogger<ShippingScheduleRepository> logger)
        {
            _context = context;
            _logger = logger;
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
    }
}