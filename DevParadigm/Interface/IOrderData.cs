namespace DevParadigm.Interface;

public interface IOrderData
{
    string UserId { get; }
    Guid ProductId { get; }
    int Quantity { get; }
    string? Remark { get; }
}

