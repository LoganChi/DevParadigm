# Interface - 接口契约层文档

## 概述

Interface 层定义了 DevParadigm 框架的核心契约和接口，是整个架构中连接各个组件的纽带。这些接口体现了依赖倒置原则，实现了组件间的松耦合，为系统的可测试性和可扩展性提供了基础。

## 核心接口概览

```
Interface/
├── IBusinessHandler.cs          # 业务处理器核心接口
├── IRepository.cs               # 统一仓储接口
├── IReadOnlyRepository.cs       # 只读仓储接口
├── IWriteOnlyRepository.cs      # 只写仓储接口
├── IEntityBuilder.cs            # 实体构建器接口
├── IBatchEntityBuilder.cs       # 批量实体构建器接口
├── ITransactionManager.cs       # 事务管理器接口
├── IBusinessValidationRule.cs   # 业务校验规则接口
├── IFunctionalValidationAdapter.cs # 函数式校验适配器接口
└── IUnifiedGradedValidator.cs   # 统一分级校验器接口
```

## 业务处理接口

### 1. IBusinessHandler - 业务处理器核心接口

```csharp
public interface IBusinessHandler<TInput, TOutput>
    where TInput : class
    where TOutput : class
{
    /// <summary>
    /// 执行业务操作
    /// </summary>
    /// <param name="input">输入参数</param>
    /// <param name="user">用户信息（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统一的 API 返回结果</returns>
    Task<ApiResult<TOutput>> ExecuteAsync(
        TInput input,
        UserInfo? user = null,
        CancellationToken cancellationToken = default);
}
```

**设计意图**：
- **统一入口**：所有业务操作的统一入口点
- **类型安全**：泛型约束确保类型正确性
- **异步优先**：原生支持异步操作
- **上下文感知**：支持用户信息和取消令牌

**接口职责**：
- 定义业务处理的标准契约
- 封装业务执行的上下文
- 提供统一的返回格式

**使用场景**：
```csharp
// 定义处理器接口
public interface ICreateUserHandler : IBusinessHandler<CreateUserRequest, UserResponse>
{
}

// 实现处理器
public class CreateUserHandler : BaseBusinessHandler<CreateUserRequest, User, UserResponse, Guid>
{
    // 实现 ExecuteAsync 方法的具体逻辑
}

// 使用处理器
public class UserController
{
    private readonly ICreateUserHandler _handler;

    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _handler.ExecuteAsync(request, GetCurrentUser());
        return result.Success ? Ok(result.Data) : BadRequest(result.Message);
    }
}
```

## 数据访问接口

### 1. IRepository - 统一仓储接口

```csharp
public interface IRepository<TEntity, TKey>
    : IReadOnlyRepository<TEntity, TKey>,
      IWriteOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
}
```

**设计原则**：
- **接口分离**：继承读写接口，保持单一职责
- **向后兼容**：提供完整的 CRUD 操作
- **泛型设计**：支持任意实体类型

### 2. IReadOnlyRepository - 只读仓储接口

```csharp
public interface IReadOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    // ========== 基础查询操作 ==========

    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="useMaster">是否使用主库（用于刚插入后的查询）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<TEntity?> GetByIdAsync(
        TKey id,
        bool useMaster = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取满足条件的第一个实体
    /// </summary>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool useMaster = false,
        CancellationToken cancellationToken = default);

    // ========== 查询构建器 ==========

    /// <summary>
    /// 获取可查询接口，支持进一步构建查询
    /// </summary>
    /// <param name="useMaster">是否使用主库</param>
    /// <param name="asNoTracking">是否启用 NoTracking</param>
    IQueryable<TEntity> Query(
        bool useMaster = false,
        bool asNoTracking = true);

    // ========== 分页查询 ==========

    /// <summary>
    /// 分页查询
    /// </summary>
    Task<PagedList<TEntity>> GetPagedListAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool useMaster = false,
        CancellationToken cancellationToken = default);

    // ========== 统计查询 ==========

    /// <summary>
    /// 统计数量
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool useMaster = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否存在满足条件的记录
    /// </summary>
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool useMaster = false,
        CancellationToken cancellationToken = default);
}
```

**关键设计决策**：
1. **useMaster 参数**：支持读写分离场景，查询刚插入的数据时强制使用主库
2. **返回 IQueryable**：支持复杂的查询构建和延迟执行
3. **异步优先**：所有操作都支持异步
4. **取消令牌支持**：支持长时间运行查询的取消

### 3. IWriteOnlyRepository - 只写仓储接口

```csharp
public interface IWriteOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    // ========== 添加操作 ==========

    /// <summary>
    /// 添加单个实体
    /// </summary>
    Task<TEntity> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加实体
    /// </summary>
    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    // ========== 更新操作 ==========

    /// <summary>
    /// 更新实体
    /// </summary>
    Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 部分更新实体（仅更新指定字段）
    /// </summary>
    Task UpdatePartialAsync(
        TKey id,
        Dictionary<string, object> updates,
        CancellationToken cancellationToken = default);

    // ========== 删除操作 ==========

    /// <summary>
    /// 删除实体
    /// </summary>
    Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据主键删除实体
    /// </summary>
    Task DeleteByIdAsync(
        TKey id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除实体
    /// </summary>
    Task DeleteRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);
}
```

**特色功能**：
- **部分更新**：`UpdatePartialAsync` 避免全量更新的性能问题
- **批量操作**：`AddRangeAsync`、`DeleteRangeAsync` 优化批量场景
- **类型安全**：强类型主键和实体

## 实体构建接口

### 1. IEntityBuilder - 实体构建器接口

```csharp
public interface IEntityBuilder<TInput, TEntity>
    where TEntity : class
{
    /// <summary>
    /// 将输入 DTO 构建为领域实体
    /// </summary>
    /// <param name="input">输入的 DTO</param>
    /// <param name="unit">业务上下文单元</param>
    /// <returns>构建后的实体</returns>
    TEntity Build(TInput input, BusinessUnit unit);
}
```

**职责**：
- **DTO 到实体转换**：实现数据传输对象到领域实体的转换
- **上下文注入**：利用 BusinessUnit 提供的上下文信息
- **业务逻辑封装**：封装实体创建的业务规则

**使用示例**：

```csharp
// 定义接口
public interface IOrderEntityBuilder : IEntityBuilder<CreateOrderRequest, Order>
{
}

// 实现接口
public class OrderEntityBuilder : IOrderEntityBuilder
{
    public Order Build(CreateOrderRequest input, BusinessUnit unit)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = input.CustomerId,
            Items = input.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = CalculatePrice(item.ProductId, item.Quantity, unit)
            }).ToList(),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = unit.User?.Id
        };

        // 应用促销规则
        ApplyPromotionRules(order, unit);

        return order;
    }

    private decimal CalculatePrice(Guid productId, int quantity, BusinessUnit unit)
    {
        // 可以从 unit.ServiceProvider 获取价格计算服务
        var priceService = unit.ServiceProvider?.GetService<IPriceService>();
        return priceService?.CalculatePrice(productId, quantity) ?? 0;
    }
}
```

### 2. IBatchEntityBuilder - 批量实体构建器

```csharp
public interface IBatchEntityBuilder<TInput, TEntity>
    where TEntity : class
{
    /// <summary>
    /// 批量构建实体
    /// </summary>
    IEnumerable<TEntity> BuildBatch(
        IEnumerable<TInput> inputs,
        BusinessUnit unit);
}
```

**适用场景**：
- 批量导入
- 批量处理
- 数据迁移

## 事务管理接口

### ITransactionManager - 事务管理器接口

```csharp
public interface ITransactionManager : IDisposable, IAsyncDisposable
{
    // ========== 自动事务管理 ==========

    /// <summary>
    /// 在事务中执行操作（自动管理事务生命周期）
    /// </summary>
    /// <typeparam name="TResult">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="isolationLevel">事务隔离级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    // ========== 手动事务管理 ==========

    /// <summary>
    /// 开始事务
    /// </summary>
    Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交事务
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚事务
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

**设计模式**：
- **模板方法模式**：`ExecuteInTransactionAsync` 封装事务模板
- **资源管理模式**：实现 `IDisposable` 和 `IAsyncDisposable`
- **策略模式**：支持不同的事务隔离级别

**使用场景**：

```csharp
// 自动事务管理 - 推荐
var result = await _transactionManager.ExecuteInTransactionAsync(async () =>
{
    var user = await _userRepository.AddAsync(userEntity);
    var profile = await _profileRepository.AddAsync(profileEntity);
    var notification = await _notificationService.SendWelcomeNotification(user);

    return new { User = user, Profile = profile, NotificationSent = notification.Sent };
});

// 手动事务管理 - 复杂场景
await _transactionManager.BeginTransactionAsync();
try
{
    await _userRepository.AddAsync(userEntity);

    // 复杂的业务逻辑
    if (await CheckDuplicateAsync(userEntity.Email))
    {
        throw new DuplicateEmailException();
    }

    await _profileRepository.AddAsync(profileEntity);
    await _transactionManager.CommitTransactionAsync();
}
catch
{
    await _transactionManager.RollbackTransactionAsync();
    throw;
}
```

## 校验接口

### 1. IBusinessValidationRule - 业务校验规则接口

```csharp
public interface IBusinessValidationRule<T>
{
    /// <summary>
    /// 规则唯一标识
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// 校验级别
    /// </summary>
    ValidationLevel Level { get; }

    /// <summary>
    /// 校验失败时的默认消息
    /// </summary>
    string FailureMessage { get; }

    /// <summary>
    /// 非强制校验失败时的提示信息
    /// </summary>
    string? NonMandatoryTip { get; }

    /// <summary>
    /// 执行校验
    /// </summary>
    Task<ValidationResult<T>> ValidateAsync(
        T input,
        BusinessUnit context);
}
```

**接口特点**：
- **可识别**：每个规则有唯一的 RuleId
- **分级校验**：支持强制和非强制校验
- **异步支持**：支持复杂的异步校验逻辑
- **上下文感知**：可以访问 BusinessUnit 获取必要信息

**实现示例**：

```csharp
public class UniqueEmailRule : IBusinessValidationRule<CreateUserRequest>
{
    private readonly IUserRepository _userRepository;

    public string RuleId => "UniqueEmail";
    public ValidationLevel Level => ValidationLevel.Mandatory;
    public string FailureMessage => "邮箱已被使用";
    public string? NonMandatoryTip => null;

    public UniqueEmailRule(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ValidationResult<CreateUserRequest>> ValidateAsync(
        CreateUserRequest input,
        BusinessUnit context)
    {
        var exists = await _userRepository.ExistsAsync(u => u.Email == input.Email);

        return exists
            ? ValidationResult<CreateUserRequest>.Failure(
                new Dictionary<string, string> { ["Email"] = FailureMessage },
                Level)
            : ValidationResult<CreateUserRequest>.Success(input);
    }
}
```

### 2. IFunctionalValidationAdapter - 函数式校验适配器

```csharp
public interface IFunctionalValidationAdapter<T>
{
    /// <summary>
    /// 将函数式校验器转换为 IBusinessValidationRule
    /// </summary>
    IBusinessValidationRule<T> CreateRule(
        string ruleId,
        ValidationLevel level = ValidationLevel.Mandatory,
        string? failureMessage = null,
        string? nonMandatoryTip = null);
}
```

**设计目的**：桥接 F# 函数式校验和 C# 面向对象系统

### 3. IUnifiedGradedValidator - 统一分级校验器

```csharp
public interface IUnifiedGradedValidator
{
    /// <summary>
    /// 执行分级校验
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <param name="input">要校验的对象</param>
    /// <param name="context">业务上下文</param>
    /// <param name="nonMandatoryHandler">非强制校验失败处理策略</param>
    Task<ValidationResult<TInput>> ValidateAsync<TInput>(
        TInput input,
        BusinessUnit context,
        Func<Dictionary<string, List<GradedValidationError>>, Task<NonMandatoryChoice>>? nonMandatoryHandler = null);

    /// <summary>
    /// 注册校验规则
    /// </summary>
    void RegisterRule<T>(IBusinessValidationRule<T> rule);
}
```

**核心功能**：
- **统一入口**：整合属性校验和业务规则校验
- **分级处理**：区分强制和非强制校验
- **规则管理**：动态注册和管理校验规则
- **灵活策略**：支持自定义非强制校验处理策略

## 接口设计原则

### 1. 单一职责原则（SRP）

每个接口都有明确的单一职责：
- `IBusinessHandler`：业务处理
- `IReadOnlyRepository`：数据查询
- `IWriteOnlyRepository`：数据修改
- `ITransactionManager`：事务管理

### 2. 接口隔离原则（ISP）

将大接口拆分为小而专一的接口：
```csharp
// 好的设计 - 接口分离
public interface IReadOnlyRepository<T, K> { /* 读操作 */ }
public interface IWriteOnlyRepository<T, K> { /* 写操作 */ }

// 避免 - 臃肿的接口
public interface IRepository<T, K>
{
    // 读写操作混合在一起
    Task<T?> GetByIdAsync(K id);
    Task<T> AddAsync(T entity);
    // ... 大量方法
}
```

### 3. 依赖倒置原则（DIP）

高层模块不依赖低层模块，都依赖于抽象：
```csharp
// 高层模块依赖抽象
public class UserService
{
    private readonly IUserRepository _repository; // 依赖接口
    private readonly ITransactionManager _transactionManager; // 依赖接口
}
```

### 4. 里氏替换原则（LSP）

任何实现都可以替换接口：
```csharp
IUserRepository repository = new EfUserRepository(context);
// 也可以是
IUserRepository repository = new DapperUserRepository(connection);
```

## 扩展指南

### 1. 添加新的仓储实现

```csharp
// Dapper 实现
public class DapperReadOnlyRepository<TEntity, TKey> : IReadOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    // 实现 Dapper 特定的查询逻辑
}
```

### 2. 添加新的校验规则类型

```csharp
public interface IAsyncValidationRule<T> : IBusinessValidationRule<T>
{
    // 扩展支持更复杂的异步校验场景
    Task<ValidationResult<T>> ValidateWithCacheAsync(
        T input,
        BusinessUnit context,
        TimeSpan cacheDuration);
}
```

### 3. 添加跨仓储事务支持

```csharp
public interface IDistributedTransactionManager : ITransactionManager
{
    Task<TResult> ExecuteAcrossDatabasesAsync<TResult>(
        Dictionary<string, Func<Task>> operations,
        CancellationToken cancellationToken = default);
}
```

## 测试友好性

接口设计充分考虑了单元测试的需求：

```csharp
// 使用 Mock 库轻松创建测试替身
var mockRepository = new Mock<IUserRepository>();
mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
    .ReturnsAsync((Guid id) => new User { Id = id, Name = "Test User" });

var mockTransactionManager = new Mock<ITransactionManager>();
mockTransactionManager.Setup(t => t.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
    .ReturnsAsync(() => { /* 模拟事务成功 */ });
```

## 总结

Interface 层通过精心设计的接口体系，为 DevParadigm 框架提供了：

1. **清晰的契约定义**：每个接口都有明确的职责和边界
2. **松耦合架构**：组件间通过接口解耦，易于替换和测试
3. **可扩展性**：接口设计预留了扩展空间
4. **类型安全**：强类型设计减少运行时错误
5. **测试友好**：便于创建 Mock 和测试替身
6. **文档自明性**：接口本身就是最好的文档

这些接口构成了框架的骨架，是整个系统稳定、可扩展的基础。