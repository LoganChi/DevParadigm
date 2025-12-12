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

    /// <summary>
    /// 扣减库存（部分更新）
    /// </summary>
    public async Task DeductStockAsync(Guid productId, int deductQuantity, CancellationToken cancellationToken = default)
    {
        // 部分更新：仅更新剩余库存和更新时间
        await UpdatePartialAsync(productId, new Dictionary<string, object>
        {
            { "RemainQuantity", await GetCurrentStockAsync(productId) - deductQuantity },
            { "UpdateTime", DateTime.Now }
        }, cancellationToken);
    }

    /// <summary>
    /// 获取当前库存（主库，保证实时性）
    /// </summary>
    private async Task<int> GetCurrentStockAsync(Guid productId)
    {
        var stock = await WriteDbContext.Stocks.FindAsync(productId);
        return stock?.RemainQuantity ?? 0;
    }
}
