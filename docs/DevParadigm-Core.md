# DevParadigm 核心框架文档

## 概述

DevParadigm 是一个现代化的 .NET 企业级开发框架，采用领域驱动设计（DDD）思想，结合面向对象和函数式编程范式，为 AI 辅助开发提供标准化的架构基础。

## 架构分层

### Basement - 基础层

基础层定义了框架的核心概念和基础抽象，是整个架构的基石。

#### UserInfo - 用户信息抽象

```csharp
public record UserInfo(string Id, string Name, int Level);
```

**设计理念**：
- 使用 C# 9.0 的 `record` 类型，确保不可变性
- 简洁的数据载体，封装用户基本信息
- 线程安全，适合在异步场景中传递

**使用场景**：
```csharp
// 在业务处理器中获取用户信息
var userInfo = new UserInfo("123", "张三", 2);
var result = await handler.ExecuteAsync(request, userInfo);
```

#### BusinessUnit - 业务上下文单元

```csharp
public sealed record BusinessUnit(
    object Input,
    IUnifiedGradedValidator Validator,
    object? DbContext = null,
    IServiceProvider? ServiceProvider = null,
    UserInfo? User = null,
    Dictionary<string, object>? Extensions = null
)
{
    public ValidationResult? ValidationResult { get; init; }
}
```

**核心特性**：
- **封装业务上下文**：包含业务执行所需的所有信息
- **依赖注入支持**：通过 `ServiceProvider` 解耦具体实现
- **灵活扩展**：`Extensions` 字典支持动态属性
- **线程安全**：使用 `init` 访问器，创建后不可变

**设计模式**：上下文对象模式（Context Object Pattern）

**使用示例**：
```csharp
// 构建业务单元
var businessUnit = new BusinessUnit(
    Input: requestDto,
    Validator: validator,
    DbContext: dbContext,
    ServiceProvider: serviceProvider,
    User: userInfo,
    Extensions: new() { ["RequestId"] = Guid.NewGuid() }
);
```

#### IBusinessHandler - 业务处理器接口

```csharp
public interface IBusinessHandler<TInput, TOutput>
    where TInput : class
    where TOutput : class
{
    Task<ApiResult<TOutput>> ExecuteAsync(
        TInput input,
        UserInfo? user = null,
        CancellationToken cancellationToken = default
    );
}
```

**设计亮点**：
- **泛型设计**：支持不同类型的输入输出
- **异步支持**：原生支持异步操作
- **取消令牌**：支持长时间运行的操作取消
- **标准契约**：统一的业务处理接口

#### BaseBusinessHandler - 业务处理器基类

这是框架的核心，采用**模板方法模式**封装了完整的业务执行流程：

```csharp
public abstract class BaseBusinessHandler<TInput, TEntity, TOutput, TKey>
    : IBusinessHandler<TInput, TOutput>
    where TInput : class
    where TEntity : class
    where TOutput : class
{
    // 核心执行流程
    public async Task<ApiResult<TOutput>> ExecuteAsync(
        TInput input,
        UserInfo? user = null,
        CancellationToken cancellationToken = default)
    {
        // 1. 构建业务上下文
        var unit = CreateBusinessUnit(input, user);

        // 2. 执行分级校验
        var validationResult = await ValidateAsync(unit, cancellationToken);

        // 3. 处理非强制校验失败
        if (!HandleNonMandatoryFailures(validationResult, out var shouldTerminate))
            return CreateFailureResult(validationResult);

        // 4. 构建实体
        var entity = await BuildEntityAsync(input, unit, cancellationToken);
        entity = await PostProcessEntityAsync(entity, unit, cancellationToken);

        // 5. 事务内执行数据操作
        var result = await ExecuteInTransactionAsync(
            () => ExecuteDataOperationAsync(entity, unit, cancellationToken),
            cancellationToken
        );

        // 6. 映射输出
        return MapToResult(result);
    }
}
```

**五个扩展点**：

1. **NonMandatoryHandler** - 非强制校验失败处理策略
2. **ValidateBusinessLogicAsync** - 自定义业务逻辑校验
3. **PostProcessEntityAsync** - 实体后处理（如审计字段）
4. **ExecuteDataOperationAsync** - 具体的数据操作逻辑
5. **MapToResponse** - 输出映射

**使用示例**：
```csharp
public class CreateUserHandler :
    BaseBusinessHandler<CreateUserRequest, User, UserResponse, Guid>
{
    protected override async Task<User> ExecuteDataOperationAsync(
        User entity,
        BusinessUnit unit,
        CancellationToken ct)
    {
        await _userRepository.AddAsync(entity, ct);
        await _transactionManager.SaveChangesAsync(ct);
        return entity;
    }

    protected override async Task<User> PostProcessEntityAsync(
        User entity,
        BusinessUnit unit,
        CancellationToken ct)
    {
        // 设置审计字段
        entity.CreatedBy = unit.User?.Id;
        entity.CreatedAt = DateTime.UtcNow;
        return entity;
    }
}
```

### Common - 通用组件层

#### 校验体系

##### ValidationLevel - 校验级别枚举

```csharp
public enum ValidationLevel
{
    Mandatory,    // 强制校验，失败直接终止
    NonMandatory  // 非强制校验，失败可选择继续
}
```

##### NonMandatoryChoice - 非强制校验选择

```csharp
public enum NonMandatoryChoice
{
    TerminateProcess,  // 终止进程
    ConfirmContinue    // 确认继续
}
```

##### GradedValidationAttribute - 分级校验特性

```csharp
public abstract class GradedValidationAttribute : ValidationAttribute
{
    public ValidationLevel Level { get; set; } = ValidationLevel.Mandatory;
    public string? NonMandatoryTip { get; set; }
}
```

**扩展示例**：
```csharp
public class StrongPasswordAttribute : GradedValidationAttribute
{
    public StrongPasswordAttribute()
    {
        Level = ValidationLevel.NonMandatory;
        NonMandatoryTip = "建议使用强密码以增强安全性";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string password) return false;

        // 检查密码强度：大小写、数字、特殊字符
        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(c => !char.IsLetterOrDigit(c));
    }
}
```

#### 统一返回类型

##### ApiResult - API 统一返回结果

```csharp
public sealed record ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public List<string> Errors { get; init; } = new();

    // 静态工厂方法
    public static ApiResult<T> Ok(T data, string message = "操作成功") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResult<T> Fail(string message, string? code = null) =>
        new() { Success = false, Message = message, Code = code ?? "ERROR" };
}
```

**设计亮点**：
- **不可变类型**：使用 `record` 确保线程安全
- **类型安全**：泛型设计支持任意数据类型
- **便捷创建**：静态工厂方法简化实例创建

##### ValidationResult - 校验结果

```csharp
public sealed record ValidationResult<T> : ValidationResult
{
    public T? Target { get; init; }

    public static ValidationResult<T> Success(T target) =>
        new() { IsValid = true, Target = target };

    public static ValidationResult<T> Failure(
        Dictionary<string, string> errors,
        ValidationLevel level = ValidationLevel.Mandatory) =>
        new() { IsValid = false, Errors = errors, Level = level };
}
```

**特殊功能**：
- 支持区分强制和非强制校验失败
- 保留原始校验目标对象
- 结构化的错误信息

## 设计模式总结

1. **模板方法模式**：BaseBusinessHandler 定义业务处理骨架
2. **策略模式**：非强制校验失败的不同处理策略
3. **工厂方法模式**：ApiResult 和 ValidationResult 的静态工厂
4. **适配器模式**：连接不同校验体系
5. **组合模式**：通过组合实现功能聚合

## 最佳实践

### 1. 业务处理器实现

```csharp
public class UpdateProductHandler :
    BaseBusinessHandler<UpdateProductRequest, Product, ProductResponse, Guid>
{
    // 实现必要的抽象方法
    protected override async Task<Product> ReadEntityAsync(Guid id, CancellationToken ct) =>
        await _productRepository.GetByIdAsync(id, ct);

    protected override async Task<Guid> WriteEntityAsync(Product entity, CancellationToken ct)
    {
        await _productRepository.UpdateAsync(entity, ct);
        await _transactionManager.SaveChangesAsync(ct);
        return entity.Id;
    }

    protected override ProductResponse MapToResponse(Product entity) =>
        new(entity.Id, entity.Name, entity.Price, entity.Stock);
}
```

### 2. 校验规则定义

```csharp
// 属性级校验
public record CreateOrderRequest(
    [Required(ErrorMessage = "客户ID不能为空")]
    Guid CustomerId,

    [Range(1, int.MaxValue, ErrorMessage = "商品数量必须大于0")]
    [GradedValidation(ValidationLevel.NonMandatory, NonMandatoryTip = "批量订购可能享受折扣")]
    int Quantity
);

// 业务逻辑校验
protected override async Task<ValidationResult<CreateOrderRequest>>
    ValidateBusinessLogicAsync(CreateOrderRequest input, BusinessUnit unit, CancellationToken ct)
{
    var errors = new Dictionary<string, string>();

    // 检查客户是否存在
    var customer = await _customerRepository.GetByIdAsync(input.CustomerId, ct);
    if (customer == null)
        errors["CustomerId"] = "客户不存在";

    // 检查库存
    var stock = await _inventoryRepository.GetStockAsync(input.ProductId, ct);
    if (stock < input.Quantity)
        errors["Quantity"] = $"库存不足，当前库存：{stock}";

    return errors.Count > 0
        ? ValidationResult<CreateOrderRequest>.Failure(errors)
        : ValidationResult<CreateOrderRequest>.Success(input);
}
```

### 3. 错误处理

```csharp
// 自定义非强制校验处理
protected override async Task<NonMandatoryChoice> NonMandatoryHandler(
    Dictionary<string, List<GradedValidationError>> errors)
{
    // 根据错误类型决定是否继续
    foreach (var error in errors.Values.SelectMany(v => v))
    {
        if (error.PropertyName == "Security")
        {
            // 安全相关警告必须终止
            return NonMandatoryChoice.TerminateProcess;
        }
    }

    // 其他警告可以继续，但记录日志
    _logger.LogWarning("业务执行时出现非强制校验警告：{@Errors}", errors);
    return NonMandatoryChoice.ConfirmContinue;
}
```

## 扩展指南

### 1. 添加新的校验特性

```csharp
public class BusinessHourAttribute : GradedValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is DateTime dateTime)
        {
            var hour = dateTime.Hour;
            return hour >= 9 && hour <= 18; // 9:00-18:00 为工作时间
        }
        return false;
    }
}
```

### 2. 自定义事务管理

```csharp
public class DistributedTransactionManager : ITransactionManager
{
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        // 实现分布式事务逻辑
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await operation();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

## 性能优化建议

1. **使用读写分离**：读操作使用 `IReadOnlyRepository`，写操作使用 `IWriteOnlyRepository`
2. **开启 NoTracking**：查询操作默认使用 `AsNoTracking()` 提升性能
3. **批量操作**：使用批量 API 减少数据库往返
4. **延迟加载**：谨慎使用，避免 N+1 查询问题
5. **缓存策略**：在适当的位置添加缓存层

## 总结

DevParadigm 核心框架通过精心的抽象设计，为业务开发提供了：

- **标准化流程**：统一的业务处理模式
- **灵活扩展**：多个扩展点支持定制化
- **类型安全**：强类型设计减少运行时错误
- **性能优化**：内置多种性能优化策略
- **AI 友好**：清晰的架构便于理解和生成代码

这个框架不仅适合传统开发模式，更为 AI 辅助开发提供了良好的基础架构。