using System.Linq.Expressions;
using DevParadigm.Interface;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DevParadigm.Infrastructure.Data.Repositories.MongoDB;

public class MongoReadOnlyRepository<TEntity, TKey> : IReadOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    protected readonly IMongoCollection<TEntity> _collection;

    public MongoReadOnlyRepository(IMongoDatabase database)
    {
        // 集合名称默认使用实体类型名
        _collection = database.GetCollection<TEntity>(typeof(TEntity).Name);
    }

    public async Task<TEntity?> GetByIdAsync(TKey id, bool useReadDb = true, CancellationToken cancellationToken = default)
    {
        // MongoDB 读写分离可通过配置不同连接字符串实现（此处简化处理）
        var filter = Builders<TEntity>.Filter.Eq("_id", id);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool useReadDb = true, 
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Where(predicate);  
        var query = _collection.Find(filter);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public IQueryable<TEntity> Query(bool useReadDb = true)
    {
        // MongoDB.Driver 支持 IQueryable 转换
        return _collection.AsQueryable();
    }

    public async Task<(List<TEntity> Items, int TotalCount)> GetPagedListAsync(Expression<Func<TEntity, bool>>? predicate, 
        int pageIndex, int pageSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, 
        bool useReadDb = true, CancellationToken cancellationToken = default)
    {
        var query = _collection.AsQueryable();
        if (predicate != null) query = query.Where(predicate);
        
        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (pageIndex - 1) * pageSize;
        
        if (orderBy != null)
            query = orderBy(query).Skip(skip).Take(pageSize);
        else
            query = query.Skip(skip).Take(pageSize);

        return (await query.ToListAsync(cancellationToken), totalCount);
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, 
        bool useReadDb = true, CancellationToken cancellationToken = default)
    {
        var query = _collection.AsQueryable();
        if (predicate != null) query = query.Where(predicate);
        return await query.CountAsync(cancellationToken);
    }
}