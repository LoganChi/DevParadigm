using System;

namespace DevParadigm.Common.Attribute;

/// <summary>
/// 标记属性必须是唯一的
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class UniqueAttribute : System.Attribute
{
    public string ErrorMessage { get; set; } = "该值已存在";

    public UniqueAttribute()
    {
    }
}
