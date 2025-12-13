# BusinessValidation - F# 函数式验证库文档

## 概述

BusinessValidation 是一个基于 F# 的函数式验证库，旨在提供类型安全、组合性强的验证解决方案。它通过函数式编程范式，实现声明式的验证逻辑定义，并与 C# 主框架无缝集成。

## 核心概念

### 1. 函数式验证器类型

```fsharp
// Validator.fs
type Validator<'T, 'Context> = 'T -> 'Context -> ValidationResult<'T>
```

**设计理念**：
- **纯函数**：验证器是无副作用的纯函数
- **高阶函数**：支持函数组合和高阶函数操作
- **类型安全**：通过类型系统确保验证的正确性

**使用示例**：
```fsharp
let validateEmail (email: string) (_: obj) =
    if email.Contains("@") then
        Success email
    else
        Failure ["邮箱格式不正确"]
```

### 2. 不可变的验证结果

```fsharp
// ValidationResult.fs
type ValidationResult<'T> = {
    IsValid: bool
    Target: 'T
    Errors: string list
}
```

**特性**：
- **不可变性**：使用 F# record 类型，创建后不可修改
- **函数式组合**：支持 `map`、`bind`、`combine` 等函数式操作
- **错误累积**：收集所有验证错误，而不是遇到第一个错误就停止

**核心方法**：

```fsharp
// 创建成功结果
let Success target = {
    IsValid = true
    Target = target
    Errors = []
}

// 创建失败结果
let Failure errors = {
    IsValid = false
    Target = Unchecked.defaultof<'T>
    Errors = errors
}

// 组合多个验证结果
let combine results =
    let allErrors =
        results
        |> List.collect (fun r -> r.Errors)

    if List.isEmpty allErrors then
        let target =
            results
            |> List.tryHead
            |> Option.map (fun r -> r.Target)
            |> Option.defaultValue (Unchecked.defaultof<'T>)
        Success target
    else
        Failure allErrors
```

## 核心组件详解

### 1. ValidatorCombinators - 验证器组合子

验证器组合子是函数式编程的核心概念，它允许我们将简单的验证器组合成复杂的验证逻辑。

#### all 组合子

```fsharp
let all validators (input: 'T) (context: 'Context) =
    validators
    |> List.map (fun validator -> validator input context)
    |> combine
```

**使用场景**：当需要同时满足多个验证条件时。

```fsharp
// 定义多个验证器
let validateName name _ =
    if String.IsNullOrWhiteSpace(name) then
        Failure ["姓名不能为空"]
    else
        Success name

let validateAge age _ =
    if age < 0 || age > 150 then
        Failure ["年龄必须在0-150之间"]
    else
        Success age

// 组合使用
let validatePerson allValidators =
    all [
        validateName
        validateAge
    ]

// 执行验证
let person = { Name = ""; Age = 200 }
let result = validatePerson person null
// 结果: Failure ["姓名不能为空"; "年龄必须在0-150之间"]
```

#### when' 条件组合子

```fsharp
let when' condition validator =
    fun input context ->
        if condition input then
            validator input context
        else
            Success input
```

**使用场景**：根据条件决定是否执行特定验证。

```fsharp
// 只有当是VIP用户时才验证信用额度
let validateCreditLimit creditLimit _ =
    if creditLimit < 10000 then
        Failure ["VIP用户信用额度不能低于10000"]
    else
        Success creditLimit

let conditionalValidator =
    when' (fun user -> user.IsVip) validateCreditLimit

let regularUser = { Name = "张三"; IsVip = false; CreditLimit = 5000 }
let vipUser = { Name = "李四"; IsVip = true; CreditLimit = 5000 }

// regularUser 不会执行信用额度验证
let result1 = conditionalValidator regularUser null
// 结果: Success

// vipUser 会执行信用额度验证
let result2 = conditionalValidator vipUser null
// 结果: Failure ["VIP用户信用额度不能低于10000"]
```

### 2. FunctionalValidationAdapter - F# 到 C# 的适配器

适配器层解决了 F# 和 C# 之间的互操作问题，让函数式验证逻辑能够无缝集成到 C# 项目中。

```fsharp
// FunctionalValidationAdapter.fs
type FunctionalValidationAdapter<'T>(validator: Validator<'T, obj>, ruleId: string) =
    interface IBusinessValidationRule<'T> with
        member _.RuleId = ruleId
        member _.Level = ValidationLevel.Mandatory
        member _.FailureMessage = ""
        member _.NonMandatoryTip = null

        member this.ValidateAsync(input: 'T, context: BusinessUnit) =
            task {
                let fsharpResult = validator input context
                return {
                    IsValid = fsharpResult.IsValid
                    Errors =
                        fsharpResult.Errors
                        |> List.map (fun error -> (this.RuleId, error))
                        |> dict
                        |> Dictionary
                    Target = input
                }
            }
```

**设计亮点**：
- **类型安全转换**：将 F# 的 `list` 转换为 C# 的 `Dictionary`
- **异步适配**：将同步的 F# 函数包装为 C# 的异步方法
- **规则标识**：每个验证器都有唯一的 `ruleId`

**C# 端使用**：

```csharp
// 在 C# 中注册 F# 验证器
services.AddScoped<IBusinessValidationRule<CreateUserRequest>>(sp =>
{
    var validator = BusinessValidation.Validators.validateStrongPassword;
    return new FunctionalValidationAdapter<CreateUserRequest>(
        validator,
        "StrongPasswordRule"
    );
});
```

## 高级特性

### 1. 上下文感知验证

验证器可以接收上下文信息，实现更复杂的验证逻辑。

```fsharp
// 验证用户是否有权限访问特定资源
let validatePermission resource (input: AccessRequest) (context: BusinessUnit) =
    let userPermissions =
        context.Extensions
        |> Option.ofObj
        |> Option.map (fun ext ->
            if ext.TryGetValue("UserPermissions", out var perms) then
                perms :?> string list
            else
                []
        )
        |> Option.defaultValue []

    if List.contains resource userPermissions then
        Success input
    else
        Failure [$"用户没有访问资源 {resource} 的权限"]
```

### 2. 异步验证支持

虽然 F# 验证器本身是同步的，但可以包装异步操作。

```fsharp
// 创建一个异步验证器包装器
let validateAsync (asyncValidator: 'T -> 'Context -> Task<ValidationResult<'T>>) =
    fun (input: 'T) (context: 'Context) ->
        asyncValidator input context
        |> Async.AwaitTask
        |> Async.RunSynchronously
```

### 3. 国际化支持

通过上下文传递本地化信息。

```fsharp
let localizedValidator messageKey (input: 'T) (context: BusinessUnit) =
    let culture =
        context.User
        |> Option.ofObj
        |> Option.map (fun u -> u.Culture)
        |> Option.defaultValue "en-US"

    let localizedMessage =
        LocalizationHelper.GetString(messageKey, culture)

    // 使用本地化消息
    if isValid input then
        Success input
    else
        Failure [localizedMessage]
```

## 实际应用示例

### 1. 订单验证场景

```fsharp
// 定义验证器
let validateCustomerId customerId _ =
    if customerId = Guid.Empty then
        Failure ["客户ID不能为空"]
    else
        Success customerId

let validateProductItems items _ =
    if List.isEmpty items then
        Failure ["订单至少包含一个商品"]
    else
        let itemErrors =
            items
            |> List.mapi (fun index item ->
                if item.Quantity <= 0 then
                    Some $"第{index + 1}个商品数量必须大于0"
                else
                    None
            )
            |> List.choose id

        if List.isEmpty itemErrors then
            Success items
        else
            Failure itemErrors

let validateDeliveryDate deliveryDate _ =
    if deliveryDate < DateTime.Today.AddDays(1) then
        Failure ["配送日期必须是明天之后"]
    else
        Success deliveryDate

// 组合所有验证器
let validateOrder = all [
    (fun order _ -> validateCustomerId order.CustomerId null)
    (fun order _ -> validateProductItems order.Items null)
    (fun order _ -> validateDeliveryDate order.DeliveryDate null)
]

// 使用示例
type Order = {
    CustomerId: Guid
    Items: OrderItem list
    DeliveryDate: DateTime
}

type OrderItem = {
    ProductId: Guid
    Quantity: int
}

let order = {
    CustomerId = Guid.Empty
    Items = []
    DeliveryDate = DateTime.Today
}

let validationResult = validateOrder order null
// 结果: Failure ["客户ID不能为空"; "订单至少包含一个商品"; "配送日期必须是明天之后"]
```

### 2. 复杂业务规则验证

```fsharp
// 验证促销活动的适用性
let validatePromotionEligibility (promotion: Promotion) (order: Order) (_: obj) =
    let errors = ResizeArray<string>()

    // 检查促销是否有效
    if not promotion.IsActive then
        errors.Add("促销活动已结束")

    // 检查时间范围
    let now = DateTime.Now
    if now < promotion.StartTime || now > promotion.EndTime then
        errors.Add("不在促销时间范围内")

    // 检查最低消费金额
    if order.TotalAmount < promotion.MinimumAmount then
        errors.Add($"订单金额未达到促销最低要求 {promotion.MinimumAmount}")

    // 检查商品限制
    let hasRestrictedItem =
        order.Items
        |> List.exists (fun item ->
            promotion.RestrictedProductIds.Contains(item.ProductId)
        )

    if hasRestrictedItem then
        errors.Add("订单包含促销不适用商品")

    if errors.Count = 0 then
        Success order
    else
        Failure (List.ofSeq errors)

// 使用条件组合子
let promotionValidator =
    when'
        (fun order -> order.PromotionCode.IsSome)
        (fun order context ->
            order.PromotionCode
            |> Option.map (fun code -> getPromotion code)
            |> Option.map (fun promo -> validatePromotionEligibility promo order context)
            |> Option.defaultValue (Success order))
```

## 性能优化

### 1. 短路验证

```fsharp
// 快速失败验证器组合子
let fastFail validators input context =
    let rec validate remaining =
        match remaining with
        | [] -> Success input
        | head :: tail ->
            match head input context with
            | Success _ -> validate tail
            | Failure errors -> Failure errors

    validate validators
```

### 2. 缓存验证结果

```fsharp
// 使用 F# 的 memoization 缓存验证结果
let memoize (f: 'T -> 'U) =
    let cache = System.Collections.Concurrent.ConcurrentDictionary<'T, 'U>()
    fun x ->
        cache.GetOrAdd(x, f)

// 创建缓存验证器
let cachedValidateEmail =
    validateEmail
    |> memoize
```

## 与 C# 集成最佳实践

### 1. 注册验证器

```csharp
// 在 Startup.cs 或 Program.cs 中注册
services.AddScoped<IUnifiedGradedValidator, UnifiedGradedValidator>();

// 注册 F# 验证规则
services.Scan(scan => scan
    .FromAssemblyOf<FunctionalValidationAdapter<object>>()
    .AddClasses(classes => classes.AssignableTo<IBusinessValidationRule<object>>())
    .AsImplementedInterfaces()
    .WithTransientLifetime());
```

### 2. 在业务处理器中使用

```csharp
public class CreateOrderHandler : BaseBusinessHandler<CreateOrderRequest, Order, OrderResponse, Guid>
{
    protected override async Task<ValidationResult<CreateOrderRequest>>
        ValidateBusinessLogicAsync(CreateOrderRequest input, BusinessUnit unit, CancellationToken ct)
    {
        // 使用统一的验证器，会自动执行所有注册的验证规则
        return await unit.Validator.ValidateAsync(input, unit);
    }
}
```

## 总结

BusinessValidation F# 库通过函数式编程范式提供了：

1. **类型安全**：编译时捕获验证逻辑错误
2. **组合性**：通过组合子构建复杂验证逻辑
3. **不可变性**：避免副作用，提高代码可靠性
4. **声明式**：专注于"验证什么"而非"如何验证"
5. **可测试性**：纯函数易于单元测试
6. **性能**：优化的组合策略和缓存机制

这个库完美展现了 F# 在特定领域（如验证逻辑）的优势，并通过适配器模式与 C# 生态系统无缝集成，是混合编程范式的典型应用。