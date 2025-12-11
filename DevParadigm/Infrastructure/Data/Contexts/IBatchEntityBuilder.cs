using DevParadigm.Basement.Units;

namespace DevParadigm.Interface;

/// <summary>
/// 批量实体生成器扩展接口
/// </summary>
/// <typeparam name="TInput">输入DTO类型</typeparam>
/// <typeparam name="TEntity">领域实体类型</typeparam>
public interface IBatchEntityBuilder<in TInput, out TEntity> : IEntityBuilder<TInput, TEntity>
    where TInput : class
    where TEntity : class
{
    IEnumerable<TEntity> BuildBatch(TInput input, BusinessUnit unit);
}