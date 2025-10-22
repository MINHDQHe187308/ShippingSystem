using System;
using System.Collections.Generic;

namespace ASP.DTO.DensoDTO
{
    
    public class OrderDTO
    {
        public Guid PcOrderId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public DateTime ShippingDate { get; set; }
        public int OrderStatus { get; set; }
        public string TranCd { get; set; } = string.Empty;
        public int TotalPallet { get; set; }
        public DateTime OrderCreatedDate { get; set; }
        public List<OrderDetailDTO> OrderDetails { get; set; } = new List<OrderDetailDTO>();
    }

   
    public class OrderDetailDTO
    {
        public long BookContDetailId { get; set; }
        public long ShippingId { get; set; }
        public int ContNo { get; set; }
        public string PartNo { get; set; } = string.Empty;
        public int PalletSize { get; set; }
        public int Quantity { get; set; }
        public int TotalPallet { get; set; }
        public int Status { get; set; }
        public string Warehouse { get; set; } = string.Empty;
        public int BookContStatus { get; set; }
        public List<ShoppingListDTO> ShoppingLists { get; set; } = new List<ShoppingListDTO>();
    }

    public class ShoppingListDTO
    {
        public long CollectionId { get; set; }
        public long PalletId { get; set; }
        public int PalletNo { get; set; }
        public int PalletStatus { get; set; }
        public DateTime? CollectedDate { get; set; }
        public bool IsThreePointCheck { get; set; } = false;
        public string PlMarkQr { get; set; } = string.Empty;
        public string PlNoQr { get; set; } = string.Empty;
        public string CasemarkQr { get; set; } = string.Empty;
        public DateTime? ThreePointCheckTime { get; set; }
    }

    // Enum DTOs (tận dụng từ API definitions, thêm vào namespace để mapping)
    public enum OrderStatusEnumDTO
    {
        Available = 0,
        Cancel = 1,
        Revised = 2,
        Deny = 3
    }

    public enum BookingStatusEnumDTO
    {
        None = 0,
        New = 1,
        Ordered = 2,
        Collected = 3,
        Exported = 4,
        Canceled = -1
    }

    public enum CollectionStatusEnumDTO
    {
        None = 0,
        Collected = 1,
        Exported = 2,  // ThreePointCheck
        Delivered = 3,  // Loaded to cont
        Canceled = 4
    }
}