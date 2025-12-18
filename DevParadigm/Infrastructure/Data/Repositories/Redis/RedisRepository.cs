using DevParadigm.Interface;
using StackExchange.Redis;

namespace DevParadigm.Infrastructure.Data.Repositories.Redis;

public class RedisRepository<TEntity, TKey> : RedisReadOnlyRepository<TEntity, TKey>, IRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    private readonly RedisWriteOnlyRepository<TEntity, TKey> _writeRepository;

    public RedisRepository(IConnectionMultiplexer redis) : base(redis)
    {
        _writeRepository = new RedisWriteOnlyRepository<TEntity, TKey>(redis);
    }

    // 委托 IWriteOnlyRepository 方法实现
    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => _writeRepository.AddAsync(entity, cancellationToken);

    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => _writeRepository.AddRangeAsync(entities, cancellationToken);

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        => _writeRepository.UpdateAsync(entity, cancellationToken);

    public Task UpdatePartialAsync(TKey id, Dictionary<string, object> updateProperties, CancellationToken cancellationToken = default)
        => _writeRepository.UpdatePartialAsync(id, updateProperties, cancellationToken);

    public Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        => _writeRepository.DeleteAsync(id, cancellationToken);

    public Task DeleteRangeAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
        => _writeRepository.DeleteRangeAsync(ids, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _writeRepository.SaveChangesAsync(cancellationToken);
}