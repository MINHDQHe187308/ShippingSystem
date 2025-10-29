using ASP.Models.Front;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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
    }
}