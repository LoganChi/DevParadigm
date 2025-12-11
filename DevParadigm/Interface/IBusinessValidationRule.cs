using DevParadigm.Basement.Units;
using DevParadigm.Enum;

namespace DevParadigm.Interface;

/// <summary>
/// 业务逻辑校验规则核心接口
/// </summary>
/// <typeparam name="TInput">输入类型</typeparam>
public interface IBusinessValidationRule<in TInput>
{
    string RuleId { get; }
    ValidationLevel Level { get; }
    string FailureMessage { get; }
    string NonMandatoryTip { get; }
    bool Validate(TInput input, BusinessUnit context);
    IEnumerable<string> GetErrors(TInput input, BusinessUnit context);
}