using DevParadigm.Example.DbContexts;
using DevParadigm.Example.DTOs.Input;
using DevParadigm.Example.DTOs.Output;
using DevParadigm.Example.Entities;
using DevParadigm.Example.Repositories;
using DevParadigm.Example.Utils.BusinessRules;
using DevParadigm.Example.Utils.EntityBuilders;
using DevParadigm.Example.Utils.Handlers;
using DevParadigm.Infrastructure;
using DevParadigm.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevParadigm.Console;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. 注册EF上下文（主库/从库）
        services.AddDbContext<WriteDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("WriteDb")));
    
        services.AddDbContext<ReadDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("ReadDb")));
        // 注册 object 类型的 dbContext
        services.AddScoped(sp => (object)sp.GetRequiredService<WriteDbContext>());
        services.AddScoped<StockRepository>();
        // 2. 注册仓储
        services.AddScoped<IWriteOnlyRepository<Order, Guid>, OrderWriteRepository>();
        services.AddScoped<IReadOnlyRepository<Order, Guid>, OrderReadRepository>();
        services.AddScoped<IRepository<Order, Guid>, OrderRepository>();
    
        services.AddScoped<IWriteOnlyRepository<Stock, Guid>, StockWriteRepository>();
        services.AddScoped<IReadOnlyRepository<Stock, Guid>, StockReadRepository>();
        services.AddScoped<IRepository<Stock, Guid>, StockRepository>();

        // 3. 注册事务管理器
        services.AddScoped<ITransactionManager, EfTransactionManager<WriteDbContext>>();

        // 4. 注册校验器
        services.AddScoped<IUnifiedGradedValidator, UnifiedGradedValidator>();
        services.AddScoped<OrderCreationBusinessRule>();
        services.AddScoped<IBusinessValidationRule<CreateOrderInput>, OrderCreationBusinessRule>();

        // 5. 注册实体生成器
        services.AddScoped<IEntityBuilder<CreateOrderInput, Order>, OrderEntityBuilder>();

        // 6. 注册日志服务（修正位置）
        services.AddScoped(typeof(ILogger<>), typeof(Logger<>));

        // 7. 注册业务处理器（移除重复注册）
        services.AddScoped<IBusinessHandler<CreateOrderInput, CreateOrderOutput>, CreateOrderHandler>();

        return services;
    }
}
