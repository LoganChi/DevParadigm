# Infrastructure.Data - 基础设施数据层文档

## 概述

Infrastructure.Data 是 DevParadigm 框架的数据访问层实现，基于 Entity Framework Core 提供了完整的数据持久化解决方案。该层采用读写分离架构，支持多数据库、事务管理和高性能操作。

## 架构设计

### 读写分离架构

```
主库 (Master/Write)
├── 写入操作
├── 事务管理
└── 实时查询

从库 (Slave/Read)
├── 只读操作
├── 报表查询
└── 大数据量查询
```

**核心优势**：
- **性能优化**：读操作使用从库，减轻主库压力
- **可扩展性**：可水平扩展多个从库
- **可用性**：主库故障时，从库仍可提供读服务
- **灵活性**：支持动态切换数据源

## 核心接口定义

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

**设计理念**：
- 接口分离原则：读、写操作分别定义
- 向后兼容： IRepository 包含所有操作
- 类型安全：泛型约束确保类型正确性

### 2. IReadOnlyRepository - 只读仓储接口

```csharp
public interface IReadOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    // 基础查询
    Task<TEntity?> GetByIdAsync(
        TKey id,
        bool useMaster = false,
        CancellationToken cancellationToken = default);

    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool useMaster = false,
        CancellationToken cancellationToken = default);

    // 查询接口
    IQueryable<TEntity> Query(
        bool useMaster = false,
        bool asNoTracking = true);

    // 分页查询
    Task<PagedList<TEntity>> GetPagedListAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool useMaster = false,
        CancellationToken cancellationToken = default);

    // 统计查询
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool useMaster = false,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool useMaster = false,
        CancellationToken cancellationToken = default);
}
```

**关键特性**：
- **useMaster 参数**：强制使用主库进行查询（如查询刚插入的数据）
- **asNoTracking 优化**：默认开启，提升查询性能
- **延迟执行**：Query 返回 IQueryable 支持进一步构建

**使用示例**：

```csharp
// 基础查询
var user = await _userRepository.GetByIdAsync(userId);

// 条件查询
var activeUsers = await _userRepository.GetListAsync(u => u.IsActive);

// 复杂查询
var query = _userRepository.Query()
    .Where(u => u.IsActive)
    .Include(u => u.Profile)
    .OrderByDescending(u => u.CreatedAt);

var result = await query.ToListAsync();

// 分页查询
var pagedUsers = await _userRepository.GetPagedListAsync(
    pageIndex: 1,
    pageSize: 20,
    predicate: u => u.IsActive,
    orderBy: q => q.OrderByDescending(u => u.CreatedAt));
```

### 3. IWriteOnlyRepository - 只写仓储接口

```csharp
public interface IWriteOnlyRepository<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    // 添加操作
    Task<TEntity> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    // 更新操作
    Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    Task UpdatePartialAsync(
        TKey id,
        Dictionary<string, object> updates,
        CancellationToken cancellationToken = default);

    // 删除操作
    Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    Task DeleteByIdAsync(
        TKey id,
        CancellationToken cancellationToken = default);

    Task DeleteRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);
}
```

**设计亮点**：
- **部分更新**：`UpdatePartialAsync` 支持只更新特定字段
- **批量操作**：`AddRangeAsync`、`DeleteRangeAsync` 优化性能
- **强类型主键**：泛型 TKey 确保类型安全

**使用示例**：

```csharp
// 添加实体
var newUser = new User { Name = "张三", Email = "zhangsan@example.com" };
var created = await _userRepository.AddAsync(newUser);

// 批量添加
var users = new List<User> { /* ... */ };
await _userRepository.AddRangeAsync(users);

// 部分更新
var updates = new Dictionary<string, object>
{
    ["Name"] = "李四",
    ["UpdatedAt"] = DateTime.UtcNow
};
await _userRepository.UpdatePartialAsync(userId, updates);
```

### 4. IEntityBuilder - 实体构建器

```csharp
public interface IEntityBuilder<TInput, TEntity>
    where TEntity : class
{
    TEntity Build(TInput input, BusinessUnit unit);
}
```

**职责**：将 DTO 转换为领域实体，支持业务上下文注入。

**实现示例**：

```csharp
public class UserEntityBuilder : IEntityBuilder<CreateUserRequest, User>
{
    public User Build(CreateUserRequest input, BusinessUnit unit)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            Email = input.Email,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = unit.User?.Id
        };
    }
}
```

### 5. ITransactionManager - 事务管理器

```csharp
public interface ITransactionManager
{
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

**设计模式**：模板方法模式，确保事务的正确管理。

**使用示例**：

```csharp
// 自动事务管理
var result = await _transactionManager.ExecuteInTransactionAsync(async () =>
{
    var user = await _userRepository.AddAsync(userEntity);
    var profile = await _profileRepository.AddAsync(profileEntity);
    return new { User = user, Profile = profile };
});

// 手动事务管理
await _transactionManager.BeginTransactionAsync();
try
{
    await _userRepository.AddAsync(userEntity);
    await _profileRepository.AddAsync(profileEntity);
    await _transactionManager.CommitTransactionAsync();
}
catch
{
    await _transactionManager.RollbackTransactionAsync();
    throw;
}
```

## EF Core 实现

### 1. EfReadOnlyRepository - 只读仓储实现

```csharp
public class EfReadOnlyRepository<TEntity, TKey> :
    IReadOnlyRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly DbContext _context;
    private readonly bool _defaultAsNoTracking;

    public EfReadOnlyRepository(
        IReadDbContextFactory contextFactory,
        bool defaultAsNoTracking = true)
    {
        _context = contextFactory.CreateDbContext();
        _defaultAsNoTracking = defaultAsNoTracking;
    }

    public async Task<TEntity?> GetByIdAsync(
        TKey id,
        bool useMaster = false,
        CancellationToken ct = default)
    {
        var context = useMaster
            ? _masterContextFactory.CreateDbContext()
            : _context;

        var query = context.Set<TEntity>();

        if (_defaultAsNoTracking && !useMaster)
        {
            query = query.AsNoTracking();
        }

        return await query.FindAsync(new object[] { id }, ct);
    }
}
```

**关键实现细节**：
- **动态上下文切换**：根据 useMaster 参数选择数据源
- **性能优化**：默认使用 AsNoTracking
- **异步支持**：所有操作都支持异步和取消令牌

### 2. EfWriteOnlyRepository - 只写仓储实现

```csharp
public class EfWriteOnlyRepository<TEntity, TKey> :
    IWriteOnlyRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly DbContext _context;

    public EfWriteOnlyRepository(IWriteDbContextFactory contextFactory)
    {
        _context = contextFactory.CreateDbContext();
    }

    public async Task<TEntity> AddAsync(
        TEntity entity,
        CancellationToken ct = default)
    {
        var trackedEntry = await _context.Set<TEntity>().AddAsync(entity, ct);
        return trackedEntry.Entity;
    }

    public async Task UpdatePartialAsync(
        TKey id,
        Dictionary<string, object> updates,
        CancellationToken ct = default)
    {
        // 先查询实体（不跟踪）
        var entity = await _context.Set<TEntity>()
            .AsNoTracking()
            .FindAsync(new object[] { id }, ct);

        if (entity == null)
            throw new EntityNotFoundException(typeof(TEntity).Name, id.ToString()!);

        // 创建只更新指定字段的实体
        var trackedEntity = new Dictionary<string, object>();

        // 设置主键
        var keyProperty = typeof(TEntity)
            .GetProperties()
            .First(p => p.Name == "Id");
        trackedEntity[keyProperty.Name] = id;

        // 应用更新
        foreach (var update in updates)
        {
            trackedEntity[update.Key] = update.Value;
        }

        // 附加并标记部分更新
        var entry = _context.Attach(trackedEntity);
        entry.State = EntityState.Unchanged;

        foreach (var update in updates)
        {
            entry.Property(update.Key).IsModified = true;
        }
    }
}
```

**实现亮点**：
- **部分更新优化**：避免全量更新带来的性能问题
- **异常处理**：完善的错误处理机制
- **审计支持**：可扩展的审计字段自动填充

### 3. EfTransactionManager - 事务管理实现

```csharp
public class EfTransactionManager : ITransactionManager, IDisposable
{
    private readonly DbContext _context;
    private IDbContextTransaction? _transaction;

    public EfTransactionManager(IWriteDbContextFactory contextFactory)
    {
        _context = contextFactory.CreateDbContext();
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken ct = default)
    {
        using var transaction = await _context.Database
            .BeginTransactionAsync(isolationLevel, ct);

        try
        {
            var result = await operation();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
```

**特性**：
- **自动资源管理**：使用 using 确保事务正确释放
- **异常安全**：异常时自动回滚
- **可配置隔离级别**：支持不同的事务隔离级别

## 扩展工具

### 1. EfCoreExtensions - EF Core 扩展方法

```csharp
public static class EfCoreExtensions
{
    // 条件包含
    public static IQueryable<T> IncludeIf<T, TProperty>(
        this IQueryable<T> source,
        Expression<Func<T, TProperty>> navigationPropertyPath,
        bool condition)
        where T : class
    {
        return condition
            ? source.Include(navigationPropertyPath)
            : source;
    }

    // 条件过滤
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> source,
        Expression<Func<T, bool>> predicate,
        bool condition)
        where T : class
    {
        return condition
            ? source.Where(predicate)
            : source;
    }

    // 分页扩展
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        int pageIndex,
        int pageSize,
        CancellationToken ct = default)
    {
        var totalCount = await source.CountAsync(ct);
        var items = await source
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedList<T>(items, totalCount, pageIndex, pageSize);
    }
}
```

**使用示例**：

```csharp
// 动态查询构建
var query = _userRepository.Query()
    .IncludeIf(u => u.Profile, includeProfile)
    .WhereIf(u => u.IsActive, filterInactive)
    .WhereIf(u => u.DepartmentId == departmentId, filterByDepartment);

var result = await query.ToPagedListAsync(pageIndex, pageSize);
```

## 性能优化策略

### 1. 查询优化

```csharp
// 使用 AsNoTracking 进行只读查询
var users = await _context.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .ToListAsync();

// 使用 Select 投影，避免查询不必要字段
var userDtos = await _context.Users
    .Where(u => u.IsActive)
    .Select(u => new UserDto
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email
        // 只查询需要的字段
    })
    .ToListAsync();

// 批量加载避免 N+1 问题
var usersWithOrders = await _context.Users
    .Include(u => u.Orders)
    .ToListAsync();
```

### 2. 批量操作优化

```csharp
// 使用 BulkExtensions 进行大批量操作
await _context.Users
    .Where(u => !u.IsActive)
    .BatchUpdateAsync(u => new User
    {
        IsActive = false,
        UpdatedAt = DateTime.UtcNow
    });

await _context.Users
    .Where(u => u.LastLoginAt < DateTime.UtcNow.AddYears(-1))
    .BatchDeleteAsync();
```

### 3. 连接池配置

```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString,
        sqlOptions => sqlOptions
            .EnableRetryOnFailure(maxRetryCount: 3)
            .CommandTimeout(30));

    // 配置连接池
    options.EnableSensitiveDataLogging(false);
    options.EnableServiceProviderCaching();
});
```

## 多数据库支持

### 1. 读写分离配置

```csharp
public class AppDbContextFactory : IReadDbContextFactory, IWriteDbContextFactory
{
    private readonly IConfiguration _configuration;

    DbContext IReadDbContextFactory.CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_configuration.GetConnectionString("ReadOnlyConnection"))
            .Options;

        return new AppDbContext(options);
    }

    DbContext IWriteDbContextFactory.CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_configuration.GetConnectionString("WriteConnection"))
            .Options;

        return new AppDbContext(options);
    }
}
```

### 2. 数据库迁移策略

```csharp
// 主库迁移
await context.Database.MigrateAsync();

// 从库只读，不需要迁移
// 可以使用数据库快照或复制机制保持数据同步
```

## 监控和日志

### 1. 查询日志

```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseLoggerFactory(loggerFactory);
    options.EnableSensitiveDataLogging(false);
    options.EnableDetailedErrors(detailedErrorsEnabled);
});
```

### 2. 性能监控

```csharp
public class PerformanceInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Executing SQL: {CommandText}", command.CommandText);

        return base.ReaderExecuting(command, eventData, result);
    }
}
```

## 最佳实践

1. **读写分离**：读操作使用 `IReadOnlyRepository`，写操作使用 `IWriteOnlyRepository`
2. **事务最小化**：保持事务范围尽可能小
3. **异步优先**：始终使用异步方法
4. **延迟加载谨慎使用**：考虑使用 Eager Loading 或 Projection
5. **批量操作优化**：大量数据使用批量 API
6. **连接管理**：合理配置连接池大小

## 总结

Infrastructure.Data 通过精心设计的抽象层，提供了：

- **高性能**：读写分离、批量操作、查询优化
- **可扩展**：支持多数据库、插件化架构
- **易测试**：接口抽象便于单元测试
- **类型安全**：强类型设计减少运行时错误
- **事务一致性**：完善的事务管理机制
- **监控友好**：内置日志和性能监控支持

这为 DevParadigm 框架提供了坚实的数据访问基础，是现代化企业级应用的最佳实践。