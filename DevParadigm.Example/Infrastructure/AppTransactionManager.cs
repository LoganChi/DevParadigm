using DevParadigm.Common.Attribute;
using DevParadigm.Example.DbContexts;
using DevParadigm.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace DevParadigm.Example.Infrastructure;

/// <summary>
/// 事务管理器
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class AppTransactionManager : EfTransactionManager<WriteDbContext>
{
    public AppTransactionManager(WriteDbContext dbContext) : base(dbContext)
    {
    }
}
