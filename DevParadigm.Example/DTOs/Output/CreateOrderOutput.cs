namespace DevParadigm.Example.DTOs.Output;

/// <summary>
/// 订单创建输出DTO
/// </summary>
public class CreateOrderOutput
{
    /// <summary>
    /// 订单ID
    /// </summary>
    public Guid OrderId { get; set; }
    
    /// <summary>
    /// 订单编号
    /// </summary>
    public string OrderNo { get; set; } = string.Empty;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }
}