using Microsoft.AspNetCore.Mvc;
using ASP.Models.Front;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;
using OfficeOpenXml;  // EPPlus package
using System.IO;
using ASP.DTO.DensoDTO;  // THÊM: Import để sử dụng CollectionStatusEnumDTO

namespace ASP.Controllers.Front
{
    public class OrderController : Controller
    {
        private readonly OrderRepositoryInterface _orderRepository;
        private readonly OrderDetailRepositoryInterface _orderDetailRepository;

        public OrderController(OrderRepositoryInterface orderRepository, OrderDetailRepositoryInterface orderDetailRepository)
        {
            _orderRepository = orderRepository;
            _orderDetailRepository = orderDetailRepository;
        }

        public async Task<IActionResult> OrderList(string dayOfWeek = null, string customerCode = null)
        {
            var today = DateTime.Today;
            int dayOfWeekInt = (int)today.DayOfWeek;
            int diff = dayOfWeekInt - (int)DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            var weekStart = today.AddDays(-diff);
            var allOrders = await _orderRepository.GetOrdersForWeek(weekStart);
            var customerCodes = allOrders.Select(o => o.CustomerCode).Distinct().OrderBy(c => c).ToList();
            ViewBag.CustomerCodes = customerCodes;
            IEnumerable<ASP.Models.Front.Order> orders = allOrders;
            if (!string.IsNullOrEmpty(customerCode))
            {
                orders = orders.Where(o => o.CustomerCode == customerCode);
            }
            if (!string.IsNullOrEmpty(dayOfWeek))
            {
                if (Enum.TryParse<DayOfWeek>(dayOfWeek, true, out var day))
                {
                    orders = orders.Where(o => o.ShipDate.DayOfWeek == day);
                }
            }
            ViewData["WeekStart"] = weekStart;
            ViewBag.DayOfWeek = dayOfWeek;
            ViewBag.CustomerCode = customerCode;
            return View("~/Views/Front/DensoWareHouse/OrderList.cshtml", orders);
        }

        // Action để export Excel - Thiết kế đơn giản: Header Order -> Headers data -> Flat rows (repeat OrderDetail info cho mỗi ShoppingList)
        [HttpGet]
        public async Task<IActionResult> ExportExcel(Guid orderId)
        {
            try
            {
                // Set EPPlus License (thay "Your Name" bằng tên thực tế)
                ExcelPackage.License.SetNonCommercialPersonal("Your Name");  // Hoặc SetNonCommercialOrganization cho tổ chức

                // Lấy full order với details
                var order = await _orderRepository.GetOrderById(orderId);
                if (order == null)
                {
                    return NotFound("Order not found.");
                }

                // Lấy order details với shopping lists
                var orderDetails = order.OrderDetails ?? await _orderDetailRepository.GetOrderDetailsByOrderId(orderId);

                // Tạo Excel package
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Picking List");

                int currentRow = 1;  // Bắt đầu từ row 1

                // 1. Order Header (merged across columns)
                int headerCols = 8;  // Số cột cho header (dựa trên data columns sau)
                worksheet.Cells[currentRow, 1, currentRow, headerCols].Merge = true;
                worksheet.Cells[currentRow, 1].Value = $"ORDER INFORMATION";
                worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 1].Style.Font.Size = 14;
                worksheet.Cells[currentRow, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));  // Blue header
                worksheet.Cells[currentRow, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Cells[currentRow, 1, currentRow, headerCols].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
                currentRow += 2;  // Skip line for spacing

                // Order details row (non-merged, aligned properly)
                var orderInfo = new object[] {
                    "Order UId:", order.UId.ToString(),
                    "Customer:", order.CustomerCode,
                    "Ship Date:", order.ShipDate.ToString("yyyy-MM-dd"),
                    "Total Pallets:", order.TotalPallet.ToString()
                };
                worksheet.Cells[currentRow, 1, currentRow, orderInfo.Length].LoadFromArrays(new[] { orderInfo });
                worksheet.Cells[currentRow, 1, currentRow, orderInfo.Length].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 1, currentRow, orderInfo.Length].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 1, currentRow, orderInfo.Length].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(236, 240, 241));  // Light gray
                currentRow += 2;  // Spacing

                // 2. Headers cho data (aligned exactly 8 columns)
                var dataHeaders = new string[] { "Part No", "Pallet Size", "Quantity", "Total Pallets", "Warehouse", "Pallet No", "PL Status", "Collected Date" };
                var headerRange = worksheet.Cells[currentRow, 1, currentRow, dataHeaders.Length];
                headerRange.LoadFromArrays(new[] { dataHeaders });
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(52, 152, 219));  // Lighter blue
                headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
                headerRange.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                currentRow++;  // Next row for data

                // 3. Flat data rows: Repeat OrderDetail info cho mỗi ShoppingList, sắp xếp theo PartNo rồi PalletNo
                foreach (var od in orderDetails.OrderBy(od => od.PartNo))  // Sắp xếp theo PartNo
                {
                    var shoppingLists = od.ShoppingLists?.OrderBy(sl => sl.PalletNo).ToList() ?? new List<ShoppingList>();
                    if (!shoppingLists.Any())
                    {
                        // Nếu không có ShoppingList, thêm 1 row empty với OrderDetail info
                        var emptyRow = new object[] {
                            od.PartNo, od.PalletSize, od.Quantity, od.TotalPallet, od.Warehouse,
                            "No Pallets", "N/A", "N/A"
                        };
                        worksheet.Cells[currentRow, 1, currentRow, dataHeaders.Length].LoadFromArrays(new[] { emptyRow });
                        worksheet.Cells[currentRow, 1, currentRow, dataHeaders.Length].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                        currentRow++;
                    }
                    else
                    {
                        foreach (var sl in shoppingLists)
                        {
                            // Map PLStatus từ enum
                            string plStatusText = sl.PLStatus switch
                            {
                                (short)CollectionStatusEnumDTO.None => "None",
                                (short)CollectionStatusEnumDTO.Collected => "Đã Thu Thập",
                                (short)CollectionStatusEnumDTO.Exported => "Đã Check 3 điểm",
                                (short)CollectionStatusEnumDTO.Delivered => "Đã Load lên Cont",
                                (short)CollectionStatusEnumDTO.Canceled => "Bị Huỷ",
                                _ => $"Unknown ({sl.PLStatus})"
                            };

                            // Data row: Exactly 8 columns, aligned properly (repeat OrderDetail fields)
                            var dataRow = new object[] {
                                od.PartNo,                    // Col 1: Part No
                                od.PalletSize,                // Col 2: Pallet Size
                                od.Quantity,                  // Col 3: Quantity
                                od.TotalPallet,               // Col 4: Total Pallets
                                od.Warehouse,                 // Col 5: Warehouse
                                sl.PalletNo,                  // Col 6: Pallet No
                                plStatusText,                 // Col 7: PL Status (mapped từ enum)
                                sl.CollectedDate?.ToString("yyyy-MM-dd HH:mm") ?? "N/A"  // Col 8: Collected Date
                            };
                            worksheet.Cells[currentRow, 1, currentRow, dataHeaders.Length].LoadFromArrays(new[] { dataRow });
                            // Conditional formatting: Green nếu Collected hoặc cao hơn (Exported, Delivered)
                            if (sl.PLStatus >= (short)CollectionStatusEnumDTO.Collected && sl.PLStatus != (short)CollectionStatusEnumDTO.Canceled)
                            {
                                var statusRange = worksheet.Cells[currentRow, 7];  // Col G: PL Status
                                statusRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                statusRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(144, 238, 144));  // Light green
                                var dateRange = worksheet.Cells[currentRow, 8];  // Col H: Collected Date
                                dateRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                dateRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(144, 238, 144));  // Light green
                            }
                            // Red nếu Canceled
                            else if (sl.PLStatus == (short)CollectionStatusEnumDTO.Canceled)
                            {
                                var statusRange = worksheet.Cells[currentRow, 7];  // Col G: PL Status
                                statusRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                statusRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 182, 193));  // Light red
                            }
                            worksheet.Cells[currentRow, 1, currentRow, dataHeaders.Length].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                            currentRow++;
                        }
                    }
                }

                // Auto-fit columns và thêm outer border cho data range
                worksheet.Cells.AutoFitColumns(15);  // Max width 15 chars cho readability
                if (currentRow > 3)  // Nếu có data
                {
                    worksheet.Cells[3, 1, currentRow - 1, dataHeaders.Length].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);  // Outer border cho data (từ header row 3)
                }

                // Tên file
                var fileName = $"PickingList_Order_{order.UId}_{order.ShipDate:yyyyMMdd}.xlsx";
                var fileBytes = package.GetAsByteArray();

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating Excel: {ex.Message}");
            }
        }
    }
}