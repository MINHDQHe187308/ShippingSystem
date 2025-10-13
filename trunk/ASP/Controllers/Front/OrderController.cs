using Microsoft.AspNetCore.Mvc;
using ASP.Models.Front;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;

namespace ASP.Controllers.Front
{
    public class OrderController : Controller
    {
        private readonly OrderRepositoryInterface _orderRepository;

        public OrderController(OrderRepositoryInterface orderRepository)
        {
            _orderRepository = orderRepository;
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
                    orders = orders.Where(o => o.StartTime.DayOfWeek == day);
                }
            }

            ViewData["WeekStart"] = weekStart;
            ViewBag.DayOfWeek = dayOfWeek;
            ViewBag.CustomerCode = customerCode;

            return View("~/Views/Front/DensoWareHouse/OrderList.cshtml", orders);
        }
    }
}