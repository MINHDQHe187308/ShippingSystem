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
        private readonly DelayHistoryRepositoryInterface _delayHistoryRepository;  // THÊM: Inject DelayHistoryRepo

        public DensoWareHouseController(
            OrderRepositoryInterface orderRepository,
            CustomerRepositoryInterface customerRepository,
            OrderDetailRepositoryInterface orderDetailRepository,
            DelayHistoryRepositoryInterface delayHistoryRepository)  // THÊM
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _orderDetailRepository = orderDetailRepository;
            _delayHistoryRepository = delayHistoryRepository;
        }

        public async Task<IActionResult> Calendar()
        {
            var today = DateTime.Today;

            // Lấy orders từ DB, filter theo ShipDate = hôm nay
            var orders = await _orderRepository.GetOrdersByDate(today);

            Console.WriteLine($"Loaded {orders.Count} orders for {today}. First UId: {orders.FirstOrDefault()?.UId}");
            if (orders.Any())
            {
                Console.WriteLine($"First Order Details: UId={orders.First().UId}, Status={orders.First().OrderStatus}, StartTime={orders.First().StartTime}");
            }
            // Lấy tất cả customers từ DB
            var allCustomers = await _customerRepository.GetAllCustomers();

            // Lọc chỉ những customers có order trong ngày hiện tại
            var customerCodesWithOrders = orders.Select(o => o.CustomerCode).Distinct().ToHashSet();
            var customers = allCustomers.Where(c => customerCodesWithOrders.Contains(c.CustomerCode)).ToList();

            // Map orders sang anonymous object - THÊM UId VÀ GIỮ STATUS LÀ NUMBER, THÊM TOTALPALLET VÀ CÁC TRƯỜNG KHÁC + THÊM DELAY INFO NẾU STATUS=4
            var ordersForView = orders.Select(o => {
                // Collect: Tổng số pallet đã collect (đếm SL có PLStatus==1, không cần All)
                int collectCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == 1) ?? 0) ?? 0;

                // ThreePointScan: Tổng số pallet đã prepare (SL==2)
                int prepareCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == 2) ?? 0) ?? 0;

                // LoadCont: Tổng số pallet đã load (SL==3)
                int loadCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == 3) ?? 0) ?? 0;

                // SỬA: Delay info nếu OrderStatus==4 - DÙNG ORDER FIELDS THAY VÌ HISTORIES
                string delayStartTime = null;
                double delayTime = 0;
                if (o.OrderStatus == 4)
                {
                    delayStartTime = o.DelayStartTime?.ToString("yyyy-MM-ddTHH:mm:ss");  // ← FIX: Từ Order field
                    delayTime = o.DelayTime ?? 0;  // ← FIX: Handle nullable double? with null-coalescing
                    Console.WriteLine($"Order {o.UId}: DelayStartTime={o.DelayStartTime}, DelayTime={o.DelayTime}");  // ← DEBUG LOG
                }

                return new
                {
                    UId = o.UId,
                    Resource = o.CustomerCode,
                    ShipDate = o.ShipDate.ToString("yyyy-MM-dd"),
                    StartTime = o.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    EndTime = o.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    AcStartTime = o.AcStartTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    AcEndTime = o.AcEndTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Status = o.OrderStatus,
                    TotalPallet = o.TotalPallet,
                    // THÊM: Các trường mới cho progress (dạng string "X / Y")
                    CollectPallet = $"{collectCount} / {o.TotalPallet}",
                    ThreePointScan = $"{prepareCount} / {o.TotalPallet}",
                    LoadCont = $"{loadCount} / {o.TotalPallet}",
                    TransCd = o.TransCd,
                    TransMethod = o.TransMethod,
                    ContSize = o.ContSize,
                    TotalColumn = o.TotalColumn,
                    // THÊM: Delay fields
                    DelayStartTime = delayStartTime,
                    DelayTime = delayTime
                };
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
                var order = await _orderRepository.GetOrderById(parsedOrderId);  // THÊM: Lấy full order để tính New Time

                // DEBUG LOG: Kiểm tra load dữ liệu
                foreach (var od in orderDetails)
                {
                    var slCount = od.ShoppingLists?.Count ?? 0;
                    var tpcTotal = od.ShoppingLists?.Sum(sl => sl.ThreePointCheck != null ? 1 : 0) ?? 0;  // SỬA: 1-1 relationship, đếm 1 nếu có ThreePointCheck
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

                // SỬA: Tính New Time nếu Delay - DÙNG ORDER FIELDS
                object orderSummary = null;
                if (order != null && order.OrderStatus == 4)
                {
                    // SỬA: Không dùng latestDelay từ Histories
                    DateTime? delayStart = order.DelayStartTime;  // ← FIX
                    double delayTime = order.DelayTime ?? 0;  // ← FIX: Handle nullable double?
                    if (delayStart.HasValue)
                    {
                        DateTime baseStart = order.AcStartTime.HasValue ? order.AcStartTime.Value : order.StartTime;
                        DateTime baseEnd = order.AcEndTime.HasValue ? order.AcEndTime.Value : order.EndTime;
                        DateTime newEnd = baseEnd.AddHours(delayTime);  // ← Dùng delayTime từ Order
                        string newTimeRange = $"{baseStart:HH:mm:ss} - {newEnd:HH:mm:ss}";

                        orderSummary = new
                        {
                            newTimeRange = newTimeRange,
                            delayTime = delayTime
                        };
                        Console.WriteLine($"Order {order.UId}: Calculated newTimeRange={newTimeRange}, delayTime={delayTime}");  // ← DEBUG LOG
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
            var orders = await _orderRepository.GetOrdersWithDelayByDate(today);  // SỬA: Dùng method include DelayHistories (nhưng map dùng Order fields)
            var allCustomers = await _customerRepository.GetAllCustomers();
            var customerCodesWithOrders = orders.Select(o => o.CustomerCode).Distinct().ToHashSet();
            var customers = allCustomers.Where(c => customerCodesWithOrders.Contains(c.CustomerCode)).ToList();

            // THÊM: Include DelayHistories cho Delay info (nhưng không dùng)

            var ordersForView = orders.Select(o => {  // SỬA: Dùng Select thay vì Select(o => { ... }).ToArray() cho consistency
                // Collect: Tổng số pallet đã collect (đếm SL có PLStatus==1, không cần All)
                int collectCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == 1) ?? 0) ?? 0;

                // ThreePointScan: Tổng số pallet đã prepare (SL==2)
                int prepareCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == 2) ?? 0) ?? 0;

                // LoadCont: Tổng số pallet đã load (SL==3)
                int loadCount = o.OrderDetails?.Sum(od => od.ShoppingLists?.Count(sl => sl.PLStatus == 3) ?? 0) ?? 0;

                // SỬA: Delay info nếu OrderStatus==4 - DÙNG ORDER FIELDS THAY VÌ HISTORIES
                string delayStartTime = null;
                double delayTime = 0;
                if (o.OrderStatus == 4)
                {
                    delayStartTime = o.DelayStartTime?.ToString("yyyy-MM-ddTHH:mm:ss");  // ← FIX: Từ Order field
                    delayTime = o.DelayTime ?? 0;  // ← FIX: Handle nullable double?
                    Console.WriteLine($"GetCalendarData Order {o.UId}: DelayStartTime={o.DelayStartTime}, DelayTime={o.DelayTime}");  // ← DEBUG LOG
                }

                return new
                {
                    UId = o.UId,
                    Resource = o.CustomerCode,
                    ShipDate = o.ShipDate.ToString("yyyy-MM-dd"),
                    StartTime = o.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    EndTime = o.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    AcStartTime = o.AcStartTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    AcEndTime = o.AcEndTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Status = o.OrderStatus,
                    TotalPallet = o.TotalPallet,
                    // THÊM: Các trường mới cho progress (dạng string "X / Y")
                    CollectPallet = $"{collectCount} / {o.TotalPallet}",
                    ThreePointScan = $"{prepareCount} / {o.TotalPallet}",
                    LoadCont = $"{loadCount} / {o.TotalPallet}",
                    TransCd = o.TransCd,
                    TransMethod = o.TransMethod,
                    ContSize = o.ContSize,
                    TotalColumn = o.TotalColumn,
                    // THÊM: Delay fields
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