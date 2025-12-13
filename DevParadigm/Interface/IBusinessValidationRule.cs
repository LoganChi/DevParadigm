using DevParadigm.Basement.Units;
using DevParadigm.Common.Enum;
using DevParadigm.Common.Results;

namespace DevParadigm.Interface;

public interface IBusinessValidationRule<in TInput>
{
    string RuleId { get; }
    ValidationLevel Level { get; }
    string FailureMessage { get; }
    string NonMandatoryTip { get; }
    Task<ApiResult<bool>> ValidateAsync(TInput input, BusinessUnit context);
}
