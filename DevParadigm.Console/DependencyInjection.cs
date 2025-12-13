using DevParadigm.Example.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevParadigm.Console;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 所有的服务注册逻辑已经移动到 DevParadigm.Example 项目中
        // 使用服务发现（Service Discovery）模式自动扫描和注册服务
        services.AddExampleLayer(configuration);

        return services;
    }
}
