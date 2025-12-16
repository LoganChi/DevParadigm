using System;

namespace DevParadigm.Common.Attribute;

/// <summary>
/// 标记实体间的级联关系
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CascadeAttribute : System.Attribute
{
    /// <summary>
    /// 关联的实体类型
    /// </summary>
    public Type RelatedType { get; }
    
    /// <summary>
    /// 是否级联删除
    /// </summary>
    public bool Delete { get; set; }
    
    /// <summary>
    /// 是否级联更新
    /// </summary>
    public bool Update { get; set; }
    
    /// <summary>
    /// 外键属性名
    /// </summary>
    public string ForeignKey { get; set; }

    public CascadeAttribute(Type relatedType)
    {
        RelatedType = relatedType;
    }
}
