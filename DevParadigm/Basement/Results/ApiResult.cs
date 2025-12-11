namespace DevParadigm.Basement.Results;

/// <summary>
/// 全局统一返回结果（不可变）
/// </summary>
/// <typeparam name="TData">数据类型</typeparam>
public record ApiResult<TData>(
    bool Success,
    TData? Data = default,
    string? Message = null,
    int Code = 0,
    List<string>? Errors = null)
{
    public static ApiResult<TData> Ok(TData data, string message = "操作成功") 
        => new(true, data, message);
    
    public static ApiResult<TData> Fail(string message, List<string>? errors = null, int code = 500) 
        => new(false, default, message, code, errors);
}