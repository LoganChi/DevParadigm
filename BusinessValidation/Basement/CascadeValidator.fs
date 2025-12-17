namespace BusinessValidation.Basement

open DevParadigm.Basement.Units
open DevParadigm.Common.Results
open DevParadigm.Common.Attribute
open System
open System.Reflection
open System.Threading.Tasks
open System.Collections.Concurrent

module CascadeValidator =
    
    // 缓存反射结果：Type -> (PropertyInfo * CascadeAttribute)[]
    let private cache = ConcurrentDictionary<Type, (PropertyInfo * CascadeAttribute)[]>()

    let private getCascadeProperties (t: Type) =
        cache.GetOrAdd(t, fun type' ->
            type'.GetProperties()
            |> Array.choose (fun prop ->
                let attr = prop.GetCustomAttribute<CascadeAttribute>()
                if isNull attr then None
                else Some (prop, attr)
            )
        )

    /// 验证级联删除约束
    /// 假设 Context 中包含关联数据的计数，Key 为 "{PropertyName}Count"
    /// 针对单个实体的属性循环，使用串行处理即可
    let ValidateCascadeDelete (target: 'T) (context: BusinessUnit) = task {
            let propsWithAttrs = getCascadeProperties typeof<'T>
            let errors = ResizeArray<BusinessError>()

            for (prop, attr) in propsWithAttrs do
                // 检查是否允许删除
                if not attr.Delete then
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
