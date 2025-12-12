using DevParadigm.Example.DbContexts;
using DevParadigm.Example.Entities;
using DevParadigm.Infrastructure.Data.Repositories.EF;

namespace DevParadigm.Example.Repositories;

/// <summary>
/// 库存读写仓储聚合
/// </summary>
public class StockRepository : EfRepository<Stock, Guid, WriteDbContext, ReadDbContext>
{
    public StockRepository(WriteDbContext writeDbContext, ReadDbContext readDbContext) 
        : base(writeDbContext, readDbContext) { }
}