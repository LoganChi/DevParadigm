using DevParadigm.Basement.Units;
using DevParadigm.Common.Enum;
using DevParadigm.Common.Results;
using DevParadigm.Interface;

namespace DevParadigm.Basement.Handler;

/// <summary>
/// 业务处理器基类（模板方法模式封装全流程）
/// </summary>
/// <typeparam name="TInput">输入DTO类型</typeparam>
/// <typeparam name="TEntity">领域实体类型</typeparam>
/// <typeparam name="TOutput">输出结果类型</typeparam>
/// <typeparam name="TKey">实体主键类型</typeparam>
public abstract class BaseBusinessHandler<TInput, TEntity, TOutput, TKey> : IBusinessHandler<TInput, TOutput>
    where TInput : class
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    protected readonly IUnifiedGradedValidator Validator;
    protected readonly IEntityBuilder<TInput, TEntity> EntityBuilder;
    protected readonly IRepository<TEntity, TKey> Repository;
    protected readonly ITransactionManager TransactionManager;
    protected readonly object DbContext; // 解耦ORM，不绑定EF
    protected readonly IServiceProvider ServiceProvider; // 添加 ServiceProvider 属性

    protected BaseBusinessHandler(
        IUnifiedGradedValidator validator,
        IEntityBuilder<TInput, TEntity> entityBuilder,
        IRepository<TEntity, TKey> repository,
        ITransactionManager transactionManager,
        object dbContext,
        IServiceProvider serviceProvider)
    {
        Validator = validator;
        EntityBuilder = entityBuilder;
        Repository = repository;
        TransactionManager = transactionManager;
        DbContext = dbContext;
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// 全流程执行模板（核心入口）
    /// </summary>
    public async Task<ApiResult<TOutput>> ExecuteAsync(
        TInput input,
        UserInfo? user = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 构建业务上下文
            var businessContext = new BusinessUnit
            {
                Input = input,
                User = user,
                DbContext = DbContext,
                ServiceProvider = this.ServiceProvider // 假设 BaseBusinessHandler 中有注入 IServiceProvider
            };

            // 2. 分级校验（属性级+业务规则级）
            var validateResult = await Validator.ValidateAsync(input, businessContext, NonMandatoryHandler);
            if (!validateResult.IsValid)
            {
                var errors = validateResult.MandatoryFailures?.Values.ToList() 
                             ?? validateResult.NonMandatoryFailures?.Values.ToList() 
                             ?? new List<string>();
                return ApiResult<TOutput>.Fail("参数校验失败", errors);
            }

            // 3. 自定义业务逻辑校验（子类实现）
            var businessCheckResult = await ValidateBusinessLogicAsync(input, businessContext);
            if (!businessCheckResult.Success)
                return ApiResult<TOutput>.Fail(businessCheckResult.Message, businessCheckResult.Errors);

            // 4. 实体生成（DTO转领域实体）
            var entity = EntityBuilder.Build(input, businessContext);
            await PostProcessEntityAsync(entity, input, businessContext);

            // 5. 事务内执行数据操作
            var output = await TransactionManager.ExecuteInTransactionAsync(async () =>
            {
                var result = await ExecuteDataOperationAsync(entity, input, businessContext, cancellationToken);
                await Repository.SaveChangesAsync(cancellationToken);
                return result;
            }, cancellationToken);

            return ApiResult<TOutput>.Ok(output);
        }
        catch (Exception ex)
        {
            return ApiResult<TOutput>.Fail($"业务执行失败：{ex.Message}", new List<string> { ex.ToString() });
        }
    }

    #region 可扩展点（子类重写）
    /// <summary>
    /// 非强制校验失败处理策略（默认继续）
    /// </summary>
    protected virtual Task<NonMandatoryChoice> NonMandatoryHandler(Dictionary<string, string> failures)
        => Task.FromResult(NonMandatoryChoice.ConfirmContinue);

    /// <summary>
    /// 自定义业务逻辑校验（子类必须实现）
    /// </summary>
    protected abstract Task<ApiResult<bool>> ValidateBusinessLogicAsync(TInput input, BusinessUnit context);

    /// <summary>
    /// 实体生成后后置处理（可选重写）
    /// </summary>
    protected virtual Task PostProcessEntityAsync(TEntity entity, TInput input, BusinessUnit context)
        => Task.CompletedTask;

    /// <summary>
    /// 数据操作逻辑（子类必须实现）
    /// </summary>
    protected abstract Task<TOutput> ExecuteDataOperationAsync(
        TEntity entity,
        TInput input,
        BusinessUnit context,
        CancellationToken cancellationToken);
    #endregion
}