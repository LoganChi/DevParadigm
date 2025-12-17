namespace BusinessValidation.Basement

open DevParadigm.Basement.Units
open DevParadigm.Interface
open DevParadigm.Common.Results
open ValidatorCombinators
open System.Collections.Generic

module BatchOrderValidation =

    // 批量订单校验
    // 使用泛型以避免直接依赖具体 DTO 类型 (解耦)
    // 'TBatch: 批量输入 DTO 类型 (e.g. CreateBatchOrderInput)
    // 'TOrder: 单个订单 DTO 类型 (e.g. CreateOrderInput)
    let validateBatchOrders<'TBatch, 'TOrder when 'TBatch :> IBatchOrderData<'TOrder>> (input: 'TBatch) (context: BusinessUnit) =
        // 1. 检查集合内部唯一性
        // 这里会检查 'TOrder 类型中标记了 [Unique] 的属性
        let validator = 
            prop (fun (x: 'TBatch) -> x.Orders) 
                 (UniqueValidator.ValidateCollectionUnique)

        validator input context
