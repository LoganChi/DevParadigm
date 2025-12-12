using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevParadigm.Example.Entities;

/// <summary>
/// 库存实体（主库存储）
/// </summary>
[Table("Stocks")]
public class Stock
{
    [Key]
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// 剩余库存
    /// </summary>
    public int RemainQuantity { get; set; }
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }
}