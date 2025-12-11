using System.Linq.Expressions;
using DevParadigm.Interface;
using Microsoft.EntityFrameworkCore;

namespace DevParadigm.Infrastructure.Data.Repositories.EF;

/// <summary>
/// EF Core 读写仓储聚合实现（主库写 + 从库读）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TWriteDbContext">主库上下文</typeparam>
/// <typeparam name="TReadDbContext">从库上下文</typeparam>
public class EfRepository<TEntity, TKey, TWriteDbContext, TReadDbContext> 
    : EfWriteOnlyRepository<TEntity, TKey, TWriteDbContext>, 
      IRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
    where TWriteDbContext : DbContext
    where TReadDbContext : DbContext
{
    private readonly EfReadOnlyRepository<TEntity, TKey, TReadDbContext> _readOnlyRepository;

    public EfRepository(TWriteDbContext writeDbContext, TReadDbContext readDbContext)
        : base(writeDbContext)
    {
        _readOnlyRepository = new EfReadOnlyRepository<TEntity, TKey, TReadDbContext>(readDbContext);
    }

    // 实现IReadOnlyRepository接口（委托给从库读仓储）
    public Task<TEntity?> GetByIdAsync(TKey id, bool useReadDb = true, CancellationToken cancellationToken = default)
        => _readOnlyRepository.GetByIdAsync(id, useReadDb, cancellationToken);

    public Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool useReadDb = true, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, CancellationToken cancellationToken = default)
        => _readOnlyRepository.GetFirstOrDefaultAsync(predicate, useReadDb, include, cancellationToken);

    public IQueryable<TEntity> Query(bool useReadDb = true)
        => _readOnlyRepository.Query(useReadDb);

    public Task<(List<TEntity> Items, int TotalCount)> GetPagedListAsync(Expression<Func<TEntity, bool>>? predicate, int pageIndex, int pageSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, bool useReadDb = true, CancellationToken cancellationToken = default)
        => _readOnlyRepository.GetPagedListAsync(predicate, pageIndex, pageSize, orderBy, useReadDb, cancellationToken);

    public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, bool useReadDb = true, CancellationToken cancellationToken = default)
        => _readOnlyRepository.CountAsync(predicate, useReadDb, cancellationToken);
}