using DevParadigm.Interface;
using MongoDB.Driver;

namespace DevParadigm.Infrastructure.Data.Repositories.MongoDB;

public class MongoRepository<TEntity, TKey> : MongoReadOnlyRepository<TEntity, TKey>, IRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    private readonly MongoWriteOnlyRepository<TEntity, TKey> _writeRepository;

    public MongoRepository(IMongoDatabase database) : base(database)
    {
        _writeRepository = new MongoWriteOnlyRepository<TEntity, TKey>(database);
    }

    // 实现 IWriteOnlyRepository 接口方法（委托给 _writeRepository）
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