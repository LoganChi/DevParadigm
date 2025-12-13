using System.Reflection;
using DevParadigm.Common.Attribute;
using DevParadigm.Example.DbContexts;
using DevParadigm.Infrastructure;
using DevParadigm.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DevParadigm.Example.Extensions;

/// <summary>
/// 示例层服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    // 常量定义：避免魔法值，提高可维护性
    private const string WriteDbConnectionKey = "WriteDb";
    private const string ReadDbConnectionKey = "ReadDb";
    private static readonly HashSet<string> _scanAssemblyPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "DevParadigm",
        "BusinessValidation"
    };

    /// <summary>
    /// 注册示例层所有服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="configureDbContext">自定义DbContext配置（可选）</param>
    /// <returns>服务集合</returns>
    /// <exception cref="ArgumentNullException">参数为空时抛出</exception>
    public static IServiceCollection AddExampleLayer(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder, IConfiguration>? configureDbContext = null)
    {
        // 空值保护
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 1. 注册EF上下文（解耦配置逻辑）
        RegisterDbContexts(services, configuration, configureDbContext);

        // 2. 注册基础服务
        services.AddLogging();

        // 3. 基于特性的服务自动发现（优化扫描逻辑）
        RegisterServicesByAttribute(services);

        return services;
    }

    /// <summary>
    /// 注册EF上下文（职责单一）
    /// </summary>
    private static void RegisterDbContexts(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder, IConfiguration>? configureDbContext)
    {
        // 注册写库上下文
        services.AddDbContext<WriteDbContext>(options =>
        {
            // 自定义配置优先
            if (configureDbContext != null)
            {
                configureDbContext(options, configuration);
            }
            else
            {
                options.UseSqlite(configuration.GetConnectionString(WriteDbConnectionKey));
            }
        });

        // 注册读库上下文
        services.AddDbContext<ReadDbContext>(options =>
        {
            if (configureDbContext != null)
            {
                configureDbContext(options, configuration);
            }
            else
            {
                options.UseSqlite(configuration.GetConnectionString(ReadDbConnectionKey));
            }
        });

        // 注册Object类型的DbContext（用于泛型仓储）
        // 使用TryAdd避免重复注册
        services.TryAddScoped(sp => (object)sp.GetRequiredService<WriteDbContext>());
    }

    /// <summary>
    /// 基于ServiceAttribute自动注册服务
    /// </summary>
    private static void RegisterServicesByAttribute(IServiceCollection services)
    {
        // 优化程序集扫描：只加载已加载且符合前缀的程序集，排除动态程序集
        var targetAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => 
                !assembly.IsDynamic && 
                assembly.FullName != null && 
                _scanAssemblyPrefixes.Any(prefix => assembly.FullName.StartsWith(prefix)))
            .Distinct()
            .ToList();

        if (targetAssemblies.Count == 0)
        {
            return;
        }

        // 遍历所有目标程序集，注册带特性的服务
        foreach (var assembly in targetAssemblies)
        {
            RegisterAssemblyServices(services, assembly);
        }
    }

    /// <summary>
    /// 注册单个程序集中的特性服务
    /// </summary>
    private static void RegisterAssemblyServices(IServiceCollection services, Assembly assembly)
    {
        // 筛选：非抽象类 + 带有ServiceAttribute
        var serviceTypes = assembly.GetTypes()
            .Where(t => t.IsClass && 
                       !t.IsAbstract && 
                       !t.IsGenericTypeDefinition && // 排除泛型定义类
                       t.GetCustomAttribute<ServiceAttribute>(inherit: true) != null) // 支持特性继承
            .ToList();

        foreach (var implementationType in serviceTypes)
        {
            var serviceAttr = implementationType.GetCustomAttribute<ServiceAttribute>(inherit: true)!;
            RegisterSingleService(services, implementationType, serviceAttr);
        }
    }

    /// <summary>
    /// 注册单个服务（自身 + 实现的接口）
    /// </summary>
    private static void RegisterSingleService(
        IServiceCollection services,
        Type implementationType,
        ServiceAttribute attribute)
    {
        // 1. 注册服务自身（如果启用）
        if (attribute.AsSelf)
        {
            // 使用TryAdd避免重复注册（支持多种注册方式）
            services.TryAdd(CreateServiceDescriptor(implementationType, implementationType, attribute.Lifetime));
        }

        // 2. 注册实现的接口（如果启用）
        if (attribute.AsImplementedInterfaces)
        {
            RegisterServiceInterfaces(services, implementationType, attribute);
        }
    }

    /// <summary>
    /// 注册服务实现的所有非系统接口
    /// </summary>
    private static void RegisterServiceInterfaces(
        IServiceCollection services,
        Type implementationType,
        ServiceAttribute attribute)
    {
        // 筛选：非系统接口 + 非IDisposable + 非泛型参数接口
        var interfaces = implementationType.GetInterfaces()
            .Where(i => 
                i.Namespace != null && 
                !i.Namespace.StartsWith("System", StringComparison.OrdinalIgnoreCase) && 
                !i.Namespace.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                i != typeof(IDisposable) &&
                !i.IsGenericTypeDefinition)
            .ToList();

        foreach (var serviceType in interfaces)
        {
            // 确保接口注册指向同一个实例（工厂方式）
            var descriptor = CreateServiceDescriptor(
                serviceType, 
                sp => sp.GetRequiredService(implementationType), 
                attribute.Lifetime);
            
            // TryAdd：避免重复注册相同接口+实现
            services.TryAdd(descriptor);
        }
    }

    /// <summary>
    /// 创建服务描述符（统一处理生命周期）
    /// </summary>
    private static ServiceDescriptor CreateServiceDescriptor(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime)
    {
        return lifetime switch
        {
            ServiceLifetime.Singleton => ServiceDescriptor.Singleton(serviceType, implementationType),
            ServiceLifetime.Scoped => ServiceDescriptor.Scoped(serviceType, implementationType),
            ServiceLifetime.Transient => ServiceDescriptor.Transient(serviceType, implementationType),
            _ => throw new NotSupportedException($"不支持的服务生命周期：{lifetime}")
        };
    }

    /// <summary>
    /// 重载：创建工厂方式的服务描述符
    /// </summary>
    private static ServiceDescriptor CreateServiceDescriptor(
        Type serviceType,
        Func<IServiceProvider, object> implementationFactory,
        ServiceLifetime lifetime)
    {
        return lifetime switch
        {
            ServiceLifetime.Singleton => ServiceDescriptor.Singleton(serviceType, implementationFactory),
            ServiceLifetime.Scoped => ServiceDescriptor.Scoped(serviceType, implementationFactory),
            ServiceLifetime.Transient => ServiceDescriptor.Transient(serviceType, implementationFactory),
            _ => throw new NotSupportedException($"不支持的服务生命周期：{lifetime}")
        };
    }
}