namespace DevParadigm.Interface;

/// <summary>
/// 只写仓储接口（适配主库/写操作场景）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IWriteOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 添加单实体（主库）
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量添加实体（主库）
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新实体（主库）
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 部分更新（仅更新指定属性，避免全量更新性能问题）
    /// </summary>
    Task UpdatePartialAsync(TKey id, Dictionary<string, object> updateProperties, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除实体（主库）
    /// </summary>
    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量删除数据
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 提交写操作（统一数据持久化入口）
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}