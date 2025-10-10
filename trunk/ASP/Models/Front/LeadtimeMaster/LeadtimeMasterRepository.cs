using ASP.DTO.DensoDTO;
using ASP.Models.ASPModel;
using ASP.Models.Front;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public class LeadtimeRepository : LeadtimeMasterRepositoryInterface
    {
        private readonly ASPDbContext _context;
        private readonly ILogger<LeadtimeRepository> _logger;

        public LeadtimeRepository(ASPDbContext context, ILogger<LeadtimeRepository> logger)
        {
            _context = context;
            _logger = logger;
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
    }
}