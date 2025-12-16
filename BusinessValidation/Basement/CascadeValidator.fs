namespace BusinessValidation.Basement

open DevParadigm.Basement.Units
open DevParadigm.Common.Results
open DevParadigm.Common.Attribute
open System.Reflection
open System.Threading.Tasks

module CascadeValidator =
    
    /// 验证级联删除约束
    /// 假设 Context 中包含关联数据的计数，Key 为 "{PropertyName}Count"
    let ValidateCascadeDelete (target: 'T) (context: BusinessUnit) = task {
            let props = typeof<'T>.GetProperties()
            let errors = ResizeArray<BusinessError>()
            
            for prop in props do
                let attr = prop.GetCustomAttribute<CascadeAttribute>()
                if not (isNull attr) then
                    // 检查是否允许删除
                    if not attr.Delete then
                        // 获取关联数据计数
                        // 约定：Context中存储的Key为 "PropertyCount" (e.g., "OrdersCount")
                        let countKey = $"{prop.Name}Count"
                        let count = context.Get<int>(countKey, 0)
                        
                        if count > 0 then
                            let error = ValidatorHelpers.createError 
                                            "Cascade.DeleteNotAllowed" 
                                            $"无法删除：存在 {count} 个关联的 {prop.Name}" 
                                            prop.Name
                            errors.Add(error)

            if errors.Count = 0 then
                return FsValidationResult<'T>.Success target
            else
                return FsValidationResult<'T>.Failure target (List.ofSeq errors)
        }
