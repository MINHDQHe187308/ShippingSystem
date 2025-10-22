// File: ASP.Controllers.Front/DensoWareHouseController.cs (Cập nhật để sử dụng BookContStatus và progress mới; giữ logic delay)
using ASP.DTO.DensoDTO;
using ASP.Models.ASPModel;
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
        private readonly DelayHistoryRepositoryInterface _delayHistoryRepository;

        public DensoWareHouseController(
            OrderRepositoryInterface orderRepository,
            CustomerRepositoryInterface customerRepository,
            OrderDetailRepositoryInterface orderDetailRepository,
            DelayHistoryRepositoryInterface delayHistoryRepository)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _orderDetailRepository = orderDetailRepository;
            _delayHistoryRepository = delayHistoryRepository;
        }

        public async Task<IActionResult> Calendar()
        {
            var today = DateTime.Today;
            var orders = await _orderRepository.GetOrdersByDate(today);

            if (orders.Any())
            {
                Console.WriteLine($"First Order Details: UId={orders.First().UId}, Status={orders.First().OrderStatus}, StartTime={orders.First().StartTime}");
            }

            var allCustomers = await _customerRepository.GetAllCustomers();
            var customerCodesWithOrders = orders.Select(o => o.CustomerCode).Distinct().ToHashSet();
            var customers = allCustomers.Where(c => customerCodesWithOrders.Contains(c.CustomerCode)).ToList();

            var ordersForView = orders.Select(o => {
                int collectCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == (short)CollectionStatusEnumDTO.Collected) ?? 0) ?? 0;
                int prepareCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == (short)CollectionStatusEnumDTO.Exported) ?? 0) ?? 0;
                int loadCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == (short)CollectionStatusEnumDTO.Delivered) ?? 0) ?? 0;

                string delayStartTime = null;
                double delayTime = 0;
                if (o.OrderStatus == 4)
                {
                    delayStartTime = o.DelayStartTime?.ToString("yyyy-MM-ddTHH:mm:ss");
                    delayTime = o.DelayTime ?? 0;
                }

                return new
                {
                    UId = o.UId.ToString(),
                    Resource = o.CustomerCode ?? "Unknown",
                    ShipDate = o.ShipDate.ToString("yyyy-MM-dd"),
                    StartTime = o.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    EndTime = o.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    AcStartTime = o.AcStartTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    AcEndTime = o.AcEndTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Status = o.OrderStatus,
                    TotalPallet = o.TotalPallet,
                    CollectPallet = $"{collectCount} / {o.TotalPallet}",
                    ThreePointScan = $"{prepareCount} / {o.TotalPallet}",
                    LoadCont = $"{loadCount} / {o.TotalPallet}",
                    TransCd = o.TransCd ?? "N/A",
                    TransMethod = o.TransMethod.ToString(),
                    ContSize = o.ContSize.ToString(),
                    TotalColumn = o.TotalColumn,
                    DelayStartTime = delayStartTime,
                    DelayTime = delayTime
                };
            }).ToArray();

            var customersForView = customers.Select(c => new
            {
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName
            }).ToArray();

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
                var order = await _orderRepository.GetOrderById(parsedOrderId);

                foreach (var od in orderDetails)
                {
                    var slCount = od.ShoppingLists?.Count ?? 0;
                    var tpcTotal = od.ShoppingLists?.Count(sl => sl.ThreePointCheck != null) ?? 0;
                }

                var detailsForView = orderDetails.Select(od => {
                    var progress = od.GetProgress();
                    return new
                    {
                        UId = progress.UId,
                        partNo = progress.PartNo,
                        quantity = progress.Quantity,
                        totalPallet = progress.TotalPallet,
                        palletSize = od.PalletSize,
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

                object orderSummary = null;
                if (order != null && order.OrderStatus == 4)
                {
                    DateTime? delayStart = order.DelayStartTime;
                    double delayTime = order.DelayTime ?? 0;
                    if (delayStart.HasValue)
                    {
                        DateTime baseStart = order.AcStartTime.HasValue ? order.AcStartTime.Value : order.StartTime;
                        DateTime baseEnd = order.AcEndTime.HasValue ? order.AcEndTime.Value : order.EndTime;
                        DateTime newEnd = baseEnd.AddHours(delayTime);
                        string newTimeRange = $"{baseStart:HH:mm:ss} - {newEnd:HH:mm:ss}";

                        orderSummary = new
                        {
                            newTimeRange = newTimeRange,
                            delayTime = delayTime
                        };
                    }
                }

                return Json(new { success = true, data = detailsForView, orderSummary = orderSummary });
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

            var ordersForView = orders.Select(o => {
                int collectCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == (short)CollectionStatusEnumDTO.Collected) ?? 0) ?? 0;
                int prepareCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == (short)CollectionStatusEnumDTO.Exported) ?? 0) ?? 0;
                int loadCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == (short)CollectionStatusEnumDTO.Delivered) ?? 0) ?? 0;

                string delayStartTime = null;
                double delayTime = 0;
                if (o.OrderStatus == 4)
                {
                    delayStartTime = o.DelayStartTime?.ToString("yyyy-MM-ddTHH:mm:ss");
                    delayTime = o.DelayTime ?? 0;
                }

                return new
                {
                    UId = o.UId.ToString(),
                    Resource = o.CustomerCode ?? "Unknown",
                    ShipDate = o.ShipDate.ToString("yyyy-MM-dd"),
                    StartTime = o.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    EndTime = o.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    AcStartTime = o.AcStartTime?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
                    AcEndTime = o.AcEndTime?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
                    Status = o.OrderStatus,
                    TotalPallet = o.TotalPallet,
                    CollectPallet = $"{collectCount} / {o.TotalPallet}",
                    ThreePointScan = $"{prepareCount} / {o.TotalPallet}",
                    LoadCont = $"{loadCount} / {o.TotalPallet}",
                    TransCd = o.TransCd ?? "N/A",
                    TransMethod = o.TransMethod.ToString(),
                    ContSize = o.ContSize.ToString(),
                    TotalColumn = o.TotalColumn,
                    DelayStartTime = delayStartTime,
                    DelayTime = delayTime
                };
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
                4 => "Delay",
                _ => "Planned"
            };
        }
    }
}