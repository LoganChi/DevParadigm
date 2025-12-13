using DevParadigm.Common.Attribute;
using DevParadigm.Example.DbContexts;
using DevParadigm.Example.Entities;
using DevParadigm.Infrastructure.Data.Repositories.EF;
using Microsoft.Extensions.DependencyInjection;

namespace DevParadigm.Example.Repositories;

/// <summary>
/// 库存读仓储
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class StockReadRepository : EfReadOnlyRepository<Stock, Guid, ReadDbContext>
{
    public StockReadRepository(ReadDbContext readDbContext) : base(readDbContext) { }
}