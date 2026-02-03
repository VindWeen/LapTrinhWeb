using Microsoft.EntityFrameworkCore;
using LapTrinhWeb.Models;
using System.Data.Common;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Diagnostics;

namespace LapTrinhWeb.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<ArticleCategories> ArticleCategories { get; set; }
        public DbSet<Articles> Articles { get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Coupons> Coupons { get; set; }
        public DbSet<MasterColors> MasterColors { get; set; }
        public DbSet<MasterSizes> MasterSizes { get; set; }
        public DbSet<Notifications> Notifications { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; }
        public DbSet<OrderDetails> OrderDetails { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<ProductImage> ProductImage { get; set; }
        public DbSet<ProductPromotions> ProductPromotions { get; set; }
        public DbSet<ProductReviews> ProductReviews { get; set; }
        public DbSet<Products> Products { get; set; }
        public DbSet<ProductVariants> ProductVariants { get; set; }
        public DbSet<PromotionCoditions> PromotionConditions { get; set; }
        public DbSet<Promotions> Promotions { get; set; }
        public DbSet<UserAddresses> UserAddresses { get; set; }
        public DbSet<Users> Users { get; set; }
    }
}