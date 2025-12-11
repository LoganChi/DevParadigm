using DevParadigm.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevParadigm.Attribute
{
    /// <summary>
    /// 分级校验特性基类（复用.NET原生校验体系）
    /// </summary>
    public abstract class GradedValidationAttribute : ValidationAttribute
    {
        /// <summary>
        /// 校验级别
        /// </summary>
        public ValidationLevel Level { get; set; } 
            = ValidationLevel.Mandatory;
        /// <summary>
        /// 非必须校验提示
        /// </summary>
        public string NonMandatoryTip { get; set; } 
            = string.Empty;
        /// <summary>
        /// 非必须选择
        /// </summary>
        protected abstract bool IsValidValue(object value);
        /// <summary>
        /// 校验
        /// </summary>
        public override bool IsValid(object value) 
            => IsValidValue(value);
    }
}
