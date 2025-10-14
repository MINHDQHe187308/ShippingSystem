using ASP.Models.Front;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ASP.DTO.DensoDTO;
namespace ASP.Controllers.Front
{
    [ApiController]
    [Route("api/[controller]")]
    public class DelayHistoryController : Controller
    {
        private readonly DelayHistoryRepositoryInterface _delayHistoryRepository;
        private readonly OrderRepositoryInterface _orderRepository; 

        public DelayHistoryController(DelayHistoryRepositoryInterface delayHistoryRepository, OrderRepositoryInterface orderRepository)
        {
            _delayHistoryRepository = delayHistoryRepository;
            _orderRepository = orderRepository;
        }

        [HttpGet("{orderId}")]
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

        // THÊM MỚI: POST endpoint để save DelayHistory và update OrderStatus sang 4 (Delay)
        [HttpPost("SaveDelay")]
        public async Task<IActionResult> SaveDelay([FromBody] DelaySaveDto delayDto)
        {
            try
            {
                if (!Guid.TryParse(delayDto.OrderId, out Guid orderId) || string.IsNullOrEmpty(delayDto.Reason))
                {
                    return BadRequest(new { success = false, message = "Invalid data" });
                }

                // Tạo DelayHistory record
                var delayHistory = new DelayHistory
                {
                    OId = orderId,
                    DelayType = delayDto.DelayType,
                    Reason = delayDto.Reason,
                    StartTime = DateTime.Parse(delayDto.StartTime),
                    ChangeTime = DateTime.Parse(delayDto.ChangeTime),
                    DelayTime = delayDto.DelayTime,
                    // Giả sử có CreatedDate = DateTime.Now
                    CreatedDate = DateTime.Now
                };

                await _delayHistoryRepository.CreateAsync(delayHistory);

                // Update OrderStatus sang 4 (Delay)
                await _orderRepository.UpdateOrderStatusToDelay(orderId);

                return Ok(new { success = true, message = "Delay saved and status updated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
  
  