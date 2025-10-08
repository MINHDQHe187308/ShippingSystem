using Microsoft.AspNetCore.SignalR;

namespace ASP.Hubs
{
    public class OrderHub : Hub
    {
        // Server gửi update đến tất cả clients
        public async Task UpdateOrderStatus(string orderUid, int newStatus)
        {
            await Clients.All.SendAsync("OrderStatusUpdated", orderUid, newStatus);
        }
    }
}