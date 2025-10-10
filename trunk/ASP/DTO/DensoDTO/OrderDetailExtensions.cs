using ASP.DTO.DensoDTO;
using ASP.Models.ASPModel;
using ASP.Models.Front;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASP.DTO.DensoDTO
{
    public static class OrderDetailExtensions
    {
        public static OrderDetailProgress GetProgress(this OrderDetail orderDetail)
        {
            // Đảm bảo đã Include ShoppingLists và ThreePointCheck (làm ở Repository)
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
                    LoadingPercent = 0,
                    CurrentStage = "NotStarted",
                    Status = GetBookContStatusText(orderDetail.BookContStatus)
                };
            }

            // % Collect: pallet có PLStatus >= 1 (collected)
            var collectedPallets = allShoppingLists
                .Where(sl => sl.PLStatus >= 1)  // Dùng PLStatus cho collected (giả sử 1 = collected)
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();
            var collectPercent = (double)collectedPallets / totalPallets * 100;

            // % Prepare: pallet có PLStatus >= 2 (prepared, quét ba điểm) - ưu tiên PLStatus thay vì ThreePointCheck
            var preparedPallets = allShoppingLists
                .Where(sl => sl.PLStatus >= 2)  //  Dùng PLStatus cho prepared (giả sử 2 = prepared)
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();
            var preparePercent = (double)preparedPallets / totalPallets * 100;

            // % Loading: pallet có PLStatus >= 3 (Quet confirm để loaded lên cont)
            var loadedPallets = allShoppingLists
                .Where(sl => sl.PLStatus >= 3)  // Dùng PLStatus cho loading (giả sử 3 = loaded)
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();
            var loadingPercent = (double)loadedPallets / totalPallets * 100;

            string currentStage = DetermineCurrentStage(collectPercent, preparePercent, loadingPercent, orderDetail.BookContStatus);
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
                LoadingPercent = Math.Round(loadingPercent, 1),  // THÊM MỚI: Bao gồm loading percent
                CurrentStage = currentStage,
                Status = statusText
            };
        }

        private static string DetermineCurrentStage(double collect, double prepare, double loading, short bookContStatus)
        {
            if (collect < 100) return "Collecting";
            if (prepare < 100) return "Preparing";
            if (loading < 100) return "Loading";  // THÊM MỚI: Stage cho loading
            return bookContStatus == 1 ? "Completed" : "WaitingForBookCont";
        }

        private static string GetBookContStatusText(short status)
        {
            return status switch
            {
                0 => "Chưa Book Cont",
                1 => "Đã Book Cont",
                _ => "Không xác định"
            };
        }
    }
}