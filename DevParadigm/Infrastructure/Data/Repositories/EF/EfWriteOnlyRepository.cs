using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DevParadigm.Interface;
using Microsoft.EntityFrameworkCore;

namespace DevParadigm.Infrastructure.Data.Repositories.EF;

/// <summary>
/// EF Core 主库写仓储实现（适配IWriteOnlyRepository）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TWriteDbContext">主库EF上下文</typeparam>
public class EfWriteOnlyRepository<TEntity, TKey, TWriteDbContext> : IWriteOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
    where TWriteDbContext : DbContext
{
    protected readonly TWriteDbContext WriteDbContext;
    protected readonly DbSet<TEntity> WriteDbSet;

    public EfWriteOnlyRepository(TWriteDbContext writeDbContext)
    {
        WriteDbContext = writeDbContext ?? throw new ArgumentNullException(nameof(writeDbContext));
        WriteDbSet = writeDbContext.Set<TEntity>();
    }

    /// <summary>
    /// 添加单实体（强制主库）
    /// </summary>
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        await WriteDbSet.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// 批量添加实体（强制主库）
    /// </summary>
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        await WriteDbSet.AddRangeAsync(entities, cancellationToken);
    }

    /// <summary>
    /// 更新实体（强制主库）
    /// </summary>
    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        WriteDbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 部分更新（仅更新指定属性，强制主库）
    /// </summary>
    public virtual Task UpdatePartialAsync(TKey id, Dictionary<string, object> updateProperties, CancellationToken cancellationToken = default)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        if (updateProperties == null || !updateProperties.Any()) 
            throw new ArgumentException("更新属性不能为空", nameof(updateProperties));

        // 使用扩展方法实现部分更新
        WriteDbContext.UpdatePartial<TEntity, TKey>(id, updateProperties);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 删除实体（强制主库）
    /// </summary>
    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        
        // 附加实体（避免查询数据库）
        var entity = Activator.CreateInstance<TEntity>();
        var keyProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperties()
            .First(p => p.GetCustomAttributes<KeyAttribute>().Any());
        keyProperty.SetValue(entity, id);
        
        WriteDbContext.Attach(entity);
        WriteDbSet.Remove(entity);
    }

    /// <summary>
    /// 批量删除数据（强制主库）
    /// </summary>
    public virtual async Task DeleteRangeAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || !ids.Any()) throw new ArgumentException("删除ID集合不能为空", nameof(ids));

        // 批量附加实体并删除
        var entities = ids.Select(id =>
        {
            var entity = Activator.CreateInstance<TEntity>();
            var keyProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperties()
                .First(p => p.GetCustomAttributes<KeyAttribute>().Any());
            keyProperty.SetValue(entity, id);
            return entity;
        }).ToList();

        WriteDbContext.AttachRange(entities);
        WriteDbSet.RemoveRange(entities);
    }

    /// <summary>
    /// 提交写操作（主库）
    /// </summary>
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 可添加审计字段自动填充逻辑（如CreateTime/UpdateTime/CreateBy等）
        return await WriteDbContext.SaveChangesAsync(cancellationToken);
    }
}