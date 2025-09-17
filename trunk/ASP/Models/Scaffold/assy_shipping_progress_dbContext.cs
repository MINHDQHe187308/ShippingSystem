using System;
using System.Collections.Generic;
using ASP.Models.ASPModel;
using ASP.Models.Front;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ASP.Models.Scaffold
{
//    public partial class assy_shipping_progress_dbContext : DbContext
//    {
//        public assy_shipping_progress_dbContext()
//        {
//        }

//        public assy_shipping_progress_dbContext(DbContextOptions<assy_shipping_progress_dbContext> options)
//            : base(options)
//        {
//        }

//        public virtual DbSet<AspNetRole> AspNetRoles { get; set; } = null!;
//        public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; } = null!;
//        public virtual DbSet<AspNetUser> AspNetUsers { get; set; } = null!;
//        public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; } = null!;
//        public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; } = null!;
//        public virtual DbSet<AspNetUserRole> AspNetUserRoles { get; set; } = null!;
//        public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; } = null!;
//        public virtual DbSet<Customer> Customers { get; set; } = null!;
//        public virtual DbSet<DelayHistory> DelayHistories { get; set; } = null!;
//        public virtual DbSet<LeadtimeMaster> LeadtimeMasters { get; set; } = null!;
//        public virtual DbSet<Log> Logs { get; set; } = null!;
//        public virtual DbSet<Menu> Menus { get; set; } = null!;
//        public virtual DbSet<Order> Orders { get; set; } = null!;
//        public virtual DbSet<OrderDetail> OrderDetails { get; set; } = null!;
//        public virtual DbSet<ShoppingList> ShoppingLists { get; set; } = null!;
//        public virtual DbSet<ThemeOption> ThemeOptions { get; set; } = null!;
//        public virtual DbSet<ThreePointCheck> ThreePointChecks { get; set; } = null!;

//        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        {
//            if (!optionsBuilder.IsConfigured)
//            {
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
//                optionsBuilder.UseSqlServer("Server=10.73.131.12;Database=assy_shipping_progress_db;User Id=dev;Password=@dmin123456;TrustServerCertificate=True");
//            }
//        }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            modelBuilder.Entity<AspNetRole>(entity =>
//            {
//                entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
//                    .IsUnique()
//                    .HasFilter("([NormalizedName] IS NOT NULL)");

//                entity.Property(e => e.Content).HasColumnType("ntext");

//                entity.Property(e => e.DefaultRole)
//                    .IsRequired()
//                    .HasDefaultValueSql("(CONVERT([bit],(0)))");

//                entity.Property(e => e.Name).HasMaxLength(256);

//                entity.Property(e => e.NormalizedName).HasMaxLength(256);
//            });

//            modelBuilder.Entity<AspNetRoleClaim>(entity =>
//            {
//                entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

//                entity.HasOne(d => d.Role)
//                    .WithMany(p => p.AspNetRoleClaims)
//                    .HasForeignKey(d => d.RoleId);
//            });

//            modelBuilder.Entity<AspNetUser>(entity =>
//            {
//                entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

//                entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
//                    .IsUnique()
//                    .HasFilter("([NormalizedUserName] IS NOT NULL)");

//                entity.Property(e => e.Email).HasMaxLength(256);

//                entity.Property(e => e.FullName).HasMaxLength(50);

//                entity.Property(e => e.NormalizedEmail).HasMaxLength(256);

//                entity.Property(e => e.NormalizedUserName).HasMaxLength(256);

//                entity.Property(e => e.UserName).HasMaxLength(256);
//            });

//            modelBuilder.Entity<AspNetUserClaim>(entity =>
//            {
//                entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

//                entity.HasOne(d => d.User)
//                    .WithMany(p => p.AspNetUserClaims)
//                    .HasForeignKey(d => d.UserId);
//            });

//            modelBuilder.Entity<AspNetUserLogin>(entity =>
//            {
//                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

//                entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

//                entity.HasOne(d => d.User)
//                    .WithMany(p => p.AspNetUserLogins)
//                    .HasForeignKey(d => d.UserId);
//            });

//            modelBuilder.Entity<AspNetUserRole>(entity =>
//            {
//                entity.HasKey(e => new { e.UserId, e.RoleId });

//                entity.HasIndex(e => e.RoleId, "IX_AspNetUserRoles_RoleId");

//                entity.HasOne(d => d.Role)
//                    .WithMany(p => p.AspNetUserRoles)
//                    .HasForeignKey(d => d.RoleId);

//                entity.HasOne(d => d.User)
//                    .WithMany(p => p.AspNetUserRoles)
//                    .HasForeignKey(d => d.UserId);
//            });

//            modelBuilder.Entity<AspNetUserToken>(entity =>
//            {
//                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

//                entity.HasOne(d => d.User)
//                    .WithMany(p => p.AspNetUserTokens)
//                    .HasForeignKey(d => d.UserId);
//            });

//            modelBuilder.Entity<Customer>(entity =>
//            {
//                entity.HasKey(e => e.CustomerCode);

//                entity.Property(e => e.CustomerCode).HasMaxLength(5);

//                entity.Property(e => e.CreateBy).HasMaxLength(10);

//                entity.Property(e => e.CustomerName).HasMaxLength(20);

//                entity.Property(e => e.Descriptions).HasMaxLength(255);

//                entity.Property(e => e.UpdateBy).HasMaxLength(10);
//            });

//            modelBuilder.Entity<DelayHistory>(entity =>
//            {
//                entity.HasKey(e => e.Uid);

//                entity.Property(e => e.Uid).ValueGeneratedNever();

//                entity.Property(e => e.Reason).HasMaxLength(255);
//            });

//            modelBuilder.Entity<LeadtimeMaster>(entity =>
//            {
//                entity.HasKey(e => new { e.CustomerCode, e.TransCd });

//                entity.Property(e => e.CustomerCode).HasMaxLength(5);

//                entity.Property(e => e.TransCd).HasMaxLength(3);

//                entity.Property(e => e.CreateBy).HasMaxLength(10);

//                entity.Property(e => e.UpdateBy).HasMaxLength(10);
//            });

//            modelBuilder.Entity<Log>(entity =>
//            {
//                entity.Property(e => e.Id).HasColumnName("ID");

//                entity.Property(e => e.Author).HasMaxLength(500);

//                entity.Property(e => e.Content).HasColumnType("ntext");

//                entity.Property(e => e.Ip)
//                    .HasMaxLength(50)
//                    .HasColumnName("IP");

//                entity.Property(e => e.LogType).HasMaxLength(500);
//            });

//            modelBuilder.Entity<Menu>(entity =>
//            {
//                entity.Property(e => e.Id).HasColumnName("ID");

//                entity.Property(e => e.Content).HasColumnType("ntext");

//                entity.Property(e => e.Description).HasMaxLength(100);

//                entity.Property(e => e.Language).HasMaxLength(20);

//                entity.Property(e => e.Name).HasMaxLength(50);
//            });

//            modelBuilder.Entity<Order>(entity =>
//            {
//                entity.HasKey(e => e.Uid);

//                entity.Property(e => e.Uid).ValueGeneratedNever();

//                entity.Property(e => e.CreateBy).HasMaxLength(10);

//                entity.Property(e => e.CustomerCode).HasMaxLength(5);

//                entity.Property(e => e.PartList).HasMaxLength(255);

//                entity.Property(e => e.PoorderId)
//                    .HasMaxLength(50)
//                    .HasColumnName("POOrderId");

//                entity.Property(e => e.TransCd).HasMaxLength(3);

//                entity.Property(e => e.UpdateBy).HasMaxLength(10);
//            });

//            modelBuilder.Entity<OrderDetail>(entity =>
//            {
//                entity.HasKey(e => e.Uid);

//                entity.Property(e => e.Uid).ValueGeneratedNever();

//                entity.Property(e => e.Oid).HasColumnName("OId");

//                entity.Property(e => e.PartNo).HasMaxLength(15);

//                entity.Property(e => e.Warehouse).HasMaxLength(10);
//            });

//            modelBuilder.Entity<ShoppingList>(entity =>
//            {
//                entity.HasKey(e => e.Uid);

//                entity.Property(e => e.Uid).ValueGeneratedNever();

//                entity.Property(e => e.Odid).HasColumnName("ODid");
//            });

//            modelBuilder.Entity<ThemeOption>(entity =>
//            {
//                entity.Property(e => e.Id).HasColumnName("ID");

//                entity.Property(e => e.Language).HasMaxLength(1000);

//                entity.Property(e => e.Name).HasMaxLength(500);

//                entity.Property(e => e.TypeData).HasMaxLength(200);

//                entity.Property(e => e.Value).HasColumnType("ntext");
//            });

//            modelBuilder.Entity<ThreePointCheck>(entity =>
//            {
//                entity.HasKey(e => e.Uid);

//                entity.Property(e => e.Uid).ValueGeneratedNever();

//                entity.Property(e => e.CasemarkQrContent).HasMaxLength(255);

//                entity.Property(e => e.PalletMarkQrContent).HasMaxLength(125);

//                entity.Property(e => e.PalletNoQrContent).HasMaxLength(50);

//                entity.Property(e => e.Spid).HasColumnName("SPid");
//            });

//            OnModelCreatingPartial(modelBuilder);
//        }

//        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    //}
}
