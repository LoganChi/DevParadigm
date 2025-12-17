using System.Collections.Generic;
using DevParadigm.Interface;

namespace DevParadigm.Example.DTOs.Input;

public class CreateBatchOrderInput : IBatchOrderData<CreateOrderInput>
{
    public List<CreateOrderInput> Orders { get; set; } = new();

    // Explicit implementation to satisfy interface
    IEnumerable<CreateOrderInput> IBatchOrderData<CreateOrderInput>.Orders => Orders;
}
