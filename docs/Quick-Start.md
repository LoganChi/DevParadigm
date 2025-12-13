# DevParadigm 快速开始指南

## 🚀 5分钟快速体验

### 前置条件

- .NET 9.0 SDK
- Visual Studio 2022 或 VS Code
- SQL Server（或其他 EF Core 支持的数据库）

### 运行示例项目

```bash
# 1. 克隆项目
git clone https://github.com/your-repo/DevParadigm.git
cd DevParadigm

# 2. 还原 NuGet 包
dotnet restore

# 3. 配置数据库连接字符串
# 编辑 DevParadigm.Console/appsettings.json
{
  "ConnectionStrings": {
    "WriteConnection": "Server=(localdb)\\mssqllocaldb;Database=DevParadigm;Trusted_Connection=true;",
    "ReadConnection": "Server=(localdb)\\mssqllocaldb;Database=DevParadigm_Read;Trusted_Connection=true;"
  }
}

# 4. 运行数据库迁移
cd DevParadigm.Console
dotnet ef database update

# 5. 运行示例
dotnet run
```

## 💡 核心概念快速理解

### 1. 混合语言架构

DevParadigm 采用 C# + F# 混合编程：

```csharp
// C# 端：业务流程编排
public class CreateOrderHandler : BaseBusinessHandler<CreateOrderInput, Order, CreateOrderOutput, Guid>
{
    protected override async Task<Order> ExecuteDataOperationAsync(Order entity, BusinessUnit unit, CancellationToken ct)
    {
        await _orderRepository.AddAsync(entity, ct);
        await _transactionManager.SaveChangesAsync(ct);
        return entity;
    }
}
```

```fsharp
// F# 端：函数式验证
let validateOrderAll = all [
    validateCustomerId
    validateProductItems
    validateStock
    validateDailyOrderLimit
]
```

### 2. 标准业务流程

`BaseBusinessHandler` 封装了完整的业务处理流程：

```
输入请求
    ↓
[1] 参数校验 (ValidationAttributes)
    ↓
[2] 业务规则验证 (F# Validators)
    ↓
[3] 非强制校验处理 (NonMandatoryHandler)
    ↓
[4] 构建实体 (EntityBuilder)
    ↓
[5] 实体后处理 (PostProcessEntity)
    ↓
[6] 事务内执行数据操作
    ↓
[7] 映射输出 (MapToResponse)
    ↓
返回结果
```

## 📝 创建第一个业务处理器

### 步骤 1：定义输入输出

```csharp
// DTOs/CreateProductInput.cs
public record CreateProductInput(
    [Required(ErrorMessage = "产品名称不能为空")]
    string Name,

    [Range(0.01, double.MaxValue, ErrorMessage = "价格必须大于0")]
    decimal Price,

    [Range(0, int.MaxValue, ErrorMessage = "库存不能为负数")]
    int Stock
);

// DTOs/CreateProductOutput.cs
public record CreateProductOutput(
    Guid Id,
    string Name,
    decimal Price,
    int Stock
);
```

### 步骤 2：创建实体

```csharp
// Entities/Product.cs
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
```

### 步骤 3：创建 F# 验证器

```fsharp
// BusinessValidation/ProductValidation.fs
module ProductValidation =
    open BusinessValidation.Types

    let validateName name _ =
        if String.IsNullOrWhiteSpace(name) then
            Failure ["产品名称不能为空"]
        elif name.Length > 100 then
            Failure ["产品名称不能超过100个字符"]
        else
            Success name

    let validatePrice price _ =
        if price <= 0m then
            Failure ["价格必须大于0"]
        elif price > 1000000m then
            Failure ["价格不能超过100万"]
        else
            Success price

    let validateProduct = all [
        (fun input _ -> validateName input.Name null)
        (fun input _ -> validatePrice input.Price null)
    ]
```

### 步骤 4：实现业务处理器

```csharp
// Handlers/CreateProductHandler.cs
public class CreateProductHandler :
    BaseBusinessHandler<CreateProductInput, Product, CreateProductOutput, Guid>
{
    private readonly IWriteOnlyRepository<Product, Guid> _repository;
    private readonly ITransactionManager _transactionManager;

    public CreateProductHandler(
        IWriteOnlyRepository<Product, Guid> repository,
        ITransactionManager transactionManager)
    {
        _repository = repository;
        _transactionManager = transactionManager;
    }

    protected override async Task<ValidationResult<CreateProductInput>>
        ValidateBusinessLogicAsync(CreateProductInput input, BusinessUnit unit, CancellationToken ct)
    {
        // 调用 F# 验证器
        var validationResult = await BusinessValidation.ProductValidation
            .validateProduct(input, unit);

        return validationResult.IsValid
            ? ValidationResult<CreateProductInput>.Success(input)
            : ValidationResult<CreateProductInput>.Failure(
                validationResult.Errors,
                ValidationLevel.Mandatory);
    }

    protected override async Task<Product> BuildEntityAsync(
        CreateProductInput input, BusinessUnit unit, CancellationToken ct)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            Price = input.Price,
            Stock = input.Stock,
            CreatedAt = DateTime.UtcNow
        };
    }

    protected override async Task<Product> PostProcessEntityAsync(
        Product entity, BusinessUnit unit, CancellationToken ct)
    {
        // 设置审计字段
        entity.CreatedBy = unit.User?.Id;
        return entity;
    }

    protected override async Task<Product> ExecuteDataOperationAsync(
        Product entity, BusinessUnit unit, CancellationToken ct)
    {
        return await _repository.AddAsync(entity, ct);
    }

    protected override async Task<bool> SaveChangesAsync(
        BusinessUnit unit, CancellationToken ct)
    {
        return await _transactionManager.SaveChangesAsync(ct) > 0;
    }

    protected override CreateProductOutput MapToResponse(Product entity)
    {
        return new CreateProductOutput(
            entity.Id,
            entity.Name,
            entity.Price,
            entity.Stock
        );
    }
}
```

### 步骤 5：注册服务

```csharp
// DependencyInjection/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDevParadigm(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册数据库上下文
        services.AddDbContext<WriteDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("WriteConnection")));

        services.AddDbContext<ReadDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ReadConnection")));

        // 注册仓储
        services.AddScoped<IWriteOnlyRepository<Product, Guid>, EfWriteOnlyRepository<Product, Guid>>();
        services.AddScoped<IReadOnlyRepository<Product, Guid>, EfReadOnlyRepository<Product, Guid>>();

        // 注册事务管理器
        services.AddScoped<ITransactionManager, EfTransactionManager>();

        // 注册业务处理器
        services.AddScoped<IBusinessHandler<CreateProductInput, CreateProductOutput>, CreateProductHandler>();

        // 注册统一验证器
        services.AddScoped<IUnifiedGradedValidator, UnifiedGradedValidator>();

        return services;
    }
}
```

### 步骤 6：使用业务处理器

```csharp
// Program.cs
var services = new ServiceCollection();
services.AddDevParadigm(configuration);

var serviceProvider = services.BuildServiceProvider();

// 获取业务处理器
var handler = serviceProvider.GetRequiredService<IBusinessHandler<CreateProductInput, CreateProductOutput>>();

// 创建输入
var input = new CreateProductInput(
    Name: "iPhone 15 Pro",
    Price: 999.99m,
    Stock: 100
);

// 执行业务逻辑
var userInfo = new UserInfo("user123", "张三", 1);
var result = await handler.ExecuteAsync(input, userInfo);

// 处理结果
if (result.Success)
{
    Console.WriteLine($"产品创建成功！ID: {result.Data!.Id}");
}
else
{
    Console.WriteLine($"创建失败：{result.Message}");
    if (result.Errors.Any())
    {
        Console.WriteLine("错误详情：");
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"- {error}");
        }
    }
}
```

## 🔧 高级特性

### 1. 读写分离

```csharp
// 强制使用主库查询刚插入的数据
var product = await _readOnlyRepository.GetByIdAsync(id, useMaster: true);

// 使用从库进行一般查询
var list = await _readOnlyRepository.GetPagedListAsync(1, 20);
```

### 2. 分级验证

```csharp
// 定义分级校验
public class CreateOrderRequest
{
    [Required]
    [GradedValidation(ValidationLevel.Mandatory)]
    public Guid CustomerId { get; set; }

    [Range(1, 100)]
    [GradedValidation(ValidationLevel.NonMandatory, NonMandatoryTip = "批量订购可能享受折扣")]
    public int Quantity { get; set; }
}
```

### 3. 非强制校验处理

```csharp
protected override async Task<NonMandatoryChoice> NonMandatoryHandler(
    Dictionary<string, List<GradedValidationError>> errors)
{
    // 记录警告
    _logger.LogWarning("业务执行警告：{@Errors}", errors);

    // 根据业务规则决定是否继续
    foreach (var error in errors.Values.SelectMany(v => v))
    {
        if (error.PropertyName == "Security")
            return NonMandatoryChoice.TerminateProcess;
    }

    return NonMandatoryChoice.ConfirmContinue;
}
```

## 🎯 最佳实践

### 1. 验证器设计原则

- **纯函数**：验证器不应有副作用
- **组合性**：通过组合子构建复杂验证
- **类型安全**：充分利用 F# 的类型系统

### 2. 业务处理器设计

- **单一职责**：每个处理器处理一种业务场景
- **异步优先**：所有 I/O 操作使用异步
- **事务最小化**：保持事务范围尽可能小

### 3. 性能优化

- 使用 `AsNoTracking()` 进行只读查询
- 批量操作使用 `AddRangeAsync`、`UpdateRangeAsync`
- 合理使用缓存减少数据库访问

## ❓ 常见问题

### Q: 如何在 F# 验证器中访问数据库？

A: 通过 `BusinessUnit.Extensions` 传递必要的数据：

```csharp
// C# 端准备数据
var stock = await _stockRepository.GetStockAsync(productId);
unit.Extensions["Stock"] = stock;

// F# 端使用
let validateStock input (unit: BusinessUnit) =
    let stock = unit.Extensions.["Stock"] :?> int
    if input.Quantity > stock then
        Failure $"库存不足，当前库存：{stock}"
    else
        Success input
```

### Q: 如何处理复杂的业务规则？

A: 将复杂规则分解为多个简单的验证器，然后组合：

```fsharp
let validateOrderComplex = all [
    // 基础验证
    validateBasicOrder

    // 条件验证
    when' isVipCustomer validateVipRules
    when' isNewCustomer validateNewCustomerRules

    // 跨领域验证
    validateInventory
    validateCreditLimit
]
```

### Q: 如何添加新的验证级别？

A: 扩展 `ValidationLevel` 枚举并更新相关处理逻辑：

```csharp
public enum ValidationLevel
{
    Mandatory = 1,      // 必须通过
    Recommended = 2,    // 建议遵守
    Optional = 3,       // 可选验证
    Informational = 4   // 仅信息提示
}
```

## 📚 下一步

- 查看 [项目结构总览](./Project-Structure.md) 了解完整架构
- 阅读 [DevParadigm 核心框架文档](./DevParadigm-Core.md) 深入理解设计
- 学习 [BusinessValidation F# 验证库](./BusinessValidation-FSharp.md) 掌握函数式验证
- 探索 [Infrastructure.Data 数据层](./Infrastructure-Data.md) 了解数据访问模式

## 💬 获取帮助

- 提交 [Issue](https://github.com/your-repo/DevParadigm/issues)
- 参与 [讨论](https://github.com/your-repo/DevParadigm/discussions)
- 查看 [示例代码](../DevParadigm.Example)