using System.Collections.Generic;

namespace DevParadigm.Interface;

public interface IBatchOrderData<out TOrder> where TOrder : IOrderData
{
    IEnumerable<TOrder> Orders { get; }
}
