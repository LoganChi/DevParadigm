# DevParadigm

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![F#](https://img.shields.io/badge/F%23-8-blue.svg)](https://fsharp.org/)

> 一个基于 .NET 9 的现代化企业级开发框架，融合面向对象与函数式编程范式。专为 AI 辅助开发设计的标准化架构体系。

## 🎯 项目概述

DevParadigm 是一个高度模块化的企业级开发框架，**专门为 AI 辅助开发场景设计**。在 AI 开发盛行的时代，拥有一套自己的抽象层和标准化架构至关重要。本项目旨在：

- **为 AI 提供清晰的开发上下文**：通过标准化的模式和抽象，让 AI 快速理解项目结构
- **加速 AI 辅助开发**：减少 AI 生成代码时的猜测，提高准确性和一致性
- **构建个人开发体系**：作为跨语言、跨框架的基础，方便移植到其他技术栈

本项目采用了多种先进的设计模式和编程范式，包括：

- **读写分离仓储模式**：优化数据访问性能
- **模板方法模式**：标准化业务处理流程
- **函数式验证**：使用 F# 提供强大的类型安全验证
- **领域驱动设计(DDD)**：清晰的分层架构

## ✨ 核心特性

### 🏗️ 架构设计

- **分层架构**：严格按照 DDD 分层，职责清晰
- **读写分离**：`IReadOnlyRepository` 和 `IWriteOnlyRepository` 独立设计
- **事务管理**：跨仓储的事务一致性保证
- **依赖注入友好**：基于接口的设计，易于测试和扩展

### 🔍 校验体系

- **分级校验**：强制校验(Required)和非强制校验(Optional)
- **混合编程范式**：C# 属性校验 + F# 函数式校验
- **组合子模式**：灵活的校验规则组合
- **统一接口**：`IUnifiedGradedValidator` 整合不同校验方式

### 📦 业务处理

- **标准化流程**：`BaseBusinessHandler` 封装完整业务流程
- **5个扩展点**：校验、构建、读取、写入、映射，灵活定制
- **错误处理**：统一的 `ApiResult<T>` 返回格式
- **上下文管理**：`BusinessUnit` 和 `UserInfo` 封装业务上下文

## 🚀 快速开始

### 环境要求

- .NET 9.0 SDK
- Visual Studio 2022 或 JetBrains Rider
- (可选) SQL Server 或其他 EF Core 支持的数据库

### 安装运行

```bash
# 克隆仓库
git clone https://github.com/yourusername/DevParadigm.git
cd DevParadigm

# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行项目
dotnet run --project DevParadigm
```

## 🤖 AI 辅助开发指南

### 如何与 AI 有效协作

本项目的设计初衷之一就是让 AI 能够更好地理解和参与开发。以下是使用 AI 辅助开发时的最佳实践：

#### 1. **提供清晰的上下文**

当向 AI 提问时，总是包含以下信息：

```
我正在使用 DevParadigm 框架开发项目。该框架使用：
- BaseBusinessHandler<TInput, TEntity, TOutput, TKey> 作为业务处理器基类
- 读写分离的仓储模式 (IReadOnlyRepository/IWriteOnlyRepository)
- 分级校验体系 (Required/Optional)
- F# 函数式验证 (通过 IUnifiedGradedValidator 集成)

我的问题是：...
```

#### 2. **使用标准术语**

使用框架定义的标准术语，避免歧义：

| 术语 | 含义 |
|------|------|
| Handler | 业务处理器，继承自 BaseBusinessHandler |
| Repository | 数据仓储，分为读写两种 |
| Validation | 校验，支持属性校验和函数式校验 |
| BusinessUnit | 业务上下文单元 |
| Entity | 数据实体 |
| DTO | 数据传输对象 |

#### 3. **请求代码示例的模板**

```
请帮我实现一个 [业务名称] Handler：

输入：[描述输入 DTO]
输出：[描述输出 DTO]
业务规则：[列出关键业务规则]
特殊要求：[如有，例如事务、缓存等]

请基于 BaseBusinessHandler 实现，包含：
1. 输入 DTO 定义（包含必要的校验特性）
2. 输出 DTO 定义
3. Handler 类的完整实现
4. 相关的仓储接口定义（如需要）
```

#### 4. **定位问题的标准方式**

```
我在项目中遇到了一个问题：

文件路径：DevParadigm/Infrastructure/Data/Repositories/Ef/EfRepository.cs
行号：约 50 行
问题：[描述具体问题]
错误信息：[如有]

相关上下文：
- 使用的实体类型：[实体名称]
- 调用的方法：[方法签名]
- 期望行为：[描述期望结果]
- 实际行为：[描述实际结果]
```

### AI 常见任务示例

#### 创建新的业务处理器

> "请基于 DevParadigm 框架创建一个订单创建处理器。订单包含订单号、客户ID、商品列表和总金额。需要验证客户存在性、商品库存，并确保订单号唯一。"

#### 添加复杂校验规则

> "请为用户注册场景添加 F# 函数式校验。需要验证：1. 密码强度（包含大小写、数字、特殊字符）；2. 邮箱属于允许的企业域名列表；3. 用户名不包含敏感词。"

#### 优化查询性能

> "我的用户列表查询很慢。当前使用 EfReadOnlyRepository。请帮我添加分页、排序和筛选功能，并考虑添加缓存策略。"

### 注意事项

1. **始终说明框架版本**：不同版本的 API 可能有差异
2. **提供相关代码片段**：让 AI 了解当前的实现
3. **明确请求范围**：是只需要代码，还是也需要解释
4. **逐步迭代**：复杂功能可以分步骤让 AI 实现

## 📚 使用示例

### 定义业务处理器

```csharp
// 定义输入 DTO
public record CreateUserRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [GradedValidation(ValidationLevel.Optional)] string? NickName
);

// 定义输出 DTO
public record UserResponse(
    Guid Id,
    string Email,
    string NickName,
    DateTime CreatedAt
);

// 实现业务处理器
public class CreateUserHandler :
    BaseBusinessHandler<CreateUserRequest, User, UserResponse, Guid>
{
    private readonly IUserRepository _userRepository;

    public CreateUserHandler(
        IUnifiedGradedValidator validator,
        IEntityBuilder<User> entityBuilder,
        IUserRepository userRepository,
        ITransactionManager transactionManager,
        IMapper mapper)
        : base(validator, entityBuilder, transactionManager, mapper)
    {
        _userRepository = userRepository;
    }

    // 实现抽象方法
    protected override async Task<User> ReadEntityAsync(Guid id, CancellationToken ct = default)
        => await _userRepository.GetByIdAsync(id, ct);

    protected override async Task<Guid> WriteEntityAsync(User entity, CancellationToken ct = default)
    {
        await _userRepository.AddAsync(entity, ct);
        await _transactionManager.SaveChangesAsync(ct);
        return entity.Id;
    }

    protected override UserResponse MapToResponse(User entity)
        => new(entity.Id, entity.Email, entity.NickName, entity.CreatedAt);
}
```

### 使用 F# 函数式校验

```fsharp
// 在 F# 中定义复杂校验规则
let validateEmailDomain (allowedDomains: string list) (email: string) =
    let domain = email.Split('@').[1]
    if List.contains domain allowedDomains then
        Validation.Success email
    else
        Validation.Failure ["Email domain not allowed"]

// 组合校验规则
let validateUserRequest (request: CreateUserRequest) =
    validateEmail
    |> andThen (validatePasswordStrength)
    |> andThen (validateEmailDomain ["example.com"; "test.com"])
```

## 🏛️ 项目结构

```
DevParadigm/
├── BusinessValidation/          # F# 函数式验证库
│   ├── Basement/
│   │   ├── ValidationResult.fs     # 校验结果类型
│   │   ├── Validator.fs            # 函数式校验器
│   │   └── ValidatorCombinators.fs # 校验器组合子
│   └── BusinessValidation.fsproj   # F# 项目文件
│
├── DevParadigm/               # 主项目 (C#)
│   ├── Basement/              # 基础设施层
│   │   ├── Handler/           # 业务处理器
│   │   └── Units/             # 业务单元
│   ├── Common/                # 通用组件
│   │   ├── Attribute/         # 自定义特性
│   │   ├── Enum/              # 枚举定义
│   │   └── Results/           # 统一返回结果
│   ├── Infrastructure/        # 基础设施层
│   │   └── Data/              # 数据访问层
│   │       ├── Repositories/  # 仓储实现
│   │       ├── Contexts/      # 上下文构建器
│   │       └── Transactions/  # 事务管理
│   └── Interface/             # 接口定义层
│
└── README.md
```

## 🔧 技术栈

- **框架**: .NET 9.0
- **语言**: C# 13, F# 8
- **ORM**: Entity Framework Core 9.0.11
- **设计模式**: 模板方法、仓储、CQRS 思想
- **编程范式**: 面向对象 + 函数式

## 📋 TODO

- [ ] 添加单元测试项目 (xUnit)
- [ ] 添加集成测试
- [ ] 完善 API 文档
- [ ] 添加缓存层支持
- [ ] 集成日志框架
- [ ] 添加健康检查端点
- [ ] 支持多数据库

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！请确保：

1. 代码符合项目的编码规范
2. 添加必要的测试
3. 更新相关文档
4. 提交信息遵循 Conventional Commits 规范

## 📄 许可证

本项目采用 [MIT](LICENSE) 许可证。

## 🌐 跨语言迁移指南

### 设计模式映射

本框架的核心设计模式可以轻松迁移到其他技术栈：

| 概念 | C#/.NET | Java/Spring | TypeScript/Node.js | Python/Django |
|------|----------|------------|-------------------|--------------|
| BaseBusinessHandler | Abstract Class | Abstract Class / Service | Abstract Class | Abstract Base Class |
| Repository | Interface | Interface | Interface | Abstract Base Class |
| Validation Attributes | Data Annotations | JSR-380 | Decorators | Pydantic |
| F# Validation | Functional Pipeline | Vavr / FunctionalJava | fp-ts | Pydantic + functional |
| DI Container | built-in DI | Spring IoC | Inversify | dependency-injector |

### 迁移步骤

1. **定义核心抽象**
   ```typescript
   // TypeScript 示例
   abstract class BaseHandler<I, E, O, K> {
     abstract validate(input: I): Promise<ValidationResult>;
     abstract buildEntity(input: I): Promise<E>;
     abstract execute(input: I): Promise<O>;
   }
   ```

2. **实现基础设施层**
   - 仓储接口和实现
   - 校验框架集成
   - 事务管理

3. **添加语言特有优化**
   - TypeScript：利用类型系统和装饰器
   - Python：利用 dataclass 和 type hints
   - Java：利用注解和 Spring Boot

## 💬 反馈与建议

如果你有任何问题或建议，欢迎通过以下方式联系：

- 提交 [Issue](https://github.com/yourusername/DevParadigm/issues)
- 发送邮件至：your.email@example.com

---

⭐ 如果这个项目对你有帮助，请给它一个星标！