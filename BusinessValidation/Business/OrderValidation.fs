namespace BusinessValidation.Basement

open DevParadigm.Basement.Units
open DevParadigm.Common.Results
open DevParadigm.Interface

module OrderValidation =

    let private tryGet<'T> (key: string) (unit: BusinessUnit) =
        match unit.Extensions.TryGetValue key with
        | true, value ->
            match value with
            | :? 'T as typed -> Some typed
            | _ -> None
        | _ -> None

    let validateStockAndOrderLimit : Validator<IOrderData, BusinessUnit> =
        fun input unit ->
            let stockExists = tryGet<bool> "StockExists" unit |> Option.defaultValue false
            let remainQuantity = tryGet<int> "StockRemainQuantity" unit |> Option.defaultValue 0
            let todayOrderCount = tryGet<int> "TodayOrderCount" unit |> Option.defaultValue 0

            let errors = ResizeArray<BusinessError>()

            if not stockExists then
                errors.Add(BusinessError("Stock.NotFound", $"商品{input.ProductId}不存在", "ProductId"))
            elif remainQuantity < input.Quantity then
                errors.Add(BusinessError("Stock.NotEnough", $"商品{input.ProductId}库存不足（剩余：{remainQuantity}，请求：{input.Quantity}）", "Quantity"))

            if todayOrderCount >= 10 then
                errors.Add(BusinessError("Order.DailyLimitExceeded", $"用户{input.UserId}今日已下单{todayOrderCount}次，最多允许10次", "UserId"))

            if errors.Count = 0 then
                FsValidationResult.Success input
            else
                FsValidationResult.Failure input (List.ofSeq errors)

    let allOrderRules : Validator<IOrderData, BusinessUnit> =
        ValidatorCombinators.all [
            validateStockAndOrderLimit
        ]

    let validateOrderAll (input: IOrderData) (context: BusinessUnit) : FsValidationResult<IOrderData> =
        allOrderRules input context
