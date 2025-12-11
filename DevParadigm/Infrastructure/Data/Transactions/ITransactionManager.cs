namespace DevParadigm.Interface;

/// <summary>
/// 事务管理器核心接口（统一事务操作，适配任意ORM）
/// </summary>
public interface ITransactionManager
{
    /// <summary>
    /// 开启事务
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交事务
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚事务
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 在事务内执行异步操作（自动处理开启/提交/回滚）
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default);
}