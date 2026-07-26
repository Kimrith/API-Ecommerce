using API_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Auth> Auths { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Categories> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Auth Enum Conversions ---
            modelBuilder.Entity<Auth>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Auth>()
                .Property(u => u.Status)
                .HasConversion<string>();

            // --- Product Configurations ---
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Status)
                      .HasConversion<string>();

                // Decimal precision configuration to prevent truncation/precision loss warnings
                entity.Property(p => p.Price)
                      .HasColumnType("decimal(18,2)");

                entity.Property(p => p.DiscountPrice)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired(false);

                // Optional DateTime fields
                entity.Property(p => p.DiscountStartDate)
                      .IsRequired(false);

                entity.Property(p => p.DiscountEndDate)
                      .IsRequired(false);

                entity.Property(p => p.PublishAt)
                      .IsRequired(false);
            });

            // --- Categories Enum Conversions ---
            modelBuilder.Entity<Categories>()
                .Property(c => c.Status)
                .HasConversion<string>();

            // --- Relationships ---

            // User -> Categories (One User creates many Categories)
            modelBuilder.Entity<Categories>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Category -> Products (One Category has many Products)
            modelBuilder.Entity<Categories>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}