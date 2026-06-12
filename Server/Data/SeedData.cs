using Microsoft.AspNetCore.Identity;
using InvestmentTracker.Server.Models;
using InvestmentTracker.Server.Data;

namespace InvestmentTracker.Server.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Создаём роль Admin, если её нет
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Создаём тестового пользователя-админа
            var adminEmail = "admin@example.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FullName = "Admin User"
                };
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Валюты
            if (!context.Currencies.Any())
            {
                context.Currencies.AddRange(
                    new Currency { Code = "RUR", Name = "Российский рубль" },
                    new Currency { Code = "USD", Name = "Доллар США" },
                    new Currency { Code = "EUR", Name = "Евро" },
                    new Currency { Code = "CNY", Name = "Юань" }
                );
                await context.SaveChangesAsync();
            }

            // Типы активов
            if (!context.AssetTypes.Any())
            {
                context.AssetTypes.AddRange(
                    new AssetType { Name = "Акция" },
                    new AssetType { Name = "Облигация" },
                    new AssetType { Name = "ПИФ" },
                    new AssetType { Name = "ETF" }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}