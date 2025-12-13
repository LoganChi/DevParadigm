using DevParadigm.Basement.Handler;
using DevParadigm.Basement.Units;
using DevParadigm.Common.Enum;
using DevParadigm.Common.Results;
using DevParadigm.Example.DTOs.Input;
using DevParadigm.Example.DTOs.Output;
using DevParadigm.Example.Entities;
using DevParadigm.Example.Repositories;
using DevParadigm.Example.Utils.BusinessRules;
using DevParadigm.Interface;

namespace DevParadigm.Example.Utils.Handlers;

/// <summary>
/// 订单创建业务处理器（整合全流程）
/// </summary>
public class CreateOrderHandler : BaseBusinessHandler<CreateOrderInput, Order, CreateOrderOutput, Guid>
{
    private readonly IBusinessValidationRule<CreateOrderInput> _orderBusinessRule;
    private readonly StockRepository _stockRepository;

    public CreateOrderHandler(
        IUnifiedGradedValidator validator,
        IEntityBuilder<CreateOrderInput, Order> entityBuilder,
        IRepository<Order, Guid> repository,
        ITransactionManager transactionManager,
        object dbContext,
        IBusinessValidationRule<CreateOrderInput> orderBusinessRule,
        StockRepository stockRepository)
        : base(validator, entityBuilder, repository, transactionManager, dbContext)
    {
        _orderBusinessRule = orderBusinessRule;
        _stockRepository = stockRepository;
    }

    protected override Task<ApiResult<bool>> ValidateBusinessLogicAsync(CreateOrderInput input, BusinessUnit unit)
        => _orderBusinessRule.ValidateAsync(input, unit);

    /// <summary>
    /// 实体生成后后置处理（可选）
    /// </summary>
    protected override Task PostProcessEntityAsync(Order entity, CreateOrderInput input, BusinessUnit unit)
    {
        // 示例：添加扩展字段
        if (!string.IsNullOrEmpty(input.Remark))
        {
            unit.Extensions["OrderRemark"] = input.Remark;
        }
        
        // 记录操作日志（示例）
        Console.WriteLine($"订单实体生成完成：{entity.OrderNo}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 核心数据操作（事务内执行）
    /// </summary>
    protected override async Task<CreateOrderOutput> ExecuteDataOperationAsync(
        Order entity,
        CreateOrderInput input,
        BusinessUnit unit,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. 新增订单（主库）
            await Repository.AddAsync(entity, cancellationToken);
            
            // 2. 扣减库存（主库）
            await _stockRepository.DeductStockAsync(input.ProductId, input.Quantity, cancellationToken);
            
            // 3. 返回输出结果
            return new CreateOrderOutput
            {
                OrderId = entity.Id,
                OrderNo = entity.OrderNo,
                CreateTime = entity.CreateTime,
                Success = true
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"订单数据操作失败：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 非强制校验失败处理（示例：弹窗确认）
    /// </summary>
    protected override Task<NonMandatoryChoice> NonMandatoryHandler(Dictionary<string, string> failures)
    {
        // 实际场景可替换为用户交互（如前端弹窗）
        Console.WriteLine("非强制校验失败：");
        foreach (var (key, value) in failures)
        {
            Console.WriteLine($"- {key}: {value}");
        }
        Console.WriteLine("是否继续？（Y/N）");
        var input = Console.ReadLine()?.Trim().ToUpper() ?? "N";
        
        return Task.FromResult(input == "Y" ? NonMandatoryChoice.ConfirmContinue : NonMandatoryChoice.TerminateProcess);
    }
}
