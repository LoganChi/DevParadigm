using Microsoft.Extensions.DependencyInjection;

namespace DevParadigm.Common.Attribute;

/// <summary>
/// 服务注册特性，用于自动扫描和注册服务
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ServiceAttribute : System.Attribute
{
    public ServiceLifetime Lifetime { get; }
    
    /// <summary>
    /// 是否注册自身类型
    /// </summary>
    public bool AsSelf { get; set; } = true;
    
    /// <summary>
    /// 是否注册所有实现的接口（System命名空间下的接口除外）
    /// </summary>
    public bool AsImplementedInterfaces { get; set; } = true;

    public ServiceAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}
