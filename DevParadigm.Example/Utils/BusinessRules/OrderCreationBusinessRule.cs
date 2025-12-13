using BusinessValidation.Basement;
using DevParadigm.Basement.Units;
using DevParadigm.Common.Enum;
using DevParadigm.Common.Results;
using DevParadigm.Example.DTOs.Input;
using DevParadigm.Example.Entities;
using DevParadigm.Interface;

namespace DevParadigm.Example.Utils.BusinessRules;

/// <summary>
/// 订单创建业务规则校验器
/// </summary>
public class OrderCreationBusinessRule : IBusinessValidationRule<CreateOrderInput>
{
    private readonly IReadOnlyRepository<Stock, Guid> _stockRepository;
    // 新增：订单只读仓储（用于查询用户下单次数）
    private readonly IReadOnlyRepository<Order, Guid> _orderRepository;

    // 构造函数注入订单仓储
    public OrderCreationBusinessRule(
        IReadOnlyRepository<Stock, Guid> stockRepository,
        IReadOnlyRepository<Order, Guid> orderRepository)
    {
        _stockRepository = stockRepository;
        _orderRepository = orderRepository; // 注入订单仓储
    }

    public string RuleId => "OrderCreation_StockCheck";
    public ValidationLevel Level => ValidationLevel.Mandatory;
    public string FailureMessage => "库存不足，无法创建订单";
    public string NonMandatoryTip => string.Empty;

    public bool Validate(CreateOrderInput input, BusinessUnit unit)
    {
        var validationResult = OrderValidation.validateOrderAll(input, unit);
        return validationResult.IsValid;
    }

    public IEnumerable<string> GetErrors(CreateOrderInput input, BusinessUnit unit)
    {
        var validationResult = OrderValidation.validateOrderAll(input, unit);
        return validationResult.Errors;
    }

    /// <summary>
    /// 异步校验（适配业务处理器）
    /// </summary>
    public async Task<ApiResult<bool>> ValidateAsync(CreateOrderInput input, BusinessUnit unit)
    {
        try
        {
            // 1. 查询库存与下单次数（从从库查询，读写分离）
            var stock = await _stockRepository.GetByIdAsync(input.ProductId, useReadDb: true);
            var orderCount = await _orderRepository.CountAsync(
                o => o.UserId == input.UserId && o.CreateTime >= DateTime.Today, // Order实体才有UserId/CreateTime
                useReadDb: true); // 从从库查询，减轻主库压力

            unit.Extensions["StockExists"] = stock != null;
            unit.Extensions["StockRemainQuantity"] = stock?.RemainQuantity ?? 0;
            unit.Extensions["TodayOrderCount"] = orderCount;

            // 2. 调用 F# 业务规则（库存 + 下单次数）
            var validationResult = OrderValidation.validateOrderAll(input, unit);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.ToList();
                return ApiResult<bool>.Fail("业务规则校验失败", errors);
            }

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail("业务规则校验失败", new List<string> { ex.ToString() });
        }
    }
}
