using DevParadigm.Basement;
using DevParadigm.Basement.Results;
using DevParadigm.Basement.Units;
using DevParadigm.Enum;

namespace DevParadigm.Interface;

/// <summary>
/// 统一分级校验器接口
/// </summary>
public interface IUnifiedGradedValidator
{
    Task<ValidationResult<TInput>> ValidateAsync<TInput>(
        TInput input,
        BusinessUnit context,
        Func<Dictionary<string, string>, Task<NonMandatoryChoice>>? nonMandatoryHandler = null)
        where TInput : class;
}