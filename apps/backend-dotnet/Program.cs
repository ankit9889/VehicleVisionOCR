using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using VehicleVisionOCR.Application;
using VehicleVisionOCR.Infrastructure;
using VehicleVisionOCR.Scanner.Core;
using VehicleVisionOCR.Scanner.ZebraWindows;
using VehicleVisionOCR.Scanner.Emulator;
using VehicleVisionOCR.Scanner.MobileWeb;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.OCR.Core;
using VehicleVisionOCR.OCR.Tesseract;
using VehicleVisionOCR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using VehicleVisionOCR.Backend.Hubs;

// Configure Serilog for early logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    // Fix for production permissions: Set CurrentDirectory to AppData so DB and logs can be written
    var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VehicleVisionOCR");
    if (!Directory.Exists(appData))
        Directory.CreateDirectory(appData);
    Environment.CurrentDirectory = appData;

    Log.Information("Starting VehicleVisionOCR Backend...");

    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? AppContext.BaseDirectory;
    var exeDir = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = exeDir,
        WebRootPath = Path.Combine(exeDir, "wwwroot")
    });

    // Replace built-in logging with Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
        .WriteTo.File("Logs/backend-.txt", rollingInterval: RollingInterval.Day, formatProvider: System.Globalization.CultureInfo.InvariantCulture));

    // Add API Controllers and Swagger
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddSignalR();

    builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = "VehicleVisionSession";
            options.ExpireTimeSpan = System.TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return System.Threading.Tasks.Task.CompletedTask;
            };
        });

    // Register Clean Architecture Layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Register Scanner Framework and Plugins
    builder.Services.AddScannerFramework();
    builder.Services.AddZebraScannerPlugin();
    
    // Register Emulator Scanner
    builder.Services.AddSingleton<IScannerPlugin, EmulatorScannerPlugin>();
    // Register Mobile Web Scanner
    builder.Services.AddSingleton<IScannerPlugin, MobileScannerPlugin>();

    // Register Validation & OCR Correction Framework Infrastructure
    builder.Services.Configure<VehicleVisionOCR.Backend.Services.OcrCorrection.OcrCorrectionOptions>(
        builder.Configuration.GetSection(VehicleVisionOCR.Backend.Services.OcrCorrection.OcrCorrectionOptions.Section));

    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.IOcrCorrectionCoordinator, VehicleVisionOCR.Backend.Services.OcrCorrection.OcrCorrectionCoordinator>();
    
    // VIN Pipeline
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.IVinNormalizer, VehicleVisionOCR.Backend.Services.OcrCorrection.VinServices.VinNormalizer>();
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.IVinCandidateGenerator, VehicleVisionOCR.Backend.Services.OcrCorrection.VinServices.VinCandidateGenerator>();
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.IVinScoringService, VehicleVisionOCR.Backend.Services.OcrCorrection.VinServices.VinScoringService>();
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.IOcrCorrectionStrategy, VehicleVisionOCR.Backend.Services.OcrCorrection.Strategies.VinCorrectionStrategy>();
    
    // Color Pipeline
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.IOcrCorrectionStrategy, VehicleVisionOCR.Backend.Services.OcrCorrection.Strategies.ColorCorrectionStrategy>();

    // Mock Repository for WMI, DB Repository for Colors
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.IWmiRepository, VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.MockWmiRepository>();
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.IColorRepository, VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.DbColorRepository>();

    // Register OCR Framework and Tesseract Plugin
    builder.Services.AddOcrFramework();
    builder.Services.AddTesseractOcrPlugin();

    // Add CORS for React Frontend/Electron
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend",
            policy =>
            {
                policy.SetIsOriginAllowed(origin => true) // Required for credentials
                      .AllowCredentials()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
    });

    // Register Backend Specific Services
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.IValidationEngine, VehicleVisionOCR.Backend.Services.ValidationEngine>();
    
    // Register Phase 7 Vision Integration Services
    builder.Services.AddSingleton(System.Threading.Channels.Channel.CreateUnbounded<VehicleVisionOCR.Backend.Services.VisionIntegration.Models.ComparisonResult>());
    builder.Services.AddSingleton(provider => provider.GetRequiredService<System.Threading.Channels.Channel<VehicleVisionOCR.Backend.Services.VisionIntegration.Models.ComparisonResult>>().Writer);
    builder.Services.AddSingleton(provider => provider.GetRequiredService<System.Threading.Channels.Channel<VehicleVisionOCR.Backend.Services.VisionIntegration.Models.ComparisonResult>>().Reader);
    builder.Services.AddSingleton<VehicleVisionOCR.Backend.Services.VisionIntegration.MigrationStatisticsService>();
    builder.Services.AddHostedService<VehicleVisionOCR.Backend.Services.VisionIntegration.MigrationStatisticsBackgroundService>();
    
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.VisionIntegration.PipelineComparisonService>();
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.VisionIntegration.ResultDecisionService>();
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.VisionIntegration.LegacyPipelineAdapter>();
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.VisionIntegration.ModernVisionPipeline>();
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.VisionIntegration.Interfaces.IVisionPipelineCoordinator, VehicleVisionOCR.Backend.Services.VisionIntegration.VisionPipelineCoordinator>();
    builder.Services.AddScoped<VehicleVisionOCR.Backend.Services.VisionIntegration.Interfaces.IScanProcessingEngine, VehicleVisionOCR.Backend.Services.VisionIntegration.ScanProcessingEngine>();

    // Register Artifact Recovery Engine and Strategies
    builder.Services.AddScoped<VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces.IGeometricAnalyzer, VehicleVisionOCR.Application.Vision.ArtifactRecovery.GeometricAnalyzer>();
    builder.Services.AddScoped<VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces.IArtifactRepairStrategy, VehicleVisionOCR.Application.Vision.ArtifactRecovery.Strategies.NoiseRemovalStrategy>();
    builder.Services.AddScoped<VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces.IArtifactRepairStrategy, VehicleVisionOCR.Application.Vision.ArtifactRecovery.Strategies.DuplicateSuppressionStrategy>();
    builder.Services.AddScoped<VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces.IArtifactRepairStrategy, VehicleVisionOCR.Application.Vision.ArtifactRecovery.Strategies.InsertionOutlierStrategy>();
    builder.Services.AddScoped<VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces.IArtifactRepairStrategy, VehicleVisionOCR.Application.Vision.ArtifactRecovery.Strategies.SplitCharacterMergeStrategy>();
    builder.Services.AddScoped<VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces.IArtifactRepairStrategy, VehicleVisionOCR.Application.Vision.ArtifactRecovery.Strategies.GeometricSubstitutionStrategy>();
    builder.Services.AddScoped<VehicleVisionOCR.Application.Vision.ArtifactRecovery.Interfaces.IOcrArtifactRecoveryEngine, VehicleVisionOCR.Application.Vision.ArtifactRecovery.OcrArtifactRecoveryEngine>();

    builder.Services.AddSingleton<VehicleVisionOCR.Backend.BackgroundServices.ImageProcessingQueue>();
    builder.Services.AddHostedService<VehicleVisionOCR.Backend.BackgroundServices.ScanWorkflowService>();
    builder.Services.AddHostedService<VehicleVisionOCR.Backend.BackgroundServices.SystemMaintenanceService>();

    var app = builder.Build();

    // Ensure database is created/migrated (offline SQLite)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        db.Database.ExecuteSqlRaw("PRAGMA synchronous=NORMAL;");
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;");

        // Create VehicleColors table if not exists
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""VehicleColors"" (
                ""Id"" TEXT NOT NULL CONSTRAINT ""PK_VehicleColors"" PRIMARY KEY,
                ""Name"" TEXT NOT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );
        ");

        // Seed Default Colors if not present
        var requiredColors = new[] { 
            "ATHLETIC BLUE METALLIC", "PEARL IGNEOUS BLACK", "MATTE AXIS GREY METALLIC", 
            "PEARL SIREN BLUE", "SPORTS RED", "PEARL SNOW WHITE", "RACING GREEN", 
            "ALPINE WHITE", "DEEP SAPPHIRE BLUE METALLIC" 
        };

        foreach (var color in requiredColors)
        {
            if (!db.VehicleColors.Any(c => c.Name == color))
            {
                db.VehicleColors.Add(new VehicleVisionOCR.Domain.Entities.VehicleColor { Name = color });
            }
        }
        db.SaveChanges();
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Serve React Frontend from wwwroot for Mobile Scanner access in production
    var wwwrootPath = Path.Combine(exeDir, "wwwroot");
    if (Directory.Exists(wwwrootPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwrootPath),
            OnPrepareResponse = ctx =>
            {
                if (ctx.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                }
            }
        });
    }
    else
    {
        Log.Warning($"wwwroot directory not found at {wwwrootPath}");
    }

    app.UseCors("AllowFrontend");

    // Local authentication/authorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<ScannerHub>("/hubs/scanner");
    
    // Explicitly map fallback to the physical index.html file
    app.MapFallback(async context => 
    {
        var indexPath = Path.Combine(wwwrootPath, "index.html");
        if (File.Exists(indexPath))
        {
            context.Response.ContentType = "text/html";
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            await context.Response.SendFileAsync(indexPath);
        }
        else

        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Frontend not deployed. index.html not found.");
        }
    });

    app.Run();
}
catch (System.Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
