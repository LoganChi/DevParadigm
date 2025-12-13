using DevParadigm.Basement.Units;
using DevParadigm.Common.Attribute;
using DevParadigm.Example.DTOs.Input;
using DevParadigm.Example.Entities;
using DevParadigm.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace DevParadigm.Example.Utils.EntityBuilders;

/// <summary>
/// 订单实体生成器
/// </summary>
[Service(ServiceLifetime.Singleton)]
public class OrderEntityBuilder : IEntityBuilder<CreateOrderInput, Order>
{
    /// <summary>
    /// DTO转换为订单实体
    /// </summary>
    public Order Build(CreateOrderInput input, BusinessUnit unit)
    {
        // 生成唯一订单编号（示例规则：时间戳+随机数）
        var orderNo = $"ORD{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        
        // 从业务单元获取用户信息（示例）
        var userId = unit.User?.Id ?? input.UserId;

        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNo = orderNo,
            UserId = userId,
            ProductId = input.ProductId,
            Quantity = input.Quantity,
            Amount = CalculateAmount(input.ProductId, input.Quantity), // 模拟金额计算
            CreateTime = DateTime.Now,
            Status = 0 // 待支付
        };
    }

    /// <summary>
    /// 模拟金额计算（实际应从商品库查询）
    /// </summary>
    private decimal CalculateAmount(Guid productId, int quantity)
    {
        // 示例：固定单价100元
        return 100 * quantity;
    }
}