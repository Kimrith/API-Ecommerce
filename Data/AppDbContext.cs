using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using API_Ecommerce.Models;
using API_Ecommerce.Data;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // --- Core Authentication & Users ---
        public DbSet<Auth> Auths { get; set; }
        public DbSet<Address> Addresses { get; set; }

        // --- Catalog & Categories ---
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariants> ProductVariants { get; set; }
        public DbSet<Inventory> Inventories { get; set; }

        // --- Orders & Transactions ---
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }

        // --- Marketing & Engagement ---
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Banner> Banners { get; set; }

        // --- System & Notifications ---
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<SellerBakongConfigs> SellerBakongConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 1. AUTH & ENUMS
            // ==========================================
            modelBuilder.Entity<Auth>(entity =>
            {
                entity.Property(u => u.Role)
                      .HasConversion<string>();

                entity.Property(u => u.Status)
                      .HasConversion<string>();
            });

            // ==========================================
            // 2. ADDRESS CONFIGURATION
            // ==========================================
            modelBuilder.Entity<Address>(entity =>
            {
                entity.Property(a => a.AddressType)
                      .HasConversion<string>();

                entity.HasIndex(a => a.UserId, "idx_addresses_user");

                entity.HasOne(a => a.User)
                      .WithMany(u => u.Addresses)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 3. PRODUCT & CATEGORY CONFIGURATIONS
            // ==========================================
            modelBuilder.Entity<Categories>(entity =>
            {
                entity.Property(c => c.Status)
                      .HasConversion<string>();

                // Performance Indexes
                entity.HasIndex(c => c.UserId).HasName("IX_Categories_UserId");
                entity.HasIndex(c => c.Status).HasName("IX_Categories_Status");

                // CreatedBy User
                entity.HasOne(c => c.User)
                      .WithMany()
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // One Category -> Many Products
                entity.HasMany(c => c.Products)
                      .WithOne(p => p.Category)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Status)
                      .HasConversion<string>();

                entity.Property(p => p.Price)
                      .HasColumnType("decimal(18,2)");

                entity.Property(p => p.DiscountPrice)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired(false);

                entity.Property(p => p.DiscountStartDate).IsRequired(false);
                entity.Property(p => p.DiscountEndDate).IsRequired(false);
                entity.Property(p => p.PublishAt).IsRequired(false);

                // Performance Index for joins and counts
                entity.HasIndex(p => p.CategoryId).HasName("IX_Products_CategoryId");

                // Seller relationship
                entity.HasOne(p => p.Seller)
                      .WithMany()
                      .HasForeignKey(p => p.SellerId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Product -> Variants (Cascade Delete)
                entity.HasMany(p => p.Variants)
                      .WithOne(v => v.Product)
                      .HasForeignKey(v => v.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductVariants>(entity =>
            {
                entity.HasOne(v => v.Inventory)
                      .WithOne(i => i.Variant)
                      .HasForeignKey<Inventory>(i => i.VariantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 4. ORDERS & PAYMENTS (Precision & Foreign Keys)
            // ==========================================
            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(o => o.Subtotal)
                      .HasColumnType("decimal(18,2)");

                entity.Property(o => o.TaxAmount)
                      .HasColumnType("decimal(18,2)");

                entity.Property(o => o.ShippingAmount)
                      .HasColumnType("decimal(18,2)");

                entity.Property(o => o.DiscountAmount)
                      .HasColumnType("decimal(18,2)");

                entity.Property(o => o.TotalAmount)
                      .HasColumnType("decimal(18,2)");

                // Customer relationship
                entity.HasOne(o => o.User)
                      .WithMany()
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Shipping Address relationship
                entity.HasOne(o => o.ShippingAddress)
                      .WithMany()
                      .HasForeignKey(o => o.ShippingAddressId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Billing Address relationship
                entity.HasOne(o => o.BillingAddress)
                      .WithMany()
                      .HasForeignKey(o => o.BillingAddressId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount)
                      .HasColumnType("decimal(18,2)");
            });

            // ==========================================
            // 5. COUPONS & DISCOUNTS
            // ==========================================
            modelBuilder.Entity<Coupon>(entity =>
            {
                entity.Property(c => c.DiscountValue)
                      .HasColumnType("decimal(18,2)");

                entity.Property(c => c.MinimumAmount)
                      .HasColumnType("decimal(18,2)");

                entity.Property(c => c.MaximumDiscountAmount)
                      .HasColumnType("decimal(18,2)");
            });

            // ==========================================
            // 6. FAVORITES & REVIEWS (Cascade on Product Delete)
            // ==========================================
            modelBuilder.Entity<Favorite>(entity =>
            {
                // Unique constraint: A user can favorite a product only once
                entity.HasIndex(f => new { f.UserId, f.ProductId }).IsUnique();

                entity.HasOne(f => f.User)
                      .WithMany()
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Enable Cascade Delete when a product is removed
                entity.HasOne(f => f.Product)
                      .WithMany()
                      .HasForeignKey(f => f.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Enable Cascade Delete when a product is removed
                entity.HasOne(r => r.Product)
                      .WithMany()
                      .HasForeignKey(r => r.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("order_items");
                entity.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(oi => oi.TotalPrice).HasColumnType("decimal(18,2)");

                entity.HasOne(oi => oi.Order)
                      .WithMany()
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                      .WithMany()
                      .HasForeignKey(oi => oi.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}