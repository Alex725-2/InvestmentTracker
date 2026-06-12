using InvestmentTracker.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace InvestmentTracker.Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {

        //Датасеты
        public DbSet<Broker> Brokers { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<AssetType> AssetTypes { get; set; }
        public DbSet<Security> Securities { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<PortfolioItem> PortfolioItems { get; set; }
        public DbSet<Quote> Quotes { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            if (Database.IsSqlite())
            {
                // Заменяем nvarchar(max) на TEXT для Identity-таблиц
                builder.Entity<ApplicationUser>(entity =>
                {
                    entity.Property(e => e.ConcurrencyStamp).HasColumnType("TEXT");
                    entity.Property(e => e.SecurityStamp).HasColumnType("TEXT");
                });
                builder.Entity<IdentityRole>(entity =>
                {
                    entity.Property(e => e.ConcurrencyStamp).HasColumnType("TEXT");
                });
                builder.Entity<IdentityUserLogin<string>>(entity =>
                {
                    entity.Property(e => e.ProviderKey).HasColumnType("TEXT");
                    entity.Property(e => e.LoginProvider).HasColumnType("TEXT");
                });
                builder.Entity<IdentityUserToken<string>>(entity =>
                {
                    entity.Property(e => e.Name).HasColumnType("TEXT");
                    entity.Property(e => e.LoginProvider).HasColumnType("TEXT");
                });
            }
            base.OnModelCreating(builder);

            // Шаманство, пытаюсь решаить проблему с 500 ошибкой на странице
            builder.Entity<PortfolioItem>().ToTable("PortfolioItem");

            // Настройка точности для decimal
            builder.Entity<Broker>()
                .Property(b => b.DefaultCommissionRate)
                .HasColumnType("decimal(18,4)");

            builder.Entity<Account>()
                .Property(a => a.CommissionRate)
                .HasColumnType("decimal(18,4)");

            builder.Entity<PortfolioItem>()
                .Property(p => p.Quantity)
                .HasColumnType("decimal(18,6)");

            builder.Entity<PortfolioItem>()
                .Property(p => p.AveragePurchasePrice)
                .HasColumnType("decimal(18,4)");

            builder.Entity<Transaction>()
                .Property(t => t.Price)
                .HasColumnType("decimal(18,4)");

            builder.Entity<Transaction>()
                .Property(t => t.Commission)
                .HasColumnType("decimal(18,4)");

            // Отключаем каскадное удаление для связи PortfolioItem -> User
            builder.Entity<PortfolioItem>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction); // или DeleteBehavior.Restrict

            builder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Quote>(entity =>
            {
                entity.Property(q => q.Price).HasColumnType("decimal(18,4)");
            });
        }
    }
}
