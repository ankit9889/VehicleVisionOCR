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

        // Seed Default Colors
        if (!db.VehicleColors.Any())
        {
            db.VehicleColors.AddRange(
                new VehicleVisionOCR.Domain.Entities.VehicleColor { Name = "ATHLETIC BLUE METALLIC" },
                new VehicleVisionOCR.Domain.Entities.VehicleColor { Name = "PEARL IGNEOUS BLACK" },
                new VehicleVisionOCR.Domain.Entities.VehicleColor { Name = "MATTE AXIS GREY METALLIC" },
                new VehicleVisionOCR.Domain.Entities.VehicleColor { Name = "PEARL SIREN BLUE" },
                new VehicleVisionOCR.Domain.Entities.VehicleColor { Name = "SPORTS RED" }
            );
            db.SaveChanges();
        }
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
