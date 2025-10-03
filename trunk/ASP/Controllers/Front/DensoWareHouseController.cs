using ASP.Models.Front;
using Microsoft.AspNetCore.Mvc;

namespace ASP.Controllers.Front
{
    public class DensoWareHouseController : Controller
    {
        private readonly OrderRepositoryInterface _orderRepository;
        private readonly CustomerRepositoryInterface _customerRepository;

        public DensoWareHouseController(OrderRepositoryInterface orderRepository, CustomerRepositoryInterface customerRepository)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
        }

        public async Task<IActionResult> Calendar()
        {
            var today = DateTime.Today;

            // Lấy orders từ DB, filter theo ShipDate = hôm nay
            var orders = await _orderRepository.GetOrdersByDate(today);

            // Lấy tất cả customers từ DB
            var allCustomers = await _customerRepository.GetAllCustomers();

            // Lọc chỉ những customers có order trong ngày hiện tại
            var customerCodesWithOrders = orders.Select(o => o.CustomerCode).Distinct().ToHashSet();
            var customers = allCustomers.Where(c => customerCodesWithOrders.Contains(c.CustomerCode)).ToList();

            // Map orders sang anonymous object - THÊM ACTUAL TIMES VÀ GIỮ STATUS LÀ NUMBER, THÊM TOTALPALLET
            var ordersForView = orders.Select(o => new
            {
                Resource = o.CustomerCode,
                PlanAsyTime = o.PlanAsyTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                PlanDeliveryTime = o.PlanDeliveryTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                AcAsyTime = o.AcAsyTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                AcDeliveryTime = o.AcDeliveryTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                Status = o.OrderStatus,  
                TotalPallet = o.TotalPallet
            }).ToArray();

            // Map customers sang anonymous object (giữ nguyên)
            var customersForView = customers.Select(c => new
            {
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName
            }).ToArray();

            // Trả về model chứa cả orders và customers
            var modelForView = new
            {
                Orders = ordersForView,
                Customers = customersForView
            };

            return View("~/Views/Front/DensoWareHouse/Calendar.cshtml", modelForView);
        }

        // Helper method để map OrderStatus (short) sang string (dựa trên enum hoặc logic của bạn)
        private string MapOrderStatusToString(short orderStatus)
        {
            return orderStatus switch
            {
                0 => "Planned",
                1 => "Pending",
                2 => "Shipped",
                3 => "Completed",
                _ => "Planned"
            };
        }
    }
}