using ASP.Models.Admin;
using ASP.Models.Admin.Accounts;
using ASP.Models.Admin.Logs;
using ASP.Models.Admin.Menus;
using ASP.Models.Admin.Roles;
using ASP.Models.Admin.ThemeOptions;
using ASP.Models.Front;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace ASP.Models.ASPModel
{
    public class ASPDbContext : IdentityDbContext<ApplicationUser, Role, string>
    {
        public ASPDbContext(DbContextOptions<ASPDbContext> options) : base(options) { }

        public override int SaveChanges()
        {
            SetModifiedInformation();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            SetModifiedInformation();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void SetModifiedInformation()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                        e.State == EntityState.Added
                        || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                ((BaseEntity)entityEntry.Entity).UpdatedDate = DateTime.Now;

                if (entityEntry.State == EntityState.Added)
                {
                    ((BaseEntity)entityEntry.Entity).CreatedDate = DateTime.Now;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var allEtities = modelBuilder.Model.GetEntityTypes();
            foreach (var entity in allEtities)
            {
                entity.AddProperty("CreatedDate", typeof(DateTime));
                entity.AddProperty("UpdatedDate", typeof(DateTime));
            }

            modelBuilder.Entity<Role>()
                .Property(b => b.DefaultRole)
                .HasDefaultValue(false);
        }

        public DbSet<Log> Logs { get; set; }

        public DbSet<Admin.ThemeOptions.ThemeOption> ThemeOptions { get; set; }

        public DbSet<Menu> Menus { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<DelayHistory> DelayHistory { get; set; }

        public DbSet<LeadtimeMaster> LeadtimeMasters { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<ShoppingList> ShoppingLists { get; set; }

        public DbSet<ThreePointCheck> ThreePointChecks { get; set; } 
        }
}
