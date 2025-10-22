// File: ASP.DTO.DensoDTO/OrderDetailExtensions.cs (Cập nhật logic progress dùng PalletStatus enum mapping và BookContStatus)
using ASP.DTO.DensoDTO;
using ASP.Models.Front;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASP.DTO.DensoDTO
{
    public static class OrderDetailExtensions
    {
        public static OrderDetailProgress GetProgress(this OrderDetail orderDetail)
        {
            var allShoppingLists = orderDetail.ShoppingLists?.ToList() ?? new List<ShoppingList>();
            var totalPallets = orderDetail.TotalPallet > 0
                ? orderDetail.TotalPallet
                : allShoppingLists.Select(sl => sl.PalletNo).Distinct().Count();

            if (totalPallets == 0)
            {
                return new OrderDetailProgress
                {
                    UId = orderDetail.UId,
                    PartNo = orderDetail.PartNo ?? string.Empty,
                    Quantity = orderDetail.Quantity,
                    TotalPallet = 0,
                    Warehouse = orderDetail.Warehouse ?? string.Empty,
                    ContNo = orderDetail.ContNo,
                    BookContStatus = orderDetail.BookContStatus,
                    CollectPercent = 0,
                    PreparePercent = 0,
                    LoadingPercent = 0,
                    CurrentStage = "NotStarted",
                    Status = GetBookContStatusText(orderDetail.BookContStatus)
                };
            }

            // % Collect: PLStatus == Collected (1)
            var collectedPallets = allShoppingLists
                .Where(sl => sl.PLStatus == (short)CollectionStatusEnumDTO.Collected)
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();
            var collectPercent = (double)collectedPallets / totalPallets * 100;

            // % Prepare: PLStatus == Exported (2, ThreePointCheck)
            var preparedPallets = allShoppingLists
                .Where(sl => sl.PLStatus == (short)CollectionStatusEnumDTO.Exported)
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();
            var preparePercent = (double)preparedPallets / totalPallets * 100;

            // % Loading: PLStatus == Delivered (3)
            var loadedPallets = allShoppingLists
                .Where(sl => sl.PLStatus == (short)CollectionStatusEnumDTO.Delivered)
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();
            var loadingPercent = (double)loadedPallets / totalPallets * 100;

            string currentStage = DetermineCurrentStage(collectPercent, preparePercent, loadingPercent, orderDetail.BookContStatus);
            string statusText = GetBookContStatusText(orderDetail.BookContStatus);

            return new OrderDetailProgress
            {
                UId = orderDetail.UId,
                PartNo = orderDetail.PartNo ?? string.Empty,
                Quantity = orderDetail.Quantity,
                TotalPallet = totalPallets,
                Warehouse = orderDetail.Warehouse ?? string.Empty,
                ContNo = orderDetail.ContNo,
                BookContStatus = orderDetail.BookContStatus,
                CollectPercent = Math.Round(collectPercent, 1),
                PreparePercent = Math.Round(preparePercent, 1),
                LoadingPercent = Math.Round(loadingPercent, 1),
                CurrentStage = currentStage,
                Status = statusText
            };
        }

        private static string DetermineCurrentStage(double collect, double prepare, double loading, short bookContStatus)
        {
            if (collect < 100) return "Collecting";
            if (prepare < 100) return "Preparing";
            if (loading < 100) return "Loading";
            return bookContStatus == (short)BookingStatusEnumDTO.Exported ? "Completed" : "WaitingForBookCont";
        }

        private static string GetBookContStatusText(short status)
        {
            return status switch
            {
                (short)BookingStatusEnumDTO.None => "Chưa Book Cont",
                (short)BookingStatusEnumDTO.Exported => "Đã Book Cont",
                _ => "Không xác định"
            };
        }
    }
}