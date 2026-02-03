using HighSpiritApp.Repositories;
using HighSpiritApp.Repositories.Interfaces;
using HighSpiritApp.Services;
using HighSpiritApp.Services.Interfaces;

namespace HighSpiritApp.Extensions
{
    /// <summary>
    /// Extension methods for service registration
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all application repositories with the DI container
        /// </summary>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IMembershipRepository, MembershipRepository>();
            services.AddScoped<IBoxingRepository, BoxingRepository>();

            return services;
        }

        /// <summary>
        /// Registers all application services with the DI container
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IMembershipService, MembershipService>();
            services.AddScoped<IBoxingService, BoxingService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
