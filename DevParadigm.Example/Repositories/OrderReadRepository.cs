using DevParadigm.Common.Attribute;
using DevParadigm.Example.DbContexts;
using DevParadigm.Example.Entities;
using DevParadigm.Infrastructure.Data.Repositories.EF;
using Microsoft.Extensions.DependencyInjection;

namespace DevParadigm.Example.Repositories;

/// <summary>
/// 订单读仓储（从库）
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class OrderReadRepository : EfReadOnlyRepository<Order, Guid, ReadDbContext>
{
    public OrderReadRepository(ReadDbContext readDbContext) : base(readDbContext) { }
}