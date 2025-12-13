# DevParadigm 项目文档

## 文档目录

本文档包含了 DevParadigm 框架的详细说明，涵盖了从设计理念到具体实现的各个方面。

### 📋 核心文档

1. **[项目结构总览](./Project-Structure.md)** - **新！**
   - 完整的项目组成分析
   - 混合语言架构详解
   - 依赖关系图和目录结构
   - 技术栈详情和设计亮点

2. **[DevParadigm 核心框架文档](./DevParadigm-Core.md)**
   - 框架概述和架构设计
   - Basement 基础层详解
   - Common 通用组件说明
   - BaseBusinessHandler 业务处理器核心
   - 使用示例和最佳实践

3. **[BusinessValidation - F# 函数式验证库](./BusinessValidation-FSharp.md)**
   - 函数式验证概念
   - ValidatorCombinators 组合子
   - F# 与 C# 互操作
   - 高级特性和性能优化

4. **[Infrastructure.Data - 基础设施数据层](./Infrastructure-Data.md)**
   - 读写分离架构
   - EF Core 实现详解
   - 仓储模式实现
   - 事务管理机制
   - 性能优化策略

5. **[Interface - 接口契约层](./Interface-Contracts.md)**
   - 核心接口定义
   - 设计原则说明
   - 接口扩展指南
   - 测试友好性设计

6. **[F# 验证优化方案](./FSharp-Validation-Optimization.md)**
   - 函数式验证性能对比
   - 优化策略和最佳实践
   - 内存管理和缓存方案

### 🚀 快速开始

**想要立刻开始？** 查看 **[快速开始指南](./Quick-Start.md)** - 5分钟运行示例，10分钟创建第一个业务处理器！

如果你想深入了解框架，建议按以下顺序阅读：

1. **[项目结构总览](./Project-Structure.md)** - 了解整体架构和混合语言设计
2. **[DevParadigm 核心框架文档](./DevParadigm-Core.md)** - 掌握基础组件和设计模式
3. **[Interface 接口契约层](./Interface-Contracts.md)** - 理解核心接口定义
4. **[Infrastructure.Data 数据层](./Infrastructure-Data.md)** - 学习读写分离和数据访问模式
5. **[BusinessValidation 验证库](./BusinessValidation-FSharp.md)** - 掌握函数式验证（推荐）

### 🏗️ 架构总览

```
DevParadigm 混合语言架构

┌─────────────────────────────────────────────┐
│         应用层 (Application)                │
│    DevParadigm.Console / Web APIs          │
├─────────────────────────────────────────────┤
│         示例层 (Example)                   │
│    CreateOrderHandler / BusinessRules      │
├─────────────────────────────────────────────┤
│       验证层 (Validation) - F#             │
│    ValidatorCombinators | 函数式验证        │
├─────────────────────────────────────────────┤
│      业务处理层 (Handlers) - C#            │
│    BaseBusinessHandler | BusinessUnit      │
├─────────────────────────────────────────────┤
│        核心层 (Core) - C#                  │
│    UserInfo | IBusinessHandler | Results   │
├─────────────────────────────────────────────┤
│      基础设施层 (Infrastructure) - C#       │
│   Repository | TransactionManager | EF     │
├─────────────────────────────────────────────┤
│         数据存储层 (Data)                  │
│      读写分离 | SQL Server / MySQL         │
└─────────────────────────────────────────────┘
```

**项目组成**：
- **DevParadigm** - 核心框架层 (C#)
- **BusinessValidation** - 函数式验证层 (F#)
- **DevParadigm.Example** - 示例实现层 (C#)
- **DevParadigm.Console** - 控制台应用 (C#)

### 🎯 设计理念

1. **混合语言编程**
   - C# 面向对象 + F# 函数式编程
   - 各展所长，优势互补
   - 无缝集成，透明使用

2. **函数式验证优先**
   - 类型安全的验证逻辑
   - 组合子模式构建复杂规则
   - 不可变性避免副作用

3. **AI 友好架构**
   - 标准化的抽象层
   - 清晰的设计模式
   - 一致的命名约定
   - 便于理解和生成代码

4. **现代化开发实践**
   - 领域驱动设计 (DDD)
   - 读写分离 (CQRS)
   - 异步编程优先
   - 依赖注入

5. **高性能设计**
   - 读写分离架构
   - 批量操作优化
   - 查询性能优化
   - 事务自动管理

### 💡 核心概念速查

| 概念 | 定义 | 作用 |
|------|------|------|
| **BaseBusinessHandler** | 业务处理器基类 (C#) | 封装标准业务流程：校验→构建→事务→返回 |
| **Validator<'T>** | 函数式验证器 (F#) | 类型安全的验证逻辑，支持组合 |
| **ValidatorCombinators** | 验证器组合子 (F#) | 通过 `all`、`when'` 等组合复杂规则 |
| **BusinessUnit** | 业务上下文单元 (C#) | 携带用户信息、验证结果、扩展数据 |
| **FunctionalAdapter** | F#到C#适配器 | 无缝集成函数式验证到C#项目 |
| **IRepository (Read/Write)** | 分离式仓储接口 | 支持读写分离的数据访问抽象 |
| **TransactionManager** | 事务管理器 | 自动化事务边界管理 |

### 🔧 技术栈

- **.NET 9.0** - 最新 .NET 平台
- **C# 13** - 主要开发语言（框架基础设施）
- **F# 8** - 函数式验证逻辑（业务规则验证）
- **Entity Framework Core 9** - ORM 框架
- **依赖注入** - IoC 容器
- **读写分离** - 性能优化架构
- **异步编程** - `async/await` 模式

### 📚 学习资源

#### 设计模式
- [Template Method Pattern - 模板方法模式](https://refactoring.guru/design-patterns/template-method)
- [Repository Pattern - 仓储模式](https://martinfowler.com/eaaCatalog/repository.html)
- [CQRS Pattern - 命令查询责任分离](https://martinfowler.com/bliki/CQRS.html)
- [Strategy Pattern - 策略模式](https://refactoring.guru/design-patterns/strategy)

#### 函数式编程
- [F# for fun and profit](https://fsharpforfunandprofit.com/)
- [Combinator Pattern](https://wiki.haskell.org/Combinator)

#### 领域驱动设计
- [DDD Quickly](https://www.infoq.com/minibooks/domain-driven-design-quickly)
- [Domain-Driven Design: Tackling Complexity](https://domainlanguage.com/ddd/)

### 🤝 贡献指南

如果你想为文档做贡献：

1. 确保内容准确、清晰
2. 使用 Markdown 格式
3. 提供代码示例
4. 保持与其他文档的一致性

### 📞 支持

如有问题或建议：
- 提交 Issue
- 发起 Discussion
- 联系维护者

---

**注意**：这是一个持续更新的文档，随着框架的发展会不断补充和完善。