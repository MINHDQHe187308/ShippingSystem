using ASP.DTO.DensoDTO;
using ASP.Models.Front;
using Microsoft.EntityFrameworkCore;

namespace ASP.DTO.DensoDTO
{
    public static class OrderDetailExtensions
    {
        public static OrderDetailProgress GetProgress(this OrderDetail orderDetail)
        {
            // Đảm bảo đã Include ShoppingLists và ThreePointChecks (làm ở Repository)
            var allShoppingLists = orderDetail.ShoppingLists?.ToList() ?? new List<ShoppingList>();
            var totalPallets = orderDetail.TotalPallet > 0
                ? orderDetail.TotalPallet
                : allShoppingLists.Select(sl => sl.PalletNo).Distinct().Count();

            if (totalPallets == 0)
            {
                return new OrderDetailProgress
                {
                    UId = orderDetail.UId,
                    PartNo = orderDetail.PartNo,
                    Quantity = orderDetail.Quantity,
                    TotalPallet = 0,
                    Warehouse = orderDetail.Warehouse,
                    ContNo = orderDetail.ContNo,
                    BookContStatus = orderDetail.BookContStatus,
                    CollectPercent = 0,
                    PreparePercent = 0,
                    LoadingPercent = 0, // vẫn để field này để không lỗi map model
                    CurrentStage = "NotStarted",
                    Status = GetBookContStatusText(orderDetail.BookContStatus)
                };
            }

            // % Collect
            var collectedPallets = allShoppingLists
                .Where(sl => sl.CollectionStatus == 1 || sl.CollectedDate.HasValue)
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();
            var collectPercent = (double)collectedPallets / totalPallets * 100;

            // % Prepare: unique pallet có ít nhất 1 SL có ThreePointChecks
            var preparedPallets = allShoppingLists
                .Where(sl => sl.ThreePointChecks.Any())
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();
            var preparePercent = (double)preparedPallets / totalPallets * 100;

            // Không tính phần trăm loading nữa
            string currentStage = DetermineCurrentStage(collectPercent, preparePercent, orderDetail.BookContStatus);
            string statusText = GetBookContStatusText(orderDetail.BookContStatus);

            return new OrderDetailProgress
            {
                UId = orderDetail.UId,
                PartNo = orderDetail.PartNo,
                Quantity = orderDetail.Quantity,
                TotalPallet = totalPallets,
                Warehouse = orderDetail.Warehouse,
                ContNo = orderDetail.ContNo,
                BookContStatus = orderDetail.BookContStatus,
                CollectPercent = Math.Round(collectPercent, 1),
                PreparePercent = Math.Round(preparePercent, 1),
                LoadingPercent = 0, // bỏ phần trăm loading
                CurrentStage = currentStage,
                Status = statusText
            };
        }

        private static string DetermineCurrentStage(double collect, double prepare, short bookContStatus)
        {
            if (collect < 100) return "Collecting";
            if (prepare < 100) return "Preparing";
            return bookContStatus == 1 ? "Completed" : "WaitingForLoading";
        }

        private static string GetBookContStatusText(short status)
        {
            return status switch
            {
                0 => "Chưa Loading lên cont",
                1 => "Đã Loading lên cont",
                _ => "Không xác định"
            };
        }
    }
}
