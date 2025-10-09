using ASP.DTO.DensoDTO;
using ASP.Models.Front;
using ASP.Models.Front;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.Controllers.Front
{
    public class DensoWareHouseController : Controller
    {
        private readonly OrderRepositoryInterface _orderRepository;
        private readonly CustomerRepositoryInterface _customerRepository;
        private readonly OrderDetailRepositoryInterface _orderDetailRepository;

        public DensoWareHouseController(
            OrderRepositoryInterface orderRepository,
            CustomerRepositoryInterface customerRepository,
            OrderDetailRepositoryInterface orderDetailRepository)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _orderDetailRepository = orderDetailRepository;
        }

        public async Task<IActionResult> Calendar()
        {
            var today = DateTime.Today;

            // Lấy orders từ DB, filter theo ShipDate = hôm nay
            var orders = await _orderRepository.GetOrdersByDate(today);

            Console.WriteLine($"Loaded {orders.Count} orders for {today}. First UId: {orders.FirstOrDefault()?.UId}");
            if (orders.Any())
            {
                Console.WriteLine($"First Order Details: UId={orders.First().UId}, Status={orders.First().OrderStatus}, PlanAsy={orders.First().PlanAsyTime}");
            }
            // Lấy tất cả customers từ DB
            var allCustomers = await _customerRepository.GetAllCustomers();

            // Lọc chỉ những customers có order trong ngày hiện tại
            var customerCodesWithOrders = orders.Select(o => o.CustomerCode).Distinct().ToHashSet();
            var customers = allCustomers.Where(c => customerCodesWithOrders.Contains(c.CustomerCode)).ToList();

            // Map orders sang anonymous object - THÊM UId VÀ GIỮ STATUS LÀ NUMBER, THÊM TOTALPALLET VÀ CÁC TRƯỜNG KHÁC
            var ordersForView = orders.Select(o => new
            {
                UId = o.UId,  // THÊM: Để sử dụng trong JS eventClick
                Resource = o.CustomerCode,
                ShipDate = o.ShipDate.ToString("yyyy-MM-dd"),
                PlanAsyTime = o.PlanAsyTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                PlanDeliveryTime = o.PlanDeliveryTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                AcAsyTime = o.AcAsyTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                AcDeliveryTime = o.AcDeliveryTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                Status = o.OrderStatus,
                TotalPallet = o.TotalPallet,
                TransCd = o.TransCd,
                TransMethod = o.TransMethod,
                ContSize = o.ContSize,
                TotalColumn = o.TotalColumn
            }).ToArray();
            Console.WriteLine($"First mapped UId string: {ordersForView.FirstOrDefault()?.UId}");
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
        [HttpGet]
        public async Task<JsonResult> GetOrderDetails(string orderId)
        {
            try
            {
                if (string.IsNullOrEmpty(orderId) || !Guid.TryParse(orderId, out Guid parsedOrderId))
                {
                    return Json(new { success = false, message = "Invalid orderId format" });
                }

                var orderDetails = await _orderDetailRepository.GetOrderDetailsByOrderId(parsedOrderId);

                // DEBUG LOG: Kiểm tra load dữ liệu
                foreach (var od in orderDetails)
                {
                    var slCount = od.ShoppingLists?.Count ?? 0;
                    var tpcTotal = od.ShoppingLists?.Sum(sl => sl.ThreePointChecks?.Count ?? 0) ?? 0;
                    Console.WriteLine($"OrderDetail {od.UId}: TotalPallet={od.TotalPallet}, SL Count={slCount}, Total TPC={tpcTotal}");
                }

                var detailsForView = orderDetails.Select(od => {
                    var progress = od.GetProgress();
                    Console.WriteLine($"Progress for {od.UId}: Collect={progress.CollectPercent}%, Prepare={progress.PreparePercent}%, Loading={progress.LoadingPercent}%, Status='{progress.Status}'");
                    return new
                    {
                        UId = progress.UId,
                        partNo = progress.PartNo,
                        quantity = progress.Quantity,
                        totalPallet = progress.TotalPallet,
                        palletSize = od.PalletSize ,
                        warehouse = progress.Warehouse,
                        contNo = progress.ContNo,
                        bookContStatus = progress.BookContStatus,
                        collectPercent = progress.CollectPercent,
                        preparePercent = progress.PreparePercent,
                        loadingPercent = progress.LoadingPercent,
                        currentStage = progress.CurrentStage,
                        status = progress.Status
                    };
                }).ToList();

                return Json(new { success = true, data = detailsForView });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCalendarData()
        {
            var today = DateTime.Today;
            var orders = await _orderRepository.GetOrdersByDate(today);
            var allCustomers = await _customerRepository.GetAllCustomers();
            var customerCodesWithOrders = orders.Select(o => o.CustomerCode).Distinct().ToHashSet();
            var customers = allCustomers.Where(c => customerCodesWithOrders.Contains(c.CustomerCode)).ToList();

            var ordersForView = orders.Select(o => new
            {
                UId = o.UId,
                Resource = o.CustomerCode,
                ShipDate = o.ShipDate.ToString("yyyy-MM-dd"),
                PlanAsyTime = o.PlanAsyTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                PlanDeliveryTime = o.PlanDeliveryTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                AcAsyTime = o.AcAsyTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                AcDeliveryTime = o.AcDeliveryTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                Status = o.OrderStatus,  // Sử dụng status mới
                TotalPallet = o.TotalPallet,
                TransCd = o.TransCd,
                TransMethod = o.TransMethod,
                ContSize = o.ContSize,
                TotalColumn = o.TotalColumn
            }).ToArray();

            var customersForView = customers.Select(c => new
            {
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName
            }).ToArray();

            return Json(new { orders = ordersForView, customers = customersForView });
        }
        private string MapOrderStatusToString(short orderStatus)
        {
            return orderStatus switch
            {
                0 => "Planned",
                1 => "Pending",
                2 => "Completed",         
                3 => "Shipped",
                _ => "Planned"
            };
        }
    }
}