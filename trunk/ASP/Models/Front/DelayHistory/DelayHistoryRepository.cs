// SỬA: DelayHistoryRepository.cs - THÊM CreateAsync
using ASP.Models.ASPModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public class DelayHistoryRepository : DelayHistoryRepositoryInterface
    {
        private readonly ASPDbContext _context;

        public DelayHistoryRepository(ASPDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DelayHistory>> GetByOrderIdAsync(Guid orderId)
        {
            return await _context.DelayHistory
                .Where(d => d.OId == orderId)
                .OrderByDescending(d => d.ChangeTime)
                .ToListAsync();
        }

     
        public async Task CreateAsync(DelayHistory delayHistory)
        {
            _context.DelayHistory.Add(delayHistory);
            await _context.SaveChangesAsync();
        }
    }
}