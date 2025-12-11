namespace DevParadigm.Basement.Results;

/// <summary>
/// 校验结果模型（属性级+业务级统一）
/// </summary>
/// <typeparam name="TInput">输入类型</typeparam>
/// <param name="IsValid">是否校验通过</param>
/// <param name="Input">输入值</param>
/// <param name="MandatoryFailures">必须校验失败项（键：校验项名称，值：校验失败信息）</param>
/// <param name="NonMandatoryFailures">非必须校验失败项（键：校验项名称，值：校验失败信息）</param>
public record ValidationResult<TInput>(
    bool IsValid,
    TInput Input,
    Dictionary<string, string>? MandatoryFailures = null,
    Dictionary<string, string>? NonMandatoryFailures = null)
{
    public static ValidationResult<TInput> Success(TInput input) 
        => new(true, input);
    
    public static ValidationResult<TInput> MandatoryFailed(TInput input, Dictionary<string, string> failures) 
        => new(false, input, failures);
    
    public static ValidationResult<TInput> NonMandatoryFailed(TInput input, Dictionary<string, string> failures) 
        => new(false, input, null, failures);
}