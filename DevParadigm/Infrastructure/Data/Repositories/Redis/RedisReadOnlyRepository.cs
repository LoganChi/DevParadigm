using System.Linq.Expressions;
using System.Text.Json;
using DevParadigm.Interface;
using StackExchange.Redis;

namespace DevParadigm.Infrastructure.Data.Repositories.Redis;

public class RedisReadOnlyRepository<TEntity, TKey> : IReadOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    protected readonly IDatabase _db;
    protected readonly string _prefix; // 键前缀，避免冲突
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RedisReadOnlyRepository(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
        _prefix = $"{typeof(TEntity).Name}:";
    }

    public async Task<TEntity?> GetByIdAsync(TKey id, bool useReadDb = true, CancellationToken cancellationToken = default)
    {
        var key = $"{_prefix}{id}";
        var json = await _db.StringGetAsync(key);
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<TEntity>(json, _jsonOptions);
    }

    public async Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool useReadDb = true, 
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, CancellationToken cancellationToken = default)
    {
        // Redis 不支持复杂查询，需先获取所有键再内存过滤（仅适合小规模数据）
        var keys = await _db.ScriptEvaluateAsync(LuaScript.Prepare(
            "return redis.call('KEYS', @pattern)"), 
            new { pattern = $"{_prefix}*" }
        );

        foreach (var key in (RedisResult[])keys)
        {
            var json = await _db.StringGetAsync((RedisKey)key);
            var entity = JsonSerializer.Deserialize<TEntity>(json, _jsonOptions);
            if (entity != null && predicate.Compile()(entity))
                return entity;
        }
        return null;
    }

    public IQueryable<TEntity> Query(bool useReadDb = true)
    {
        // Redis 不原生支持 IQueryable，返回内存列表的查询（需谨慎使用）
        var keys = _db.ScriptEvaluate(LuaScript.Prepare("return redis.call('KEYS', @pattern)"), 
            new { pattern = $"{_prefix}*" });

        var entities = new List<TEntity>();
        foreach (var key in (RedisResult[])keys)
        {
            var json = _db.StringGet((RedisKey)key);
            if (!json.IsNullOrEmpty)
                entities.Add(JsonSerializer.Deserialize<TEntity>(json, _jsonOptions)!);
        }
        return entities.AsQueryable();
    }

    public async Task<(List<TEntity> Items, int TotalCount)> GetPagedListAsync(Expression<Func<TEntity, bool>>? predicate, 
        int pageIndex, int pageSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, 
        bool useReadDb = true, CancellationToken cancellationToken = default)
    {
        var query = Query().AsQueryable();
        if (predicate != null) query = query.Where(predicate);
        
        var totalCount = query.Count();
        var skip = (pageIndex - 1) * pageSize;
        var items = orderBy != null 
            ? await Task.FromResult(orderBy(query).Skip(skip).Take(pageSize).ToList())
            : await Task.FromResult(query.Skip(skip).Take(pageSize).ToList());

        return (items, totalCount);
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, 
        bool useReadDb = true, CancellationToken cancellationToken = default)
    {
        var query = Query();
        return Task.FromResult(predicate != null ? query.Count(predicate) : query.Count());
    }
}