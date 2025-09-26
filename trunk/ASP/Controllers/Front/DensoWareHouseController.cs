using ASP.Models.Front;
using Microsoft.AspNetCore.Mvc;

namespace ASP.Controllers.Front
{
    public class DensoWareHouseController : Controller
    {
        public IActionResult Calendar()
        {
            var orders = new[]
             {
                new {
                    Resource = "TMV",
                    PlanAsyTime = "2025-09-26T10:30:00",
                    PlanDeliveryTime = "2025-09-26T12:00:00",
                    Status = "processing"
                },
                new {
                    Resource = "DENSO",
                    PlanAsyTime = "2025-09-26T11:00:00",
                    PlanDeliveryTime = "2025-09-26T13:30:00",
                    Status = "done"
                },
                new {
                    Resource = "NISSAN",
                    PlanAsyTime = "2025-09-26T14:00:00",
                    PlanDeliveryTime = "2025-09-26T15:30:00",
                    Status = "delay"
                }
            };

            return View("~/Views/Front/DensoWareHouse/Calendar.cshtml", orders);
        }
    }
}