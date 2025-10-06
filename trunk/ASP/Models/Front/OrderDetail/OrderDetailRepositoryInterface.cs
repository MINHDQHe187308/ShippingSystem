namespace ASP.Models.Front
{
    public interface OrderDetailRepositoryInterface
    {
        Task<List<OrderDetail>> GetOrderDetailsByOrderId(Guid orderId);
    }
}
