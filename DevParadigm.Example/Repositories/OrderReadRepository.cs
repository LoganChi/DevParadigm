using DevParadigm.Example.DbContexts;
using DevParadigm.Example.Entities;
using DevParadigm.Infrastructure.Data.Repositories.EF;

namespace DevParadigm.Example.Repositories;

/// <summary>
/// 订单读仓储（从库）
/// </summary>
public class OrderReadRepository : EfReadOnlyRepository<Order, Guid, ReadDbContext>
{
    public OrderReadRepository(ReadDbContext readDbContext) : base(readDbContext) { }
}