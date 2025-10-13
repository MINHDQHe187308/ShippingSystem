using ASP.Models.Front;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASP.Controllers.Front
{
    [ApiController]  
    [Route("api/[controller]")] 
    public class DelayHistoryController : ControllerBase  
    {
        private readonly DelayHistoryRepositoryInterface _delayHistoryRepository;

        public DelayHistoryController(DelayHistoryRepositoryInterface delayHistoryRepository)
        {
            _delayHistoryRepository = delayHistoryRepository;
        }

        [HttpGet("{orderId}")]  // Giữ nguyên, giờ khớp với route
        public async Task<IActionResult> GetByOrderId(Guid orderId)
        {
            try
            {
                var histories = await _delayHistoryRepository.GetByOrderIdAsync(orderId);
                return Ok(histories);  // Trả JSON array
            }
            catch (Exception ex)
            {
                // Log error nếu có ILogger
                return BadRequest("Error retrieving delay history: " + ex.Message);
            }
        }
    }
}