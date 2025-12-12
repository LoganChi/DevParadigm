using DevParadigm.Example.DbContexts;
using DevParadigm.Example.Entities;
using DevParadigm.Infrastructure.Data.Repositories.EF;

namespace DevParadigm.Example.Repositories;

/// <summary>
/// 库存写仓储（主库）
/// </summary>
public class StockWriteRepository : EfWriteOnlyRepository<Stock, Guid, WriteDbContext>
{
    public StockWriteRepository(WriteDbContext writeDbContext) : base(writeDbContext) { }
}
