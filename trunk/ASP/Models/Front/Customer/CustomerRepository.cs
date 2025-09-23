using ASP.Models.ASPModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public class CustomerRepository : CustomerRepositoryInterface
    {
        private readonly ASPDbContext _context;
        private readonly ILogger<CustomerRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CustomerRepository(ASPDbContext context, ILogger<CustomerRepository> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Customer>> GetAllCustomers()
        {
            return await _context.Customers
                .OrderBy(c => c.CustomerCode)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerByCode(string customerCode)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerCode == customerCode);
        }

        public async Task<bool> CreateCustomer(Customer customer)
        {
            try
            {
                customer.CustomerCode = customer.CustomerCode.Trim();
                customer.CustomerName = customer.CustomerName.Trim();
                customer.Descriptions = customer.Descriptions?.Trim();

                // Lấy username đang login
                var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";

                customer.CreateBy = currentUser;
                customer.CreatedDate = DateTime.Now;
                customer.UpdatedDate = DateTime.Now;

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return false;
            }
        }

        public async Task<bool> UpdateCustomerByCode(string code, Customer customer)
        {
            try
            {
                var dbCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == code);
                if (dbCustomer == null) return false;

                dbCustomer.CustomerName = customer.CustomerName?.Trim();
                dbCustomer.Descriptions = customer.Descriptions?.Trim();
                dbCustomer.UpdateBy = customer.UpdateBy;
                dbCustomer.UpdatedDate = DateTime.Now;

                _context.Customers.Update(dbCustomer);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer");
                return false;
            }
        }

        public async Task<bool> RemoveCustomerByCode(string code)
        {
            try
            {
                var dbCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == code);
                if (dbCustomer == null) return false;

                _context.Customers.Remove(dbCustomer);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer");
                return false;
            }
        }
    }
}
