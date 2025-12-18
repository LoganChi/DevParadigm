using DevParadigm.Interface;
using MongoDB.Driver;

namespace DevParadigm.Infrastructure.Data.Repositories.MongoDB;

public class MongoWriteOnlyRepository<TEntity, TKey> : IWriteOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    protected readonly IMongoCollection<TEntity> _collection;

    public MongoWriteOnlyRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<TEntity>(typeof(TEntity).Name);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _collection.InsertManyAsync(entities, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        // 假设实体有 Id 属性（需根据实际实体定义调整）
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty == null) throw new InvalidOperationException("实体必须包含 Id 属性");
        
        var id = (TKey)idProperty.GetValue(entity)!;
        var filter = Builders<TEntity>.Filter.Eq("_id", id);
        await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }

    public async Task UpdatePartialAsync(TKey id, Dictionary<string, object> updateProperties, CancellationToken cancellationToken = default)
    {
        var update = Builders<TEntity>.Update.Combine(
            updateProperties.Select(kv => Builders<TEntity>.Update.Set(kv.Key, kv.Value))
        );
        await _collection.UpdateOneAsync(
            Builders<TEntity>.Filter.Eq("_id", id), 
            update, 
            cancellationToken: cancellationToken
        );
    }

    public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteOneAsync(Builders<TEntity>.Filter.Eq("_id", id), cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.In("_id", ids);
        await _collection.DeleteManyAsync(filter, cancellationToken);
    }

    // MongoDB 无需显式 SaveChanges（写入即持久化）
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1); // 返回 1 表示成功
    }
}
