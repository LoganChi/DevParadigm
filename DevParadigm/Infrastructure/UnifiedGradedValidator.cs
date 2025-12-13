using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DevParadigm.Basement.Units;
using DevParadigm.Common.Attribute;
using DevParadigm.Common.Enum;
using DevParadigm.Common.Results;
using DevParadigm.Interface;

namespace DevParadigm.Infrastructure;

/// <summary>
/// 统一分级校验器（属性级+业务级）
/// </summary>
public class UnifiedGradedValidator : IUnifiedGradedValidator
{
    private readonly IServiceProvider _serviceProvider;

    public UnifiedGradedValidator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 执行分级校验
    /// </summary>
    public async Task<ValidationResult<TInput>> ValidateAsync<TInput>(
        TInput input,
        BusinessUnit unit,
        Func<Dictionary<string, string>, Task<NonMandatoryChoice>>? nonMandatoryHandler = null)
        where TInput : class
    {
        if (input == null)
        {
            return ValidationResult<TInput>.MandatoryFailed(input, new Dictionary<string, string>
            {
                { "InputNull", "输入参数不能为空" }
            });
        }
                                                                  
        // 1. 属性级校验（仅针对输入本身的字段）
        var (mandatoryAttrErrors, nonMandatoryAttrErrors) = ValidateAttributes(input);

        // 合并强制校验错误
        var allMandatoryErrors = new Dictionary<string, string>();
        foreach (var error in mandatoryAttrErrors)
        {
            allMandatoryErrors[error.Key] = error.Value;
        }

        // 合并非强制校验错误
        var allNonMandatoryErrors = new Dictionary<string, string>();
        foreach (var error in nonMandatoryAttrErrors)
        {
            allNonMandatoryErrors[error.Key] = error.Value;
        }

        // 3. 处理强制校验失败
        if (allMandatoryErrors.Any())
        {
            return ValidationResult<TInput>.MandatoryFailed(input, allMandatoryErrors);
        }

        // 4. 处理非强制校验失败
        if (allNonMandatoryErrors.Any() && nonMandatoryHandler != null)
        {
            var userChoice = await nonMandatoryHandler(allNonMandatoryErrors);
            if (userChoice == NonMandatoryChoice.TerminateProcess)
            {
                return ValidationResult<TInput>.NonMandatoryFailed(input, allNonMandatoryErrors);
            }
        }

        // 校验通过
        return ValidationResult<TInput>.Success(input);
    }

    #region 私有方法
    /// <summary>
    /// 属性级校验（分级）
    /// </summary>
    private (Dictionary<string, string> MandatoryErrors, Dictionary<string, string> NonMandatoryErrors) ValidateAttributes<TInput>(TInput input)
    {
        var mandatoryErrors = new Dictionary<string, string>();
        var nonMandatoryErrors = new Dictionary<string, string>();

        var properties = typeof(TInput).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            var value = property.GetValue(input);
            var validationAttributes = property.GetCustomAttributes<ValidationAttribute>();

            foreach (var attr in validationAttributes)
            {
                var context = new ValidationContext(input) { MemberName = property.Name };
                var result = attr.GetValidationResult(value, context);

                if (result != ValidationResult.Success)
                {
                    // 判断是否为分级校验特性
                    if (attr is GradedValidationAttribute gradedAttr)
                    {
                        if (gradedAttr.Level == ValidationLevel.Mandatory)
                        {
                            mandatoryErrors[property.Name] = result.ErrorMessage ?? $"{property.Name}校验失败";
                        }
                        else
                        {
                            nonMandatoryErrors[property.Name] = result.ErrorMessage ?? $"{property.Name}校验失败";
                        }
                    }
                    else
                    {
                        // 普通DataAnnotations默认强制校验
                        mandatoryErrors[property.Name] = result.ErrorMessage ?? $"{property.Name}校验失败";
                    }
                }
            }
        }

        return (mandatoryErrors, nonMandatoryErrors);
    }

    #endregion
}
