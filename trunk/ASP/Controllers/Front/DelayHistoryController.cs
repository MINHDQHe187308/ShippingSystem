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

        // SỬA: POST endpoint để save DelayHistory và update OrderStatus sang 4 (Delay) + SET STARTTIME=NOW, CHANGETIME=NOW
        [HttpPost("SaveDelay")]
        public async Task<IActionResult> SaveDelay([FromBody] DelaySaveDto delayDto)
        {
            try
            {
                if (!Guid.TryParse(delayDto.OrderId, out Guid orderId) || string.IsNullOrEmpty(delayDto.Reason))
                {
                    return BadRequest(new { success = false, message = "Invalid data" });
                }

                // THÊM: Parse StartTime và ChangeTime từ input (nhưng override bằng Now nếu cần)
                var now = DateTime.Now;
                var startTime = DateTime.Parse(delayDto.StartTime);  // Từ form, nhưng sẽ override
                var changeTime = DateTime.Parse(delayDto.ChangeTime);  // Từ form, nhưng sẽ override

                // Tạo DelayHistory record với StartTime=Now, ChangeTime=Now
                var delayHistory = new DelayHistory
                {
                    OId = orderId,
                    DelayType = delayDto.DelayType,
                    Reason = delayDto.Reason,
                    StartTime = now,  // SỬA: Set current time
                    ChangeTime = now,  // SỬA: Default current time
                    DelayTime = delayDto.DelayTime,
                    // Giả sử có CreatedDate = DateTime.Now
                    CreatedDate = now
                };

                await _delayHistoryRepository.CreateAsync(delayHistory);

                // Update OrderStatus sang 4 (Delay) + Lưu DelayStartTime và DelayTime vào Order (nếu cần cho query)
                await _orderRepository.UpdateOrderStatusToDelay(orderId, now, delayDto.DelayTime);  // SỬA: Pass delay info

                return Ok(new { success = true, message = "Delay saved and status updated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}