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
    /// <summary>
    /// 扣减库存（读写分离版本）
    /// </summary>
    public async Task DeductStockAsync(Guid productId, int deductQuantity, CancellationToken cancellationToken = default)
    {
        // 通过读库获取当前库存（读写分离）
        var stock = await GetByIdAsync(productId , useReadDb: true, cancellationToken);
        var currentStock = stock?.RemainQuantity ?? 0;
        
        // 通过写库更新库存
        await UpdatePartialAsync(productId, new Dictionary<string, object>
        {
            { "RemainQuantity", currentStock - deductQuantity },
            { "UpdateTime", DateTime.Now }
        }, cancellationToken);
    }
}