using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevParadigm.Common.Attribute;

namespace DevParadigm.Example.Entities;

/// <summary>
/// 订单实体（主库存储）
/// </summary>
[Table("Orders")]
public class Order
{
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// 商家ID
    /// </summary>
    public Guid MerchantId { get; set; }

    /// <summary>
    /// 订单编号
    /// </summary>
    public string OrderNo { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 商品ID
    /// </summary>
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// 订单金额
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// 购买数量
    /// </summary>
    public int Quantity { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// 订单状态（0-待支付，1-已支付，2-已取消）
    /// </summary>
    [AllowedValues(0, 1, 2)]
    public int Status { get; set; } = 0;
}
