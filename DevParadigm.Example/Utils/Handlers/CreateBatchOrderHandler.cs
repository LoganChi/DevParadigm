using DevParadigm.Basement.Units;
using DevParadigm.Common.Attribute;
using DevParadigm.Common.Results;
using DevParadigm.Example.DTOs.Input;
using DevParadigm.Example.DTOs.Output;
using DevParadigm.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace DevParadigm.Example.Utils.Handlers;

[Service(ServiceLifetime.Scoped)]
public class CreateBatchOrderHandler : IBusinessHandler<CreateBatchOrderInput, CreateBatchOrderOutput>
{
    private readonly IUnifiedGradedValidator _validator;
    private readonly IBusinessValidationRule<CreateBatchOrderInput> _batchRule;
    private readonly IBusinessHandler<CreateOrderInput, CreateOrderOutput> _singleOrderHandler;

    public CreateBatchOrderHandler(
        IUnifiedGradedValidator validator,
        IBusinessValidationRule<CreateBatchOrderInput> batchRule,
        IBusinessHandler<CreateOrderInput, CreateOrderOutput> singleOrderHandler)
    {
        _validator = validator;
        _batchRule = batchRule;
        _singleOrderHandler = singleOrderHandler;
    }

    public async Task<ApiResult<CreateBatchOrderOutput>> ExecuteAsync(
        CreateBatchOrderInput input,
        UserInfo? user = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Basic Validation (Unified)
        var unit = new BusinessUnit { Input = input, User = user };
        var validationResult = await _validator.ValidateAsync(input, unit);
        if (!validationResult.IsValid)
        {
             var errors = validationResult.MandatoryFailures?.Values.ToList() ?? new List<string>();
             return ApiResult<CreateBatchOrderOutput>.Fail("校验失败", errors);
        }

        // 2. Business Rule (Batch Check - F# Validator)
        var ruleResult = await _batchRule.ValidateAsync(input, unit);
        if (!ruleResult.Success)
        {
            return ApiResult<CreateBatchOrderOutput>.Fail(ruleResult.Message, ruleResult.Errors);
        }

        // 3. Process Each Order
        var output = new CreateBatchOrderOutput();
        
        foreach (var orderInput in input.Orders)
        {
            var result = await _singleOrderHandler.ExecuteAsync(orderInput, user, cancellationToken);
            if (result.Success)
            {
                output.SuccessCount++;
            }
            else
            {
                output.FailureCount++;
                output.Errors.Add($"Order for Product {orderInput.ProductId} failed: {result.Message}");
            }
        }

        return ApiResult<CreateBatchOrderOutput>.Ok(output);
    }
}
