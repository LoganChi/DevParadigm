using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevParadigm.Common.Attribute;

namespace DevParadigm.Example.Entities;

/// <summary>
/// 商家实体
/// </summary>
[Table("Merchants")]
public class Merchant
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// 商家名称
    /// </summary>
    [Required]
    [Unique(ErrorMessage = "商家名称已存在")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 商家状态 (0-正常, 1-停业)
    /// </summary>
    [AllowedValues(0, 1)]
    public int Status { get; set; }

    /// <summary>
    /// 关联的订单 (级联删除)
    /// </summary>
    [Cascade(typeof(Order), Delete = true, ForeignKey = "MerchantId")]
    public List<Order> Orders { get; set; } = new();
}
