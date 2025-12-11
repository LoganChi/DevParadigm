using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DevParadigm.Interface;

/// <summary>
/// EF Core 主库事务管理器（适配ITransactionManager）
/// </summary>
/// <typeparam name="TWriteDbContext">主库EF上下文</typeparam>
public class EfTransactionManager<TWriteDbContext> : ITransactionManager
    where TWriteDbContext : DbContext
{
    private readonly TWriteDbContext _writeDbContext;
    private IDbContextTransaction? _currentTransaction; // 当前活跃事务

    public EfTransactionManager(TWriteDbContext writeDbContext)
    {
        _writeDbContext = writeDbContext ?? throw new ArgumentNullException(nameof(writeDbContext));
    }

    /// <summary>
    /// 开启主库事务
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // 避免重复开启事务
        if (_currentTransaction != null)
            throw new InvalidOperationException("已有活跃事务，无法重复开启");

        // 开启EF Core事务（可指定隔离级别，默认使用数据库默认）
        _currentTransaction = await _writeDbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// 提交主库事务
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("无活跃事务，无法提交");

        try
        {
            // 先提交EF Core的变更，再提交事务（确保变更先写入事务日志）
            await _writeDbContext.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            // 释放事务资源
            await DisposeCurrentTransaction();
        }
    }

    /// <summary>
    /// 回滚主库事务
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("无活跃事务，无法回滚");

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeCurrentTransaction();
        }
    }

    /// <summary>
    /// 事务内执行操作（自动处理开启/提交/回滚）
    /// </summary>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        // 1. 开启事务
        await BeginTransactionAsync(cancellationToken);

        try
        {
            // 2. 执行业务操作（内部包含仓储写操作）
            var result = await action();

            // 3. 提交事务
            await CommitTransactionAsync(cancellationToken);

            return result;
        }
        catch (Exception)
        {
            // 4. 异常时回滚事务
            await RollbackTransactionAsync(cancellationToken);
            throw; // 重新抛出异常，让上层处理
        }
    }

    #region 私有辅助方法
    /// <summary>
    /// 释放当前事务资源
    /// </summary>
    private async Task DisposeCurrentTransaction()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
    #endregion

    #region 释放资源
    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _writeDbContext.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction != null)
            await _currentTransaction.DisposeAsync();
        await _writeDbContext.DisposeAsync();
    }
    #endregion
}