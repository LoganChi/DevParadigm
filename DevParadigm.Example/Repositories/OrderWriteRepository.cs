using DevParadigm.Example.DbContexts;
using DevParadigm.Example.Entities;
using DevParadigm.Infrastructure.Data.Repositories.EF;

namespace DevParadigm.Example.Repositories;

/// <summary>
/// 订单写仓储（主库）
/// </summary>
public class OrderWriteRepository : EfWriteOnlyRepository<Order, Guid, WriteDbContext>
{
    public OrderWriteRepository(WriteDbContext writeDbContext) : base(writeDbContext) { }

    /// <summary>
    /// 扩展：批量创建订单
    /// </summary>
    public async Task AddBatchAsync(IEnumerable<Order> orders, CancellationToken cancellationToken = default)
    {
        await AddRangeAsync(orders, cancellationToken);
    }
}