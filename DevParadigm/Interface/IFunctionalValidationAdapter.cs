using DevParadigm.Common.Results;

namespace DevParadigm.Interface;

/// <summary>
/// 函数式校验适配器接口（适配F#/C#函数式规则）
/// </summary>
/// <typeparam name="TInput">输入类型</typeparam>
/// <typeparam name="TContext">函数式上下文类型</typeparam>
public interface IFunctionalValidationAdapter<TInput, TContext> : IBusinessValidationRule<TInput>
{
    TContext FunctionalContext { get; }
    Func<TInput, TContext, ValidationResult<TInput>> ValidatorFunc { get; }
}