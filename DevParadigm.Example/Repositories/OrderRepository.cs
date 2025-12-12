using DevParadigm.Example.DbContexts;
using DevParadigm.Example.Entities;
using DevParadigm.Infrastructure.Data.Repositories.EF;

namespace DevParadigm.Example.Repositories;

/// <summary>
/// 订单读写仓储聚合
/// </summary>
public class OrderRepository : EfRepository<Order, Guid, WriteDbContext, ReadDbContext>
{
    public OrderRepository(WriteDbContext writeDbContext, ReadDbContext readDbContext) 
        : base(writeDbContext, readDbContext) { }
}