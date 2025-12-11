using System.Linq.Expressions;
using DevParadigm.Interface;
using Microsoft.EntityFrameworkCore;

namespace DevParadigm.Infrastructure.Data.Repositories.EF;

/// <summary>
/// EF Core 从库读仓储实现（适配IReadOnlyRepository）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TReadDbContext">从库EF上下文</typeparam>
public class EfReadOnlyRepository<TEntity, TKey, TReadDbContext> : IReadOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
    where TReadDbContext : DbContext
{
    protected readonly TReadDbContext ReadDbContext;
    protected readonly DbSet<TEntity> ReadDbSet;

    public EfReadOnlyRepository(TReadDbContext readDbContext)
    {
        ReadDbContext = readDbContext ?? throw new ArgumentNullException(nameof(readDbContext));
        ReadDbSet = readDbContext.Set<TEntity>();
    }

    /// <summary>
    /// 主键查询（默认从库，支持切换主库）
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(TKey id, bool useReadDb = true, CancellationToken cancellationToken = default)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        
        var dbSet = useReadDb ? ReadDbSet : ReadDbContext.Set<TEntity>(); // 主库用同上下文或切换写上下文
        return await dbSet.FindAsync(new[] { id }, cancellationToken);
    }

    /// <summary>
    /// 条件单条查询（默认从库）
    /// </summary>
    public virtual async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool useReadDb = true,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        CancellationToken cancellationToken = default)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        var query = useReadDb ? ReadDbSet.AsQueryable() : ReadDbContext.Set<TEntity>().AsQueryable();
        query = query.IncludeIf(include);
        
        // 从库默认开启NoTracking提升性能
        if (useReadDb) query = query.AsNoTracking();
        
        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// 条件查询（默认从库）
    /// </summary>
    public virtual IQueryable<TEntity> Query(bool useReadDb = true)
    {
        var query = useReadDb ? ReadDbSet.AsQueryable() : ReadDbContext.Set<TEntity>().AsQueryable();
        
        // 从库默认开启NoTracking
        if (useReadDb) query = query.AsNoTracking();
        
        return query;
    }

    /// <summary>
    /// 分页查询（默认从库）
    /// </summary>
    public virtual async Task<(List<TEntity> Items, int TotalCount)> GetPagedListAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int pageIndex,
        int pageSize,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool useReadDb = true,
        CancellationToken cancellationToken = default)
    {
        if (pageIndex < 1) throw new ArgumentOutOfRangeException(nameof(pageIndex), "页码必须大于0");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "页大小必须大于0");
        
        var query = Query(useReadDb);
        
        // 应用查询条件
        if (predicate != null) query = query.Where(predicate);
        
        // 总数查询
        var totalCount = await query.CountAsync(cancellationToken);
        
        // 排序 + 分页
        if (orderBy != null) query = orderBy(query);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// 统计查询（默认从库）
    /// </summary>
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool useReadDb = true,
        CancellationToken cancellationToken = default)
    {
        var query = Query(useReadDb);
        
        if (predicate != null) query = query.Where(predicate);
        
        return await query.CountAsync(cancellationToken);
    }
}