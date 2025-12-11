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
    /// 按主键查询（从库）
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, bool useMaster = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 提供IQueryable查询（默认从库）
    /// </summary>
    /// <param name="useMaster">是否使用主库</param>
    IQueryable<TEntity> Query(bool useMaster = false);
    
    /// <summary>
    /// 统计查询（从库）
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, bool useMaster = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 任意条件查询（从库）
    /// </summary>
    Task<List<TEntity>> QueryListAsync(Expression<Func<TEntity, bool>> predicate, bool useMaster = false, CancellationToken cancellationToken = default);
}
