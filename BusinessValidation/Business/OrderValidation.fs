namespace BusinessValidation.Basement

open DevParadigm.Basement.Units
open DevParadigm.Common.Results
open DevParadigm.Interface

module OrderValidation =

    open ValidatorCombinators
    open ValidatorCombinators.Operators
    
    
    // 库存校验规则
    // 改造：使用 failFast ( >=> ) 将两个校验串联起来
    // 逻辑：只有当商品存在时，再去检查库存数量才有意义，避免无意义的报错
    let validateStock : Validator<IOrderData, BusinessUnit> =
        // 1. 检查商品是否存在
        let checkExists = 
            verifyVal 
                (fun _ (ctx: BusinessUnit) -> ctx.Get<bool>("StockExists", false))
                (fun exists _ -> exists)
                (fun _ (input: IOrderData) -> ValidatorHelpers.createError "Stock.NotFound" $"商品{input.ProductId}不存在" "ProductId")
        
        // 2. 检查库存是否充足
        let checkQuantity = 
            verifyVal
                (fun _ (ctx: BusinessUnit) -> ctx.Get<int>("StockRemainQuantity", 0))
                (fun remain (input: IOrderData) -> remain >= input.Quantity)
                (fun remain (input: IOrderData) -> 
                    ValidatorHelpers.createError "Stock.NotEnough" $"商品{input.ProductId}库存不足（剩余：{remain}，请求：{input.Quantity}）" "Quantity")

        // 串行组合：checkExists 失败则短路，不再执行 checkQuantity
        checkExists >=> checkQuantity

    // 下单限制校验规则
    let validateOrderLimit : Validator<IOrderData, BusinessUnit> =
        validation {
            verifyVal
                (fun _ (ctx: BusinessUnit) -> ctx.Get<int>("TodayOrderCount", 0))
                (fun count _ -> count < 10)
                (fun count (input: IOrderData) -> 
                    ValidatorHelpers.createError "Order.DailyLimitExceeded" $"用户{input.UserId}今日已下单{count}次，最多允许10次" "UserId")
        }

    // 综合规则：库存校验 和 订单限制校验 并行执行
    let allOrderRules : Validator<IOrderData, BusinessUnit> =
        validation {
            validateStock 
            validateOrderLimit
        }

    let validateOrderAll (input: IOrderData) (context: BusinessUnit) =
        allOrderRules input context
