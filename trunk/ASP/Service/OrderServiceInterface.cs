namespace ASP.Service
{
    public interface OrderServiceInterface
    {
        Task SyncOrdersAsync(bool forceSyncAll = false);
    }
}