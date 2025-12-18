using System.Text.Json;
using DevParadigm.Interface;
using StackExchange.Redis;

namespace DevParadigm.Infrastructure.Data.Repositories.Redis;

public class RedisWriteOnlyRepository<TEntity, TKey> : IWriteOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    protected readonly IDatabase _db;
    protected readonly string _prefix;
    private readonly JsonSerializerOptions _jsonOptions = new();

    public RedisWriteOnlyRepository(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
        _prefix = $"{typeof(TEntity).Name}:";
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var id = GetEntityId(entity);
        var key = $"{_prefix}{id}";
        await _db.StringSetAsync(key, JsonSerializer.Serialize(entity, _jsonOptions));
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var batch = _db.CreateBatch();
        var tasks = new List<Task>();
        foreach (var entity in entities)
        {
            var id = GetEntityId(entity);
            var key = $"{_prefix}{id}";
            var task = batch.StringSetAsync(key, JsonSerializer.Serialize(entity, _jsonOptions));
            tasks.Add(task);
        }
        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        // 同 AddAsync（Redis 覆盖即更新）
        await AddAsync(entity, cancellationToken);
    }

    public async Task UpdatePartialAsync(TKey id, Dictionary<string, object> updateProperties, CancellationToken cancellationToken = default)
    {
        var key = $"{_prefix}{id}";
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty) return;

        var entity = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
        foreach (var (k, v) in updateProperties)
            entity[k] = v;

        await _db.StringSetAsync(key, JsonSerializer.Serialize(entity, _jsonOptions));
    }

    public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var key = $"{_prefix}{id}";
        await _db.KeyDeleteAsync(key);
    }

    public async Task DeleteRangeAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        var keys = ids.Select(id => (RedisKey)$"{_prefix}{id}").ToArray();
        await _db.KeyDeleteAsync(keys);
    }

    // Redis 写入即持久化，无需显式提交
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }

    // 获取实体的 Id（假设实体有 Id 属性）
    private TKey GetEntityId(TEntity entity)
    {
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty == null) throw new InvalidOperationException("实体必须包含 Id 属性");
        return (TKey)idProperty.GetValue(entity)!;
    }
}