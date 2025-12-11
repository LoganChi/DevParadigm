using DevParadigm.Basement.Units;
using DevParadigm.Common.Results;

namespace DevParadigm.Interface;

/// <summary>
/// 业务处理器核心接口
/// </summary>
/// <typeparam name="TInput">输入DTO类型</typeparam>
/// <typeparam name="TOutput">输出结果类型</typeparam>
public interface IBusinessHandler<in TInput, TOutput>
    where TInput : class
{
    Task<ApiResult<TOutput>> ExecuteAsync(
        TInput input,
        UserInfo? user = null,
        CancellationToken cancellationToken = default);
}