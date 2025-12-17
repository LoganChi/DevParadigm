using BusinessValidation.Basement;
using DevParadigm.Basement.Units;
using DevParadigm.Common.Attribute;
using DevParadigm.Common.Enum;
using DevParadigm.Common.Results;
using DevParadigm.Example.Entities;
using DevParadigm.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace DevParadigm.Example.Utils.BusinessRules;

[Service(ServiceLifetime.Scoped)]
public class MerchantDeletionBusinessRule : IBusinessValidationRule<Merchant>
{
    private readonly IReadOnlyRepository<Order, Guid> _orderRepository;

    public MerchantDeletionBusinessRule(IReadOnlyRepository<Order, Guid> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public string RuleId => "MerchantDeletion_CascadeCheck";
    public ValidationLevel Level => ValidationLevel.Mandatory;
    public string FailureMessage => "无法删除商家：存在关联数据";
    public string NonMandatoryTip => "";

    public async Task<ApiResult<bool>> ValidateAsync(Merchant input, BusinessUnit context)
    {
        try 
        {
            // 1. 查询关联订单数量
            var orderCount = await _orderRepository.CountAsync(o => o.MerchantId == input.Id, useReadDb: true);
            
            // 2. 注入上下文 (约定Key为 {PropertyName}Count)
            // Merchant.Orders 是属性名
            context.Extensions["OrdersCount"] = orderCount;

            // 3. 调用 F# 级联校验器
            var result = await CascadeValidator.ValidateCascadeDelete(input, context);

            if (!result.IsValid)
            {
                 var errors = result.Errors.Select(e => e.Message).ToList();
                 return ApiResult<bool>.Fail("级联校验失败", errors);
            }

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"校验异常: {ex.Message}");
        }
    }
}
