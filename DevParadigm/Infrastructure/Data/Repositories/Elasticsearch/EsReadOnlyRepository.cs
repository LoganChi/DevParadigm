using System.Linq.Expressions;
using DevParadigm.Interface;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace DevParadigm.Infrastructure.Data.Repositories.Elasticsearch;

public class EsReadOnlyRepository<TEntity, TKey> : IReadOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    protected readonly ElasticsearchClient _client;
    protected readonly string _indexName; // 索引名

    public EsReadOnlyRepository(ElasticsearchClient client)
    {
        _client = client;
        _indexName = typeof(TEntity).Name.ToLower(); // 索引名默认小写
    }

    public async Task<TEntity?> GetByIdAsync(TKey id, bool useReadDb = true, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync<TEntity>(id.ToString(), x => x.Index(_indexName), cancellationToken);
        return response.IsValidResponse ? response.Source : null;
    }

    public async Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool useReadDb = true, 
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, CancellationToken cancellationToken = default)
    {
        // 将 Lambda 表达式转换为 ES 查询条件（简化实现，复杂场景需完善）
        var query = new QueryContainer();
        // 此处仅示例相等条件，实际需解析表达式树生成对应的 ES 查询
        if (predicate.Body is BinaryExpression binary && binary.NodeType == ExpressionType.Equal)
        {
            var propertyName = ((MemberExpression)binary.Left).Member.Name;
            var value = ((ConstantExpression)binary.Right).Value;
            query = new TermQuery { Field = propertyName, Value = value };
        }

        var response = await _client.SearchAsync<TEntity>(s => s
            .Index(_indexName)
            .Query(q => query)
            .Size(1), cancellationToken);

        return response.Hits.FirstOrDefault()?.Source;
    }

    public IQueryable<TEntity> Query(bool useReadDb = true)
    {
        // ES 不直接支持 IQueryable，可返回空实现或内存列表（建议用原生查询）
        throw new NotImplementedException("Elasticsearch 建议使用原生查询 API 而非 IQueryable");
    }

    public async Task<(List<TEntity> Items, int TotalCount)> GetPagedListAsync(Expression<Func<TEntity, bool>>? predicate, 
        int pageIndex, int pageSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, 
        bool useReadDb = true, CancellationToken cancellationToken = default)
    {
        var from = (pageIndex - 1) * pageSize;
        var searchRequest = new SearchRequest<TEntity>(_indexName)
        {
            From = from,
            Size = pageSize,
            Query = predicate != null ? ConvertToQuery(predicate) : null,
            Sort = orderBy != null ? ConvertToSort(orderBy) : null
        };

        var response = await _client.SearchAsync(searchRequest, cancellationToken);
        return (response.Hits.Select(h => h.Source).ToList()!, (int)response.Total);
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, 
        bool useReadDb = true, CancellationToken cancellationToken = default)
    {
        var response = await _client.CountAsync<TEntity>(c => c
            .Index(_indexName)
            .Query(q => predicate != null ? ConvertToQuery(predicate) : null), cancellationToken);
        return (int)response.Count;
    }

    // 简化：将 Lambda 转换为 ES 查询（实际需完善表达式树解析）
    private QueryContainer? ConvertToQuery(Expression<Func<TEntity, bool>> predicate)
    {
        // 仅示例，需根据实际表达式类型扩展
        return new MatchAllQuery();
    }

    // 简化：将排序表达式转换为 ES 排序
    private List<SortOptions>? ConvertToSort(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy)
    {
        // 仅示例，需解析排序字段和方向
        return null;
    }
}