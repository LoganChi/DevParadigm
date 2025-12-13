using DevParadigm.Common.Attribute;
using DevParadigm.Example.DbContexts;
using DevParadigm.Example.Entities;
using DevParadigm.Infrastructure.Data.Repositories.EF;
using Microsoft.Extensions.DependencyInjection;

namespace DevParadigm.Example.Repositories;

/// <summary>
/// 库存写仓储
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class StockWriteRepository : EfWriteOnlyRepository<Stock, Guid, WriteDbContext>
{
    public StockWriteRepository(WriteDbContext writeDbContext) : base(writeDbContext) { }
}
