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
using FileManagementSystem.API.Middleware;
using FileManagementSystem.Infrastructure.Data;
using FileManagementSystem.Infrastructure.Repositories;
using FileManagementSystem.Infrastructure.Services;
using FileManagementSystem.Application.Behaviors;
using Castle.Windsor;
using Castle.MicroKernel.Registration;
using FileManagementSystem.API.Installers;

using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AspNetCoreRateLimit;
using Microsoft.Extensions.Caching.StackExchangeRedis;

var builder = WebApplication.CreateBuilder(args);

// Helper function to convert postgresql:// URI to Npgsql connection string format
static string ConvertPostgresUri(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return connectionString;
    
    // If it's already in Host= format, return as-is
    if (connectionString.Contains("Host="))
        return connectionString;
    
    // If it's not a postgresql:// URI, return as-is (probably SQLite)
    if (!connectionString.StartsWith("postgresql://") && !connectionString.StartsWith("postgres://"))
        return connectionString;
    
    try
    {
        var uri = new Uri(connectionString);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo.Length > 0 ? userInfo[0] : "";
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        
        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
    catch
    {
        return connectionString; // Return original if parsing fails
    }
}

// Configure Serilog with Seq
var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.File("logs/api.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Console();

var seqUrl = builder.Configuration["Serilog:SeqServerUrl"];
if (!string.IsNullOrEmpty(seqUrl))
{
    loggerConfiguration.WriteTo.Seq(seqUrl);
}

Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure upload limits
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "File Management System API", 
        Version = "v1" 
    });
});

// Health Checks
var healthChecks = builder.Services.AddHealthChecks();

var connectionString = ConvertPostgresUri(builder.Configuration.GetConnectionString("DefaultConnection"));
if (connectionString?.Contains("Host=") == true)
{
    healthChecks.AddNpgSql(connectionString);
}
else
{
    healthChecks.AddSqlite(connectionString ?? "Data Source=filemanager.db");
}

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString) && redisConnectionString != "localhost:6379")
{
     healthChecks.AddRedis(redisConnectionString);
}

// Storage health check - always healthy since we use Cloudinary in production
healthChecks.AddCheck("Storage", () => HealthCheckResult.Healthy("Using cloud storage (Cloudinary)"));

// Redis Cache
var redisConfig = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConfig) && redisConfig != "localhost:6379")
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConfig;
        options.InstanceName = "HorizonFMS_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Configure CORS for React frontend
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains();
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
builder.Services.AddScoped<IFilePathResolver>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IFilePathResolver>();
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
var dbConnectionString = ConvertPostgresUri(builder.Configuration.GetConnectionString("DefaultConnection")) 
    ?? "Data Source=filemanager.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    // Check if it's PostgreSQL (Host= format after conversion)
    if (dbConnectionString.Contains("Host="))
    {
        options.UseNpgsql(dbConnectionString);
    }
    else
    {
        options.UseSqlite(dbConnectionString);
    }
    
    options.EnableSensitiveDataLogging(false)
           .EnableServiceProviderCaching();
});

// Application services
builder.Services.AddScoped<FileManagementSystem.Application.Services.UploadDestinationResolver>();
builder.Services.AddScoped<FileManagementSystem.Application.Services.FolderPathService>();

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

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';";
    await next();
});

// Increase file upload limit to 10MB
app.Use(async (context, next) =>
{
    context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>()
        !.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
    await next();
});

// Rate Limiting
app.UseIpRateLimiting();

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

// Windsor scope middleware - must be early in pipeline to create scope for request
app.UseMiddleware<FileManagementSystem.API.Middleware.WindsorScopeMiddleware>();

// Global exception handler middleware
app.UseMiddleware<FileManagementSystem.API.Middleware.GlobalExceptionHandlerMiddleware>();

app.UseRouting();

app.MapHealthChecks("/health");

app.UseAuthorization();

app.MapControllers();

// Ensure database is created and initialized with retries
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<FileManagementSystem.Infrastructure.Data.DatabaseInitializer>>();
    var initializer = new FileManagementSystem.Infrastructure.Data.DatabaseInitializer(dbContext, logger);
    
    int retryCount = 0;
    while (retryCount < 5)
    {
        try 
        {
            await initializer.InitializeAsync();
            break;
        }
        catch (Exception ex)
        {
            retryCount++;
            logger.LogWarning(ex, "Failed to initialize database (Attempt {RetryCount}/5). Retrying in 5 seconds...", retryCount);
            await Task.Delay(5000);
            if (retryCount >= 5) throw;
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

public partial class Program { }
