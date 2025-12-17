using BusinessValidation.Basement;
using DevParadigm.Basement.Units;
using DevParadigm.Common.Attribute;
using DevParadigm.Common.Enum;
using DevParadigm.Common.Results;
using DevParadigm.Example.DTOs.Input;
using DevParadigm.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace DevParadigm.Example.Utils.BusinessRules;

[Service(ServiceLifetime.Scoped)]
public class BatchOrderBusinessRule : IBusinessValidationRule<CreateBatchOrderInput>
{
    public string RuleId => "BatchOrder_Check";
    public ValidationLevel Level => ValidationLevel.Mandatory;
    public string FailureMessage => "批量订单校验失败";
    public string NonMandatoryTip => string.Empty;

    public async Task<ApiResult<bool>> ValidateAsync(CreateBatchOrderInput input, BusinessUnit unit)
    {
        // Call F# validator
        // Explicitly specifying generic arguments for the F# function
        var result = await BatchOrderValidation.validateBatchOrders<CreateBatchOrderInput, CreateOrderInput>(input, unit);
        
        if (!result.IsValid)
        {
             var errors = result.Errors.Select(e => e.Message).ToList();
             return ApiResult<bool>.Fail("批量校验失败", errors);
        }

        return ApiResult<bool>.Ok(true);
    }
}
