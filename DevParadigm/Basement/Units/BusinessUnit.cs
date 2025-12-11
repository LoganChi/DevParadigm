using System.ComponentModel.DataAnnotations;

namespace DevParadigm.Basement.Units;

/// <summary>
/// 业务单元
/// </summary>
public class BusinessUnit
{
    public object Input { get; init; }
    public ValidationResult? ValidationResult { get; init; }
    public UserInfo? User { get; init; }
    public object? DbContext { get; init; }
    public IServiceProvider? ServiceProvider { get; init; }
    public Dictionary<string, object> Extensions { get; init; } = new();
}