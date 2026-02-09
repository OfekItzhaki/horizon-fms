using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Infrastructure.Repositories;
using FileManagementSystem.Infrastructure.Services;
using FileManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentValidation;
using MediatR;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Behaviors;
using Serilog;

namespace FileManagementSystem.API.Installers;

public class WindsorInstaller : IWindsorInstaller
{
    private readonly IConfiguration _configuration;

    public WindsorInstaller(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Install(IWindsorContainer container, IConfigurationStore store)
    {
        // CRITICAL: Add CollectionResolver BEFORE any component registrations
        // This enables IEnumerable<T> injection support (needed for MediatR pipeline behaviors and ValidationBehavior)
        container.Kernel.Resolver.AddSubResolver(
            new Castle.MicroKernel.Resolvers.SpecializedResolvers.CollectionResolver(
                container.Kernel, 
                allowEmptyCollections: true
            )
        );
        
        // Register the container itself first so it can be resolved
        container.Register(
            Component.For<Castle.Windsor.IWindsorContainer>()
                .Instance(container)
                .LifestyleSingleton()
        );

        // Register IConfiguration so it can be resolved by factories
        container.Register(
            Component.For<IConfiguration>()
                .Instance(_configuration)
                .LifestyleSingleton()
        );
        
        // Register IServiceProvider - will be set from Program.cs after container is created
        // This allows resolving ASP.NET Core services (like ILogger<>) from ASP.NET Core DI
        // Note: The actual instance will be registered in Program.cs after container creation
        
        // DbContext - Scoped (managed by middleware)
        container.Register(
            Component.For<AppDbContext>()
                .UsingFactoryMethod(() =>
                {
                    var connectionString = _configuration.GetConnectionString("DefaultConnection")
                        ?? "Data Source=filemanager.db";
                    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                    
                    if (connectionString.Contains("Host="))
                    {
                        optionsBuilder.UseNpgsql(connectionString);
                    }
                    else
                    {
                        optionsBuilder.UseSqlite(connectionString);
                    }
                    
                    optionsBuilder.EnableSensitiveDataLogging(false)
                        .EnableServiceProviderCaching();
                    return new AppDbContext(optionsBuilder.Options);
                })
                .LifestyleScoped()
        );

        // Memory Cache - Singleton
        container.Register(
            Component.For<IMemoryCache>()
                .ImplementedBy<MemoryCache>()
                .UsingFactoryMethod(() => new MemoryCache(new MemoryCacheOptions()))
                .LifestyleSingleton()
        );

        // Repositories - Scoped (managed by middleware)
        container.Register(
            Component.For<IUnitOfWork>()
                .ImplementedBy<UnitOfWork>()
                .LifestyleScoped(),
            Component.For<IFileRepository>()
                .ImplementedBy<FileRepository>()
                .LifestyleScoped(),
            Component.For<IFolderRepository>()
                .ImplementedBy<CachedFolderRepository>()
                .LifestyleScoped()
        );

        // Services - Scoped (managed by middleware)
        container.Register(
            Component.For<IMetadataService>()
                .ImplementedBy<MetadataService>()
                .LifestyleScoped(),
            Component.For<IStorageService>()
                .UsingFactoryMethod<IStorageService>((kernel) => {
                    try 
                    {
                        var isCloudinaryEnabled = _configuration.GetValue<bool>("CloudinarySettings:IsEnabled");
                        
                        // Debug Configuration
                        var cloudName = _configuration["CloudinarySettings:CloudName"];
                        var apiKey = _configuration["CloudinarySettings:ApiKey"];
                        System.Console.WriteLine($"DEBUG: IsEnabled={isCloudinaryEnabled}, CloudName={cloudName}, ApiKey={apiKey}");

                        if (isCloudinaryEnabled)
                        {
                            return kernel.Resolve<CloudinaryStorageService>();
                        }
                        return kernel.Resolve<StorageService>();
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"FATAL: Failed to resolve IStorageService: {ex}");
                        throw;
                    }
                })
                .LifestyleScoped(),
            Component.For<StorageService>()
                .DependsOn(Dependency.OnValue<IConfiguration>(_configuration))
                .LifestyleScoped(),
            Component.For<CloudinaryStorageService>()
                .DependsOn(Dependency.OnValue<IConfiguration>(_configuration))
                .LifestyleScoped(),
            Component.For<IFilePathResolver>()
                .ImplementedBy<FilePathResolver>()
                .LifestyleScoped()
        );

        // Register ILoggerFactory (needed to create loggers)
        container.Register(
            Component.For<ILoggerFactory>()
                .UsingFactoryMethod(() =>
                {
                    return Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                        builder.AddSerilog(Log.Logger));
                })
                .LifestyleSingleton()
        );

        // Register ILogger<T> factory - try ASP.NET Core DI first, fallback to ILoggerFactory
        container.Register(
            Component.For(typeof(ILogger<>))
                .UsingFactoryMethod((kernel, model, context) =>
                {
                    var requestedType = context.RequestedType;
                    
                    // Try to resolve from ASP.NET Core service provider first
                    try
                    {
                        var serviceProvider = kernel.Resolve<IServiceProvider>();
                        var logger = serviceProvider.GetService(requestedType);
                        if (logger != null)
                        {
                            return logger;
                        }
                    }
                    catch
                    {
                        // Fall through to factory method
                    }
                    
                    // Fallback: Use ILoggerFactory
                    var loggerType = requestedType.GetGenericArguments()[0];
                    var loggerFactory = kernel.Resolve<ILoggerFactory>();
                    return loggerFactory.CreateLogger(loggerType);
                })
                .LifestyleTransient()
        );

        // Register IServiceScopeFactory for Authentication/Authorization services
        container.Register(
            Component.For<IServiceScopeFactory>()
                .UsingFactoryMethod(() => new ServiceScopeFactoryAdapter(container))
                .LifestyleSingleton()
        );

        // Authentication/Authorization - Scoped (managed by middleware)
        container.Register(
            Component.For<IAuthenticationService>()
                .ImplementedBy<AuthenticationService>()
                .DependsOn(
                    Dependency.OnValue<IConfiguration>(_configuration)
                    // ILogger<AuthenticationService> will be auto-wired by Castle Windsor
                )
                .LifestyleScoped(),
            Component.For<IAuthorizationService>()
                .ImplementedBy<AuthorizationService>()
                // ILogger<AuthorizationService> and IServiceScopeFactory will be auto-wired by Castle Windsor
                .LifestyleScoped()
        );

        // MediatR, handlers, pipeline behaviors, and validators are registered in Program.cs
        // using ASP.NET Core DI for better compatibility and reliability
    }
}

// IServiceScopeFactory adapter for Castle Windsor
internal class ServiceScopeFactoryAdapter : IServiceScopeFactory
{
    private readonly Castle.Windsor.IWindsorContainer _container;

    public ServiceScopeFactoryAdapter(Castle.Windsor.IWindsorContainer container)
    {
        _container = container;
    }

    public IServiceScope CreateScope()
    {
        return new WindsorServiceScope(_container);
    }
}

// IServiceScope adapter for Castle Windsor
internal class WindsorServiceScope : IServiceScope
{
    private readonly Castle.Windsor.IWindsorContainer _container;
    private readonly IDisposable _scope;
    private readonly WindsorServiceProvider _serviceProvider;

    public WindsorServiceScope(Castle.Windsor.IWindsorContainer container)
    {
        _container = container;
        // Create an actual Castle Windsor scope
        // This enables scoped lifestyle services to be resolved
        // Fully qualify to avoid conflict with LoggerExtensions.BeginScope
        var kernel = (Castle.MicroKernel.IKernel)_container.Kernel;
        _scope = Castle.MicroKernel.Lifestyle.LifestyleExtensions.BeginScope(kernel);
        // Create service provider that will resolve within this scope
        // Castle Windsor uses ambient scope context, so services resolved
        // while this scope is active will be scoped
        _serviceProvider = new WindsorServiceProvider(_container);
    }

    public IServiceProvider ServiceProvider => _serviceProvider;

    public void Dispose()
    {
        _scope?.Dispose();
    }
}

// IServiceProvider adapter for Castle Windsor
internal class WindsorServiceProvider : IServiceProvider
{
    private readonly Castle.Windsor.IWindsorContainer _container;

    public WindsorServiceProvider(Castle.Windsor.IWindsorContainer container)
    {
        _container = container;
    }

    public object? GetService(Type serviceType)
    {
        // Handle IEnumerable<T> - MediatR needs this for pipeline behaviors
        if (serviceType.IsGenericType && 
            serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = serviceType.GetGenericArguments()[0];
            
            try
            {
                if (_container.Kernel.HasComponent(elementType))
                {
                    // Resolve all instances of the element type
                    var allInstances = _container.ResolveAll(elementType);
                    // Convert to array (arrays implement IEnumerable<T>)
                    var array = Array.CreateInstance(elementType, allInstances.Length);
                    Array.Copy(allInstances, array, allInstances.Length);
                    return array;
                }
                
                // Return empty array if no components found
                return Array.CreateInstance(elementType, 0);
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Error resolving IEnumerable<{elementType.Name}>: {ex.Message}");
                // Return empty array instead of throwing - MediatR can handle empty collections
                return Array.CreateInstance(elementType, 0);
            }
        }
        
        // Handle single service resolution
        try
        {
            if (_container.Kernel.HasComponent(serviceType))
            {
                return _container.Resolve(serviceType);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error resolving {serviceType.Name}: {ex.Message}");
        }
        
        return null;
    }
}
