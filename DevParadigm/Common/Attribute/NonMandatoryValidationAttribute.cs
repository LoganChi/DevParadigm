using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace DevParadigm.Common.Attribute;

/// <summary>
/// 自定义非强制校验特性（实现分级校验）
/// </summary>
public class NonMandatoryValidationAttribute : GradedValidationAttribute
{
    protected override bool IsValidValue(object value)
    {
        // 基础校验逻辑（可根据需要自定义）
        // 示例：如果是字符串，校验长度；如果是数值，校验范围
        if (value == null)
        {
            // 空值是否有效取决于是否标记为Required（这里仅处理非空场景）
            return true;
        }

        // 字符串长度校验（示例）
        if (value is string strValue)
        {
            return strValue.Length <= (MaximumLength == int.MaxValue ? int.MaxValue : MaximumLength);
        }

        // 数值范围校验（示例）
        if (value is int intValue)
        {
            return intValue >= (Minimum == int.MinValue ? int.MinValue : Minimum) && 
                   intValue <= (Maximum == int.MaxValue ? int.MaxValue : Maximum);
        }

        // 默认有效
        return true;
    }
    #region 可配置属性（示例）
    /// <summary>
    /// 最大长度（字符串）
    /// </summary>
    public int MaximumLength { get; set; } = int.MaxValue;

    /// <summary>
    /// 最小值（数值）
    /// </summary>
    public int Minimum { get; set; } = int.MinValue;

    /// <summary>
    /// 最大值（数值）
    /// </summary>
    public int Maximum { get; set; } = int.MaxValue;
    #endregion
}