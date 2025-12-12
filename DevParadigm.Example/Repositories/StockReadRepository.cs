using DevParadigm.Example.DbContexts;
using DevParadigm.Example.Entities;
using DevParadigm.Infrastructure.Data.Repositories.EF;

namespace DevParadigm.Example.Repositories;

/// <summary>
/// 库存读仓储（从库）
/// </summary>
public class StockReadRepository : EfReadOnlyRepository<Stock, Guid, ReadDbContext>
{
    public StockReadRepository(ReadDbContext readDbContext) : base(readDbContext) { }
}