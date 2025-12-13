namespace DevParadigm.Common.Results;

public sealed class BusinessError
{
    public string Code { get; }
    public string Message { get; }
    public string? Field { get; }

    public BusinessError(string code, string message, string? field = null)
    {
        Code = code;
        Message = message;
        Field = field;
    }
}

