namespace DevParadigm.Interface;


/// <summary>
/// 通用仓储接口（读写合一，兼容原有逻辑）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IRepository<TEntity, TKey> : IReadOnlyRepository<TEntity, TKey>, IWriteOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    
}