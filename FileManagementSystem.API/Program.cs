using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using FluentValidation;
using MediatR;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Services;
using FileManagementSystem.Infrastructure.Data;
using FileManagementSystem.Infrastructure.Repositories;
using FileManagementSystem.Infrastructure.Services;
using FileManagementSystem.Application.Behaviors;
using Castle.Windsor;
using Castle.MicroKernel.Registration;
using FileManagementSystem.API.Installers;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.File("logs/api.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "File Management System API", 
        Version = "v1" 
    });
});

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React dev servers
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Create Windsor container and install components
var container = new WindsorContainer();
container.Install(new WindsorInstaller(builder.Configuration));

// Register Windsor container with ASP.NET Core DI
builder.Services.AddSingleton<IWindsorContainer>(container);

// Register business services from Windsor into ASP.NET Core DI
// Use factory that resolves from active Windsor scope (created by middleware)
builder.Services.AddScoped<IUnitOfWork>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IUnitOfWork>();
});
builder.Services.AddScoped<IFileRepository>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IFileRepository>();
});
builder.Services.AddScoped<IFolderRepository>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IFolderRepository>();
});
builder.Services.AddScoped<IMetadataService>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IMetadataService>();
});
builder.Services.AddScoped<IStorageService>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IStorageService>();
});
builder.Services.AddSingleton<IAuthenticationService>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IAuthenticationService>();
});
builder.Services.AddSingleton<IAuthorizationService>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IAuthorizationService>();
});

// MediatR - use ASP.NET Core DI for handlers and pipeline behaviors
// This is more reliable than Castle Windsor integration
var assembly = typeof(ScanDirectoryCommand).Assembly;
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly);
    
    // Add pipeline behaviors (order matters - they execute in this order)
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
});

// FluentValidation validators
builder.Services.AddValidatorsFromAssembly(assembly);

// DbContext - register directly for EF Core compatibility
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=filemanager.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
        .EnableSensitiveDataLogging(false)
        .EnableServiceProviderCaching());

// Memory Cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Register IServiceProvider in Windsor so it can resolve ASP.NET Core services (like ILogger<>)
// This must be done AFTER the app is built so we have the final service provider
container.Register(
    Component.For<IServiceProvider>()
        .Instance(app.Services)
        .LifestyleSingleton()
);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");

app.UseHttpsRedirection();

// Windsor scope middleware - must be early in pipeline to create scope for request
app.UseMiddleware<FileManagementSystem.API.Middleware.WindsorScopeMiddleware>();

// Global exception handler middleware
app.UseMiddleware<FileManagementSystem.API.Middleware.GlobalExceptionHandlerMiddleware>();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    if (dbContext.Database.EnsureCreated())
    {
        Log.Logger.Information("Database created successfully");
        
        // Seed initial data
        await FileManagementSystem.Infrastructure.Data.SeedData.SeedAsync(dbContext);
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
        
        // Ensure required columns exist (manual migration for schema updates)
        try
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            
            try
            {
                // Check and add IsCompressed column if needed
                var checkCompressedCommand = connection.CreateCommand();
                checkCompressedCommand.CommandText = "SELECT COUNT(*) FROM pragma_table_info('FileItems') WHERE name='IsCompressed'";
                var compressedExists = Convert.ToInt32(await checkCompressedCommand.ExecuteScalarAsync()) > 0;
                
                if (!compressedExists)
                {
                    var addCompressedCommand = connection.CreateCommand();
                    addCompressedCommand.CommandText = "ALTER TABLE FileItems ADD COLUMN IsCompressed INTEGER NOT NULL DEFAULT 1";
                    await addCompressedCommand.ExecuteNonQueryAsync();
                    Log.Logger.Information("Added IsCompressed column to FileItems table");
                }
                
                // Check and add FileName column if needed
                var checkFileNameCommand = connection.CreateCommand();
                checkFileNameCommand.CommandText = "SELECT COUNT(*) FROM pragma_table_info('FileItems') WHERE name='FileName'";
                var fileNameExists = Convert.ToInt32(await checkFileNameCommand.ExecuteScalarAsync()) > 0;
                
                if (!fileNameExists)
                {
                    var addFileNameCommand = connection.CreateCommand();
                    addFileNameCommand.CommandText = "ALTER TABLE FileItems ADD COLUMN FileName TEXT";
                    await addFileNameCommand.ExecuteNonQueryAsync();
                    Log.Logger.Information("Added FileName column to FileItems table");
                    
                    // Populate FileName for existing files from Path using raw SQL to avoid NULL reading issues
                    try
                    {
                        var updateCommand = connection.CreateCommand();
                        // Update NULL FileName values - set to empty string to allow EF Core to read them
                        updateCommand.CommandText = @"
                            UPDATE FileItems 
                            SET FileName = '' 
                            WHERE FileName IS NULL";
                        var nullCount = await updateCommand.ExecuteNonQueryAsync();
                        
                        if (nullCount > 0)
                        {
                            Log.Logger.Information("Set {Count} NULL FileName values to empty string", nullCount);
                            
                            // Note: We'll populate FileName from Path in the mapping layer
                            // This ensures proper path handling for both / and \ separators
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Warning(ex, "Error updating NULL FileName values");
                    }
                }
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Could not add columns automatically. You may need to run manual migrations");
        }
        
        // Seed data if users don't exist
        if (!dbContext.Set<FileManagementSystem.Domain.Entities.User>().Any())
        {
            await FileManagementSystem.Infrastructure.Data.SeedData.SeedAsync(dbContext);
            Log.Logger.Information("Database seeded with initial data");
        }
        
        // Rename any "C:" folder to "Default" folder
        try
        {
            var cFolder = await dbContext.Set<FileManagementSystem.Domain.Entities.Folder>()
                .FirstOrDefaultAsync(f => f.Name == "C:" || f.Path == "C:" || f.Path == "C:\\");
            
            if (cFolder != null)
            {
                var storageBasePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FileManagementSystem",
                    "Storage"
                );
                var defaultFolderPath = Path.Combine(storageBasePath, "Default");
                
                // Check if Default folder already exists
                var defaultFolder = await dbContext.Set<FileManagementSystem.Domain.Entities.Folder>()
                    .FirstOrDefaultAsync(f => f.Path == defaultFolderPath);
                
                if (defaultFolder == null)
                {
                    // Rename C: folder to Default
                    cFolder.Name = "Default";
                    cFolder.Path = defaultFolderPath;
                    await dbContext.SaveChangesAsync();
                    Log.Logger.Information("Renamed C: folder to Default folder");
                }
                else
                {
                    // Default folder exists, move files from C: to Default and delete C: folder
                    var filesInCFolder = await dbContext.Set<FileManagementSystem.Domain.Entities.FileItem>()
                        .Where(f => f.FolderId == cFolder.Id)
                        .ToListAsync();
                    
                    foreach (var file in filesInCFolder)
                    {
                        file.FolderId = defaultFolder.Id;
                    }
                    
                    dbContext.Set<FileManagementSystem.Domain.Entities.Folder>().Remove(cFolder);
                    await dbContext.SaveChangesAsync();
                    Log.Logger.Information("Moved {Count} files from C: folder to Default folder and deleted C: folder", filesInCFolder.Count);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Could not rename C: folder to Default folder");
        }
    }
}

Log.Logger.Information("File Management System API starting...");

try
{
    app.Run();
}
finally
{
    // Dispose Windsor container on shutdown
    container?.Dispose();
    Log.CloseAndFlush();
}
