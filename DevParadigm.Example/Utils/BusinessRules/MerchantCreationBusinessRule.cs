using BusinessValidation.Basement;
using DevParadigm.Basement.Units;
using DevParadigm.Common.Attribute;
using DevParadigm.Common.Enum;
using DevParadigm.Common.Results;
using DevParadigm.Example.Entities;
using DevParadigm.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DevParadigm.Example.Utils.BusinessRules;

[Service(ServiceLifetime.Scoped)]
public class MerchantCreationBusinessRule : IBusinessValidationRule<Merchant>
{
    private readonly IReadOnlyRepository<Merchant, Guid> _merchantRepository;

    public MerchantCreationBusinessRule(IReadOnlyRepository<Merchant, Guid> merchantRepository)
    {
        _merchantRepository = merchantRepository;
    }

    public string RuleId => "MerchantCreation_UniqueCheck";
    public ValidationLevel Level => ValidationLevel.Mandatory;
    public string FailureMessage => "唯一性校验失败";
    public string NonMandatoryTip => "";

    public async Task<ApiResult<bool>> ValidateAsync(Merchant input, BusinessUnit context)
    {
        try
        {
            // 1. 手动查询唯一性 (Name)
            // 注意：实际项目中建议封装通用工具，避免手写每个字段的查询
            var nameExists = await _merchantRepository.Query(useReadDb: true)
                .AnyAsync(m => m.Name == input.Name);
            
            // 2. 注入上下文 (Key = IsUnique_{PropertyName})
            // 如果存在则不唯一
            context.Extensions["IsUnique_Name"] = !nameExists;

            // 3. 调用 F# 唯一性校验器
            var result = await UniqueValidator.ValidateUnique(input, context);

            if (!result.IsValid)
            {
                 var errors = result.Errors.Select(e => e.Message).ToList();
                 return ApiResult<bool>.Fail("唯一性校验失败", errors);
            }

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail($"校验异常: {ex.Message}");
        }
    }
}
