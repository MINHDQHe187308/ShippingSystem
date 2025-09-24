using ASP.Models.Front;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASP.Controllers.Front
{
    public class CustomerController : Controller
    {
        private readonly CustomerRepositoryInterface _customerRepository;

        public CustomerController(CustomerRepositoryInterface customerRepository)
        {
            _customerRepository = customerRepository;
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
    }
}
