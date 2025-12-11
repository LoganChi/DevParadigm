using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace DevParadigm.Infrastructure.Data.Repositories.EF;

/// <summary>
/// EF Core 扩展工具类
/// </summary>
public static class EfCoreExtensions
{
    /// <summary>
    /// 部分更新实体（仅更新指定属性）
    /// </summary>
    public static void UpdatePartial<TEntity, TKey>(this DbContext dbContext, TKey id, Dictionary<string, object> updateProperties)
        where TEntity : class
        where TKey : IEquatable<TKey>
    {
        // 1. 附加实体（避免查询数据库）
        var entity = Activator.CreateInstance<TEntity>();
        var keyProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperties()
            .First(p => p.GetCustomAttributes<KeyAttribute>().Any());
        keyProperty.SetValue(entity, id);
        
        dbContext.Attach(entity);
        var entry = dbContext.Entry(entity);

        // 2. 标记指定属性为修改状态
        foreach (var (propertyName, value) in updateProperties)
        {
            var property = entry.Property(propertyName);
            if (property != null)
            {
                property.CurrentValue = value;
                property.IsModified = true;
            }
        }
    }

    /// <summary>
    /// 构建包含关联查询的IQueryable
    /// </summary>
    public static IQueryable<TEntity> IncludeIf<TEntity>(this IQueryable<TEntity> query, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include)
        where TEntity : class
    {
        return include == null ? query : include(query);
    }
}
