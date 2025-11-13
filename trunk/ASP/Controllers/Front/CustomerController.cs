using ASP.Models.Front;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace ASP.Controllers.Front
{
    public class CustomerController : Controller
    {
        private readonly CustomerRepositoryInterface _customerRepository;
        private readonly LeadtimeMasterRepositoryInterface _leadtimeRepository;
        private readonly ShippingScheduleRepositoryInterface _shippingScheduleRepository;

        public CustomerController(
            CustomerRepositoryInterface customerRepository,
            LeadtimeMasterRepositoryInterface leadtimeRepository,
            ShippingScheduleRepositoryInterface shippingScheduleRepository)
        {
            _customerRepository = customerRepository;
            _leadtimeRepository = leadtimeRepository;
            _shippingScheduleRepository = shippingScheduleRepository;
        }
        private bool IsRowEmpty(ExcelWorksheet sheet, int row)
        {
            for (int col = 1; col <= 8; col++)
            {
                var value = sheet.Cells[row, col].Value;
                if (value != null && !string.IsNullOrWhiteSpace(value.ToString().Trim()))
                    return false; // Có ít nhất 1 ô có dữ liệu → Không trống
            }
            return true; // Toàn bộ trống
        }
        // Danh sách Customers
        public async Task<IActionResult> CustomerList()
        {
            var customers = await _customerRepository.GetAllCustomers();
            if (customers == null || customers.Count == 0)
            {
                TempData["ErrorMessage"] = "No customers found.";
                return View("~/Views/Front/Home/CustomerList.cshtml", new List<Customer>());
            }

            return View("~/Views/Front/Home/CustomerList.cshtml", customers);
        }

        // Tạo mới Customer (Ajax gọi đến)
        [HttpPost]
        public async Task<JsonResult> AddSupplier([FromBody] Customer request)
        {
            var success = await _customerRepository.CreateCustomer(request);
            if (success)
                return Json(new { success = true });
            return Json(new { success = false, message = "Cannot create supplier" });
        }

        // Cập nhật Customer
        [HttpPost]
        public async Task<JsonResult> UpdateSupplier([FromBody] Customer request)
        {
            if (request == null || string.IsNullOrEmpty(request.CustomerCode))
            {
                return Json(new { success = false, message = "Mã khách hàng không hợp lệ" });
            }

            var result = await _customerRepository.UpdateCustomerByCode(request.CustomerCode, request);

            if (result.Success)
                return Json(new { success = true, message = result.Message });

            return Json(new { success = false, message = result.Message });
        }

        // Xóa Customer
        [HttpPost]
        public async Task<JsonResult> DeleteSupplier(string code)
        {
            var success = await _customerRepository.RemoveCustomerByCode(code);
            if (success)
                return Json(new { success = true });
            return Json(new { success = false, message = "Cannot delete supplier" });
        }

        // Lấy danh sách LeadtimeMaster theo CustomerCode
        [HttpGet]
        public async Task<JsonResult> GetLeadtimesByCustomer(string customerCode)
        {
            if (string.IsNullOrEmpty(customerCode))
            {
                return Json(new { success = false, message = "CustomerCode không hợp lệ" });
            }

            var leadtimes = await _leadtimeRepository.GetAllLeadtimesByCustomer(customerCode);
            return Json(new { success = true, data = leadtimes });
        }

        // Thêm LeadtimeMaster
        [HttpPost]
        public async Task<JsonResult> AddLeadtime([FromBody] LeadtimeMaster request)
        {
            if (request == null || string.IsNullOrEmpty(request.CustomerCode) || string.IsNullOrEmpty(request.TransCd))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var success = await _leadtimeRepository.CreateLeadtime(request);
            if (success)
                return Json(new { success = true });
            return Json(new { success = false, message = "Cannot create leadtime" });
        }

        // Cập nhật LeadtimeMaster
        [HttpPost]
        public async Task<JsonResult> UpdateLeadtime([FromBody] LeadtimeMaster request)
        {
            if (request == null || string.IsNullOrEmpty(request.CustomerCode) || string.IsNullOrEmpty(request.TransCd))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var result = await _leadtimeRepository.UpdateLeadtimeByKey(request.CustomerCode, request.TransCd, request);
            if (result.Success)
                return Json(new { success = true, message = result.Message });

            return Json(new { success = false, message = result.Message });
        }

        // Xóa LeadtimeMaster
        [HttpPost]
        public async Task<JsonResult> DeleteLeadtime(string customerCode, string transCd)
        {
            var success = await _leadtimeRepository.RemoveLeadtimeByKey(customerCode, transCd);
            if (success)
                return Json(new { success = true });
            return Json(new { success = false, message = "Cannot delete leadtime" });
        }

        // Lấy danh sách ShippingSchedule theo CustomerCode
        [HttpGet]
        public async Task<JsonResult> GetShippingSchedulesByCustomer(string customerCode)
        {
            if (string.IsNullOrEmpty(customerCode))
            {
                return Json(new { success = false, message = "CustomerCode không hợp lệ" });
            }

            var schedules = await _shippingScheduleRepository.GetAllShippingSchedulesByCustomer(customerCode);
            return Json(new { success = true, data = schedules });
        }

        // Thêm ShippingSchedule
        [HttpPost]
        public async Task<JsonResult> AddShippingSchedule([FromBody] ShippingSchedule request)
        {
            if (request == null || string.IsNullOrEmpty(request.CustomerCode) || string.IsNullOrEmpty(request.TransCd))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var success = await _shippingScheduleRepository.CreateShippingSchedule(request);
            if (success)
                return Json(new { success = true });
            return Json(new { success = false, message = "Cannot create shipping schedule" });
        }

        // Cập nhật ShippingSchedule
        [HttpPost]
        public async Task<JsonResult> UpdateShippingSchedule([FromBody] ShippingSchedule request)
        {
            if (request == null || string.IsNullOrEmpty(request.CustomerCode) || string.IsNullOrEmpty(request.TransCd))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var result = await _shippingScheduleRepository.UpdateShippingScheduleByKey(request.CustomerCode, request.TransCd, request.Weekday, request);
            if (result.Success)
                return Json(new { success = true, message = result.Message });

            return Json(new { success = false, message = result.Message });
        }

        // Xóa ShippingSchedule
        [HttpPost]
        public async Task<JsonResult> DeleteShippingSchedule(string customerCode, string transCd, int weekday)
        {
            var success = await _shippingScheduleRepository.RemoveShippingScheduleByKey(customerCode, transCd, (DayOfWeek)weekday);
            if (success)
                return Json(new { success = true });
            return Json(new { success = false, message = "Cannot delete shipping schedule" });
        }
        [HttpPost]
        public async Task<IActionResult> ImportExcel(IFormFile excelFile)
        {
            // FIX: Set license mới cho EPPlus 8.2.1 (thay "Your Name" bằng tên thật hoặc tổ chức)
            ExcelPackage.License.SetNonCommercialPersonal("Your Name"); // Hoặc SetNonCommercialOrganization("Your Org");

            if (excelFile == null || excelFile.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded." });
            }
            if (!Path.GetExtension(excelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Only .xlsx files are supported." });
            }

            var results = new ImportResult
            {
                CustomersAdded = 0,
                LeadtimesAdded = 0,
                ShippingSchedulesAdded = 0,
                Errors = new List<string>()
            };

            try
            {
                using var stream = new MemoryStream();
                await excelFile.CopyToAsync(stream);
                // Không cần set LicenseContext nữa - đã set ở trên
                using var package = new ExcelPackage(stream);

                // Import from Single Sheet: "Data"
                var sheet = package.Workbook.Worksheets["Data"];
                if (sheet != null)
                {
                    await ImportFromSingleSheet(sheet, results);
                }
                else
                {
                    results.Errors.Add("Sheet 'Data' not found. Please use a single sheet named 'Data'.");
                }
            }
            catch (Exception ex)
            {
                results.Errors.Add($"Error processing file: {ex.Message}");
                // Log chi tiết (tùy chọn): _logger.LogError(ex, "Import Excel error");
            }

            if (results.Errors.Any())
            {
                return Json(new { success = false, message = string.Join("; ", results.Errors) });
            }

            return Json(new
            {
                success = true,
                message = $"Import successful! Added {results.CustomersAdded} customers, {results.LeadtimesAdded} leadtimes, {results.ShippingSchedulesAdded} shipping schedules."
            });
        }
        private async Task ImportFromSingleSheet(ExcelWorksheet sheet, ImportResult results)
        {
            var rowCount = sheet.Dimension?.Rows ?? 0;

            // Trim hàng trống ở cuối sheet tự động
            while (rowCount >= 2 && IsRowEmpty(sheet, rowCount))
            {
                rowCount--;
            }
            if (rowCount < 2)
            {
                results.Errors.Add("No data rows found (only header or empty sheet).");
                return;
            }

            for (int row = 2; row <= rowCount; row++)
            {
                // Bỏ qua ngay nếu hàng trống (không báo lỗi)
                if (IsRowEmpty(sheet, row))
                {
                    continue;
                }

                try
                {
                    var customerCode = sheet.Cells[row, 1].GetValue<string>()?.Trim();
                    var customerName = sheet.Cells[row, 2].GetValue<string>()?.Trim();
                    var transCd = sheet.Cells[row, 3].GetValue<string>()?.Trim();
                    var collectTime = sheet.Cells[row, 4].GetValue<decimal?>();
                    var prepareTime = sheet.Cells[row, 5].GetValue<decimal?>();
                    var loadingTime = sheet.Cells[row, 6].GetValue<decimal?>();

                    // Read weekday cell raw - can be string or numeric
                    var weekdayCell = sheet.Cells[row, 7].Value;
                    var weekdayStr = weekdayCell?.ToString()?.Trim(); // e.g., "Monday" or "1"

                    // Read raw value for CutOffTime - can be string, DateTime, or Excel numeric (serial)
                    var cutOffCell = sheet.Cells[row, 8].Value;
                    var cutOffTimeStr = cutOffCell as string;
                    if (cutOffTimeStr != null) cutOffTimeStr = cutOffTimeStr.Trim();

                    // Validation: Chỉ báo lỗi nếu có dữ liệu nhưng thiếu trường (không phải trống hoàn toàn)
                    if (string.IsNullOrEmpty(customerCode) || string.IsNullOrEmpty(customerName) || string.IsNullOrEmpty(transCd) ||
                        !collectTime.HasValue || !prepareTime.HasValue || !loadingTime.HasValue || weekdayCell == null || cutOffCell == null)
                    {
                        results.Errors.Add($"Row {row}: Invalid data (all fields required).");
                        continue;
                    }

                    // Create or update Customer
                    var customer = new Customer
                    {
                        CustomerCode = customerCode,
                        CustomerName = customerName,
                        Descriptions = "" // Optional, set empty
                    };
                    var customerSuccess = await _customerRepository.CreateCustomer(customer); // Assume Create handles duplicates (e.g., upsert)
                    if (customerSuccess) results.CustomersAdded++;
                    else results.Errors.Add($"Row {row}: Failed to add/update customer {customerCode}.");

                    // Create Leadtime
                    var leadtime = new LeadtimeMaster
                    {
                        CustomerCode = customerCode,
                        TransCd = transCd,
                        CollectTimePerPallet = (double)collectTime.Value,
                        PrepareTimePerPallet = (double)prepareTime.Value,
                        LoadingTimePerColumn = (double)loadingTime.Value
                    };
                    var ltSuccess = await _leadtimeRepository.CreateLeadtime(leadtime);
                    if (ltSuccess) results.LeadtimesAdded++;
                    else results.Errors.Add($"Row {row}: Failed to add leadtime for {customerCode}-{transCd}.");

                    // Parse weekday: Accept number or day name
                    int weekdayInt;
                    if (!int.TryParse(weekdayStr, out weekdayInt))
                    {
                        if (Enum.TryParse<DayOfWeek>(weekdayStr, true, out var dayOfWeek))
                        {
                            weekdayInt = (int)dayOfWeek;
                        }
                        else
                        {
                            results.Errors.Add($"Row {row}: Invalid weekday {weekdayCell}.");
                            continue;
                        }
                    }
                    if (weekdayInt < 0 || weekdayInt > 6)
                    {
                        results.Errors.Add($"Row {row}: Invalid weekday {weekdayCell}.");
                        continue;
                    }

                    // Parse TimeOnly from multiple possible Excel cell types and formats
                    TimeOnly cutOffTimeOnly;
                    bool parsedCutOff = false;
                    if (cutOffCell == null)
                    {
                        results.Errors.Add($"Row {row}: Invalid cutOffTime (empty).");
                        continue;
                    }
                    // If cell is DateTime (Excel stores times as DateTime sometimes)
                    if (cutOffCell is DateTime dt)
                    {
                        cutOffTimeOnly = TimeOnly.FromDateTime(dt);
                        parsedCutOff = true;
                    }
                    // If cell is double (Excel serial number for date/time)
                    else if (cutOffCell is double dbl)
                    {
                        // Excel stores time as fraction of a day
                        var ts = TimeSpan.FromDays(dbl);
                        cutOffTimeOnly = TimeOnly.FromTimeSpan(ts);
                        parsedCutOff = true;
                    }
                    else if (!string.IsNullOrEmpty(cutOffTimeStr))
                    {
                        // Try several common time formats: HH:mm, H:mm, HH:mm:ss, H:mm:ss
                        var formats = new[] { "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss" };
                        foreach (var fmt in formats)
                        {
                            if (TimeOnly.TryParseExact(cutOffTimeStr, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out cutOffTimeOnly))
                            {
                                parsedCutOff = true;
                                break;
                            }
                        }
                        // Try a general parse as fallback (will handle culture-specifics)
                        if (!parsedCutOff && TimeOnly.TryParse(cutOffTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out cutOffTimeOnly))
                        {
                            parsedCutOff = true;
                        }
                    }
                    if (!parsedCutOff)
                    {
                        results.Errors.Add($"Row {row}: Invalid cutOffTime {cutOffCell}. Expected format HH:mm or HH:mm:ss (seconds optional).");
                        continue;
                    }

                    // Create ShippingSchedule
                    var schedule = new ShippingSchedule
                    {
                        CustomerCode = customerCode,
                        TransCd = transCd,
                        Weekday = (DayOfWeek)weekdayInt,
                        CutOffTime = cutOffTimeOnly,
                        Description = "" // Optional, set empty
                    };
                    var ssSuccess = await _shippingScheduleRepository.CreateShippingSchedule(schedule);
                    if (ssSuccess) results.ShippingSchedulesAdded++;
                    else results.Errors.Add($"Row {row}: Failed to add shipping schedule for {customerCode}-{transCd}-{weekdayInt}.");
                }
                catch (Exception ex)
                {
                    results.Errors.Add($"Row {row}: {ex.Message}");
                }
            }
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("Data");
                // Header (updated to new format)
                worksheet.Cells[1, 1].Value = "CustomerCode";
                worksheet.Cells[1, 2].Value = "CustomerName";
                worksheet.Cells[1, 3].Value = "TransCode";
                worksheet.Cells[1, 4].Value = "CollectTime";
                worksheet.Cells[1, 5].Value = "PrepareTime";
                worksheet.Cells[1, 6].Value = "LoadingTime";
                worksheet.Cells[1, 7].Value = "Weekday";
                worksheet.Cells[1, 8].Value = "CutOffTime";
                // Style header
                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }
                // Sample data (one row example)
                // Row 2: Sample entry
                worksheet.Cells[2, 1].Value = "CUST001";
                worksheet.Cells[2, 2].Value = "Company ABC";
                worksheet.Cells[2, 3].Value = "TRANS001";
                worksheet.Cells[2, 4].Value = 1.5; // CollectTime
                worksheet.Cells[2, 5].Value = 2.0; // PrepareTime
                worksheet.Cells[2, 6].Value = 0.5; // LoadingTime
                worksheet.Cells[2, 7].Value = "1"; // Monday (or "Monday")
                worksheet.Cells[2, 8].Value = "13:00"; // CutOffTime (hours:minutes)
                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
                package.Save();
            }
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Customer_Import_Template.xlsx");
        }
    }
}
public class ImportResult
{
    public int CustomersAdded { get; set; }
    public int LeadtimesAdded { get; set; }
    public int ShippingSchedulesAdded { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}
    
