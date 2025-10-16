using ASP.DTO.DensoDTO;
using ASP.Models.ASPModel;
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

        // SỬA: POST endpoint để save DelayHistory và update OrderStatus sang 4 (Delay) + SỬA: Sử dụng input StartTime và ChangeTime (không override)
        [HttpPost("SaveDelay")]
        public async Task<IActionResult> SaveDelay([FromBody] DelaySaveDto delayDto)
        {
            try
            {
                if (!Guid.TryParse(delayDto.OrderId, out Guid orderId) || string.IsNullOrEmpty(delayDto.Reason))
                {
                    return BadRequest(new { success = false, message = "Invalid data" });
                }

                // SỬA: Parse StartTime và ChangeTime từ input (không override bằng now nữa)
                var startTime = DateTime.Parse(delayDto.StartTime);
                var changeTime = DateTime.Parse(delayDto.ChangeTime);

                // Tạo DelayHistory record với StartTime và ChangeTime từ input
                var delayHistory = new DelayHistory
                {
                    OId = orderId,
                    DelayType = delayDto.DelayType,
                    Reason = delayDto.Reason,
                    StartTime = startTime,  // SỬA: Từ input (có thể future)
                    ChangeTime = changeTime,  // SỬA: Từ input (default now nếu không set)
                    DelayTime = delayDto.DelayTime,
                    // Giả sử có CreatedDate = DateTime.Now
                    CreatedDate = DateTime.Now  // Giữ CreatedDate = now
                };

                await _delayHistoryRepository.CreateAsync(delayHistory);

                // Update OrderStatus sang 4 (Delay) + Lưu DelayStartTime và DelayTime vào Order + SỬA: Pass input delayStartTime
                await _orderRepository.UpdateOrderStatusToDelay(orderId, startTime, delayDto.DelayTime);  // SỬA: Pass startTime từ input

                return Ok(new { success = true, message = "Delay saved and status updated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}