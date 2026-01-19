using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using FluentValidation;
using System.Reflection;
using MediatR;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Infrastructure.Data;
using FileManagementSystem.Infrastructure.Repositories;
using FileManagementSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FileManagementSystem.Presentation;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Set up global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        
        try
        {
            // Use AppContext.BaseDirectory to get the directory where the executable is located
            var basePath = AppContext.BaseDirectory;
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            
            var configuration = builder.Build();
            
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.File("logs/filemanager.log", rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();
            
            Log.Logger.Information("Application starting up");
            
            var hostBuilder = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    // Configure DbContext
                    var connectionString = configuration.GetConnectionString("DefaultConnection") 
                        ?? "Data Source=filemanager.db";
                    
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlite(connectionString)
                            .EnableSensitiveDataLogging(false)
                            .EnableServiceProviderCaching());
                    
                    // Add memory cache
                    services.AddMemoryCache();
                    
                    // Repositories and UnitOfWork
                    services.AddScoped<IUnitOfWork, UnitOfWork>();
                    services.AddScoped<IFileRepository, FileRepository>();
                    services.AddScoped<IFolderRepository, CachedFolderRepository>();
                    
                    // Services
                    services.AddScoped<IMetadataService, MetadataService>();
                    services.AddScoped<IStorageService, StorageService>();
                    // Authentication/Authorization services must be Singleton because they're used by Singleton pipeline behaviors
                    services.AddSingleton<IAuthenticationService, AuthenticationService>();
                    services.AddSingleton<IAuthorizationService, AuthorizationService>();
                    
                    // MediatR
                    services.AddMediatR(cfg =>
                    {
                        cfg.RegisterServicesFromAssembly(typeof(ScanDirectoryCommand).Assembly);
                        
                        // Add pipeline behaviors (order matters)
                        cfg.AddOpenBehavior(typeof(FileManagementSystem.Application.Behaviors.LoggingBehavior<,>));
                        cfg.AddOpenBehavior(typeof(FileManagementSystem.Application.Behaviors.AuthorizationBehavior<,>));
                        cfg.AddOpenBehavior(typeof(FileManagementSystem.Application.Behaviors.ValidationBehavior<,>));
                        cfg.AddOpenBehavior(typeof(FileManagementSystem.Application.Behaviors.ExceptionHandlingBehavior<,>));
                    });
                    
                    // FluentValidation
                    services.AddValidatorsFromAssembly(typeof(ScanDirectoryCommand).Assembly);
                    
                    // WPF
                    services.AddSingleton<MainWindow>();
                });
            
            _host = hostBuilder.Build();
            
            // Ensure database is created
            using var scope = _host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Use EnsureCreated for development, MigrateAsync for production
            if (dbContext.Database.EnsureCreated())
            {
                Log.Logger.Information("Database created successfully");
                
                // Seed initial data
                await Infrastructure.Data.SeedData.SeedAsync(dbContext);
                Log.Logger.Information("Database seeded with initial data");
            }
            else
            {
                // Try to apply migrations if they exist
                try
                {
                    await dbContext.Database.MigrateAsync();
                    Log.Logger.Information("Database migrations applied successfully");
                }
                catch (Exception ex)
                {
                    Log.Logger.Warning(ex, "No migrations found or migration failed, using existing database");
                }
                
                // Seed data if users don't exist
                if (!dbContext.Set<Domain.Entities.User>().Any())
                {
                    await Infrastructure.Data.SeedData.SeedAsync(dbContext);
                    Log.Logger.Information("Database seeded with initial data");
                }
            }
            
            // Show window immediately on UI thread
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            Log.Logger.Information("MainWindow shown successfully");
        }
        catch (Exception ex)
        {
            Log.Logger?.Fatal(ex, "Failed to initialize application");
            MessageBox.Show(
                $"Failed to start application: {ex.Message}\n\n{ex.StackTrace}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
    
    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Logger.Information("Application shutting down");
        
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        
        Log.CloseAndFlush();
        base.OnExit(e);
    }
    
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        Log.Logger.Fatal(exception, "Unhandled exception occurred. IsTerminating: {IsTerminating}", e.IsTerminating);
        
        if (e.IsTerminating)
        {
            MessageBox.Show(
                $"A fatal error occurred and the application must close.\n\nError: {exception?.Message}\n\nCheck logs for details.",
                "Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Logger.Error(e.Exception, "Unhandled exception in dispatcher");
        
        var result = MessageBox.Show(
            $"An error occurred: {e.Exception.Message}\n\nDo you want to continue?\n(Clicking No will close the application)",
            "Error",
            MessageBoxButton.YesNo,
            MessageBoxImage.Error);
        
        e.Handled = result == MessageBoxResult.Yes;
        
        if (!e.Handled)
        {
            Log.Logger.Fatal("User chose to close application after error");
        }
    }
    
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Logger.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }
}
