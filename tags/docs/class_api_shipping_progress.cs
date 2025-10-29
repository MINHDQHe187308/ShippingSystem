api route: /api/delivery/shippingprogress


public class ResponseDeliveyResult
{
    public int Status { get; set; }
    public string Message { get; set; }
    public List<PcOrderDetail> Data { get; set; }
}

public class Order
{
    public Guid PcOrderId { get; set; }
    public string CustomerCode { get; set; }
    public DateTime ShippingDate { get; set; }
    public int OrderStatus { get; set; }
    public string TranCd { get; set; }
    public int TotalPallet { get; set; }
    public DateTime OrderCreatedDate { get; set; }
    public List<OrderDetails> OrderDetails { get; set; }
}

public class OrderDetails
{
    public long BookContDetailId { get; set; }
    public long ShippingId { get; set; }
    public int ContNo { get; set; }
    public string PartNo { get; set; }
    public int PalletSize { get; set; }
    public int Quantity { get; set; }
    public int TotalPallet { get; set; }
    public int Status { get; set; }
    public string Warehouse { get; set; }
    public int BookContStatus { get; set; }
    public List<ShoppingList> ShoppingLists { get; set; }
}

public class ShoppingList
{
    public long CollectionId { get; set; }
    public long PalletId { get; set; }
    public int PalletNo { get; set; }
    public int PalletStatus { get; set; }
    public DateTime? CollectedDate { get; set; }
    public bool IsThreePointCheck { get; set; } = false;
    public string PlMarkQr { get; set; }
    public string PlNoQr { get; set; }
    public string CasemarkQr { get; set; }
    public DateTime? ThreePointCheckTime { get; set; }
}

/**trang thai don hang**/
public enum OrderStatusEnum
{
    [Description("Order đang khả dụng")]
    Available = 0,
    [Description("Đã bị hủy order")]
    Cancel = 1,
    [Description("Order đã được revise")]
    Revised = 2,
    [Description("Order đã bị từ chối revise")]
    Deny = 3
}

/**trang thai don hang chi tiet**/
public enum BookingStatusEnum
{
    [Description("Chọn")]
    None = 0,
    [Description("Đã chỉ thị LOG")]
    Ordered = 2,
    [Description("Đã thu thập")]
    Collected = 3,
    [Description("Đã xuất")]
    Exported = 4,
    [Description("Đã hủy book cont")]
    Canceled = -1,
    [Description("Mới tạo book cont")]
    New = 1,
}

/**trang thai shopping list**/
public enum ColletionStatusEnum
{
    [Description("Chọn")]
    None,
    [Description("Đã thu thập")]
    Collected,
    [Description("Đã kiểm tra 3 điểm")]
    Exported,
    [Description("Đã đóng lên cont")]
    Delivered,
    [Description("Đã hủy")]
    Canceled
}