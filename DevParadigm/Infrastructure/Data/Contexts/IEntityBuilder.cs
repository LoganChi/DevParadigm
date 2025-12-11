using DevParadigm.Basement.Units;

namespace DevParadigm.Interface;

/// <summary>
/// 实体生成器核心接口
/// </summary>
/// <typeparam name="TInput">输入DTO类型</typeparam>
/// <typeparam name="TEntity">领域实体类型</typeparam>
public interface IEntityBuilder<in TInput, out TEntity>
    where TInput : class
    where TEntity : class
{
    TEntity Build(TInput input, BusinessUnit unit);
}