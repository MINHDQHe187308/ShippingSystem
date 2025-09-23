using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public interface CustomerRepositoryInterface
    {
        Task<List<Customer>> GetAllCustomers();
        Task<Customer?> GetCustomerByCode(string customerCode);
        Task<bool> CreateCustomer(Customer customer);
        Task<(bool Success, string Message)> UpdateCustomerByCode(string code, Customer customer);
        Task<bool> RemoveCustomerByCode(string code);
    }
}
