using CryptoProject.Data;
using CryptoProject.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CryptoProject.SeedDatabase
{
    public class SeedDb : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public SeedDb(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedDb>>();
            try
            {
                logger.LogInformation("Applying Crypto_Db Migration!");
                await context.Database.EnsureCreatedAsync();
                await context.Database.MigrateAsync(cancellationToken: cancellationToken);
                logger.LogInformation("Crypto_Db Migration Successful!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to apply Crypto_Db Migration!");
            }
            var userManager = scope.ServiceProvider.GetService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetService<RoleManager<Role>>();
            try
            {
                logger.LogInformation("Seeding Crypto_Db Data!");
                await SeedIdentity.SeedAsync(userManager, roleManager);
                logger.LogInformation("Seeding Crypto_Db Successful!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to execute Crypto_Db Data Seeding!");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
