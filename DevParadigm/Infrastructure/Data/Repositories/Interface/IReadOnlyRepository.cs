using System.Linq.Expressions;

namespace DevParadigm.Interface;

/// <summary>
/// 只读仓储接口（适配从库/查询场景）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IReadOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 主键查询
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="useReadDb">是否使用读库（读写分离开关）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<TEntity?> GetByIdAsync(TKey id, bool useReadDb = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件单条查询
    /// </summary>
    /// <param name="predicate">查询条件表达式</param>
    /// <param name="useReadDb">是否使用读库</param>
    /// <param name="include">关联查询配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool useReadDb = true,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件查询（返回IQueryable，延迟执行）
    /// </summary>
    /// <param name="useReadDb">是否使用读库</param>
    IQueryable<TEntity> Query(bool useReadDb = true);

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="pageIndex">页码（从1开始）</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="orderBy">排序配置</param>
    /// <param name="useReadDb">是否使用读库</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<(List<TEntity> Items, int TotalCount)> GetPagedListAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int pageIndex,
        int pageSize,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool useReadDb = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计查询
    /// </summary>
    /// <param name="predicate">统计条件</param>
    /// <param name="useReadDb">是否使用读库</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool useReadDb = true,
        CancellationToken cancellationToken = default);
}
