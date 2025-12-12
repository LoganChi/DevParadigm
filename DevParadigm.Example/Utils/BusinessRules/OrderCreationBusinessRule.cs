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

    /// <summary>
    /// 校验库存是否充足（同步）
    /// </summary>
    public bool Validate(CreateOrderInput input, BusinessUnit unit)
    {
        // 同步调用异步方法（不推荐，仅适配接口；实际应重构为纯异步）
        var stockTask = _stockRepository.GetByIdAsync(input.ProductId, useReadDb: true);
        stockTask.Wait(); // 阻塞等待
        var stock = stockTask.Result;
        
        return stock != null && stock.RemainQuantity >= input.Quantity;
    }

    /// <summary>
    /// 获取校验错误信息
    /// </summary>
    public IEnumerable<string> GetErrors(CreateOrderInput input, BusinessUnit unit)
    {
        var stockTask = _stockRepository.GetByIdAsync(input.ProductId, useReadDb: true);
        stockTask.Wait();
        var stock = stockTask.Result;
        
        if (stock == null)
        {
            return new List<string> { $"商品{input.ProductId}不存在" };
        }
        if (stock.RemainQuantity < input.Quantity)
        {
            return new List<string> { $"商品{input.ProductId}库存不足（剩余：{stock.RemainQuantity}，请求：{input.Quantity}）" };
        }
        return new List<string>();
    }

    /// <summary>
    /// 异步校验（适配业务处理器）
    /// </summary>
    public async Task<ApiResult<bool>> ValidateAsync(CreateOrderInput input, BusinessUnit unit)
    {
        try
        {
            // 1. 校验库存是否充足（从从库查询，读写分离）
            var stock = await _stockRepository.GetByIdAsync(input.ProductId, useReadDb: true);
            
            if (stock == null)
            {
                return ApiResult<bool>.Fail("商品不存在", new List<string> { $"商品ID：{input.ProductId}" });
            }
            
            if (stock.RemainQuantity < input.Quantity)
            {
                return ApiResult<bool>.Fail(FailureMessage, new List<string> { GetErrors(input, unit).First() });
            }

            // 2. 校验用户单日下单次数（修正：使用订单仓储查询）
            var orderCount = await _orderRepository.CountAsync(
                o => o.UserId == input.UserId && o.CreateTime >= DateTime.Today, // Order实体才有UserId/CreateTime
                useReadDb: true); // 从从库查询，减轻主库压力
            
            if (orderCount >= 10)
            {
                return ApiResult<bool>.Fail("用户单日下单次数超出限制", 
                    new List<string> { $"用户{input.UserId}今日已下单{orderCount}次，最多允许10次" });
            }

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail("业务规则校验失败", new List<string> { ex.ToString() });
        }
    }
}