using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VehicleVisionOCR.Application.Interfaces;
using VehicleVisionOCR.Application.Interfaces.Repositories;
using VehicleVisionOCR.Infrastructure.BackgroundServices;
using VehicleVisionOCR.Infrastructure.Caching;
using VehicleVisionOCR.Infrastructure.Configuration;
using VehicleVisionOCR.Infrastructure.Persistence;
using VehicleVisionOCR.Infrastructure.Persistence.Repositories;
using VehicleVisionOCR.Infrastructure.Storage;

namespace VehicleVisionOCR.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Configure Settings
            services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
            services.Configure<ApiSettings>(configuration.GetSection(ApiSettings.SectionName));
            services.Configure<ScannerSettings>(configuration.GetSection(ScannerSettings.SectionName));
            services.Configure<OCRSettings>(configuration.GetSection(OCRSettings.SectionName));
            services.Configure<LoggingSettings>(configuration.GetSection(LoggingSettings.SectionName));

            // 2. Configure Database
            var dbSettings = configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>();
            var connectionString = dbSettings?.ConnectionString ?? "Data Source=vehicle_vision.db";

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(connectionString);
                if (dbSettings?.EnableSensitiveDataLogging == true)
                {
                    options.EnableSensitiveDataLogging();
                }
            });

            // 3. Register Repositories
            services.AddScoped<IVehicleRepository, VehicleRepository>();
            services.AddScoped<IScanRepository, ScanRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ISettingsRepository, SettingsRepository>();
            services.AddScoped<IPendingSyncRepository, PendingSyncRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 4. Register Services
            services.AddSingleton<IImageStorageService, LocalImageStorageService>();
            
            // 5. Register Caching
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();

            // 6. Register Background Workers
            services.AddHostedService<SyncBackgroundService>();

            return services;
        }
    }
}
