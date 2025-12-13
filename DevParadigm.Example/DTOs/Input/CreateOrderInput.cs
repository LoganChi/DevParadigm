using System.ComponentModel.DataAnnotations;
using DevParadigm.Common.Attribute;
using DevParadigm.Common.Enum;
using DevParadigm.Interface;

namespace DevParadigm.Example.DTOs.Input;

/// <summary>
/// 订单创建输入DTO
/// </summary>
public class CreateOrderInput : IOrderData
{
    /// <summary>
    /// 用户ID（强制校验）
    /// </summary>
    [Required(ErrorMessage = "用户ID不能为空")]
    [StringLength(50, ErrorMessage = "用户ID长度不能超过50")]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 商品ID（强制校验）
    /// </summary>
    [Required(ErrorMessage = "商品ID不能为空")]
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// 购买数量（强制校验+范围校验）
    /// </summary>
    [Required(ErrorMessage = "购买数量不能为空")]
    [Range(1, 5, ErrorMessage = "购买数量必须在1-5之间")]
    [NonMandatoryValidation(
        NonMandatoryTip = "购买数量超出常规范围，是否继续？")]
    public int Quantity { get; set; }
    
    /// <summary>
    /// 订单备注（非强制校验）
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过500")]
    [NonMandatoryValidation(
        NonMandatoryTip = "备注过长，是否继续？")]
    public string? Remark { get; set; }
}
