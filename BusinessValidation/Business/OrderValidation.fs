namespace BusinessValidation.Basement

open DevParadigm.Basement.Units
open DevParadigm.Common.Results
open DevParadigm.Interface

module OrderValidation =

    open ValidatorCombinators
    open ValidatorCombinators.Operators

    let private tryGet<'T> (key: string) (unit: BusinessUnit) =
        match unit.Extensions.TryGetValue key with
        | true, value ->
            match value with
            | :? 'T as typed -> Some typed
            | _ -> None
        | _ -> None

    // 库存校验规则
    let validateStock : Validator<IOrderData, BusinessUnit> =
        // 1. 检查商品是否存在
        verify 
            (fun _ unit -> tryGet<bool> "StockExists" unit |> Option.defaultValue false)
            (fun input _ -> BusinessError("Stock.NotFound", $"商品{input.ProductId}不存在", "ProductId"))
        
        // 2. 检查库存是否充足
        <&> verify
            (fun input unit -> 
                let remain = tryGet<int> "StockRemainQuantity" unit |> Option.defaultValue 0
                remain >= input.Quantity)
            (fun input unit -> 
                let remain = tryGet<int> "StockRemainQuantity" unit |> Option.defaultValue 0
                BusinessError("Stock.NotEnough", $"商品{input.ProductId}库存不足（剩余：{remain}，请求：{input.Quantity}）", "Quantity"))

    // 下单限制校验规则
    let validateOrderLimit : Validator<IOrderData, BusinessUnit> =
        verify
            (fun input unit -> 
                let todayOrderCount = tryGet<int> "TodayOrderCount" unit |> Option.defaultValue 0
                todayOrderCount < 10)
            (fun input unit -> 
                let todayOrderCount = tryGet<int> "TodayOrderCount" unit |> Option.defaultValue 0
                BusinessError("Order.DailyLimitExceeded", $"用户{input.UserId}今日已下单{todayOrderCount}次，最多允许10次", "UserId"))

    let allOrderRules : Validator<IOrderData, BusinessUnit> =
        validateStock <&> validateOrderLimit

    let validateOrderAll (input: IOrderData) (context: BusinessUnit) : FsValidationResult<IOrderData> =
        allOrderRules input context
