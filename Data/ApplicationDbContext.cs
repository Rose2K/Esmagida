using Microsoft.EntityFrameworkCore;
using EsmaGida.Models;

namespace EsmaGida.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Veritabanı tablolarını temsil eden DbSet'ler
        public DbSet<User> User { get; set; }
        public DbSet<Product> products { get; set; }
        public DbSet<Certificate> certificates { get; set; }
        public DbSet<Cart> carts { get; set; }
        public DbSet<CartItem> cart_items { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // "Product" entity'si "products" tablosunu kullanacak şekilde ayarlama
            modelBuilder.Entity<Product>().ToTable("products");
            
            // "Certificate" entity'si "certificates" tablosunu kullanacak şekilde ayarlama
            modelBuilder.Entity<Certificate>().ToTable("certificates");
            
            // "Cart" entity'si "carts" tablosunu kullanacak şekilde ayarlama
            modelBuilder.Entity<Cart>().ToTable("carts");
            
            // "CartItem" entity'si "cart_items" tablosunu kullanacak şekilde ayarlama
            modelBuilder.Entity<CartItem>().ToTable("cart_items");
            
            // Relationship configurations
            modelBuilder.Entity<CartItem>()
                .HasOne<Cart>()
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.cart_id)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<CartItem>()
                .HasOne<Product>()
                .WithMany()
                .HasForeignKey(ci => ci.product_id)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Cart>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // User tablosu için konfigürasyonlar
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.email).IsRequired();
                entity.Property(e => e.password).IsRequired();
                entity.Property(e => e.name).IsRequired();
                entity.Property(e => e.surname).IsRequired();
                entity.Property(e => e.Role).HasDefaultValue("User");
            });
        }
    }
}