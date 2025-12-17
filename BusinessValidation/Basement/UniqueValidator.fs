namespace BusinessValidation.Basement

open DevParadigm.Basement.Units
open DevParadigm.Common.Results
open DevParadigm.Common.Attribute
open System
open System.Reflection
open System.Threading.Tasks
open System.Collections.Concurrent
open System.Collections.Generic

module UniqueValidator =

    // 缓存反射结果：Type -> (PropertyInfo * UniqueAttribute)[]
    let private cache = ConcurrentDictionary<Type, (PropertyInfo * UniqueAttribute)[]>()

    let private getUniqueProperties (t: Type) =
        cache.GetOrAdd(t, fun type' ->
            type'.GetProperties()
            |> Array.choose (fun prop ->
                let attr = prop.GetCustomAttribute<UniqueAttribute>()
                if isNull attr then None
                else Some (prop, attr)
            )
        )

    /// 验证唯一性约束 (针对单个实体)
    /// 假设 Context 中包含唯一性检查结果 (通常来自 DB 检查)，Key 为 "IsUnique_{PropertyName}"
    /// 针对单个实体的属性循环，使用串行处理即可，无需并行开销
    let ValidateUnique (target: 'T) (context: BusinessUnit) = task {
            let propsWithAttrs = getUniqueProperties typeof<'T>
            let errors = ResizeArray<BusinessError>()
            
            for (prop, attr) in propsWithAttrs do
                let key = $"IsUnique_{prop.Name}"
                // 默认值为 true，C# Rule 负责注入 false 来触发错误
                let isUnique = context.Get<bool>(key, true)

                if not isUnique then
                    let msg = if isNull attr.ErrorMessage then "Value must be unique" else attr.ErrorMessage
                    let error = ValidatorHelpers.createError 
                                    "Unique.ConstraintViolated" 
                                    msg
                                    prop.Name
                    errors.Add(error)
            
            if errors.Count = 0 then
                return FsValidationResult<'T>.Success target
            else
                return FsValidationResult<'T>.Failure target (List.ofSeq errors)
        }

    /// 验证集合内部唯一性 (针对列表整体)
    /// 检查输入的列表中是否存在重复的属性值 (例如批量提交时，不能包含重复的 Code)
    /// 这是一个针对列表整体的处理逻辑
    let ValidateCollectionUnique (items: 'T seq) (context: BusinessUnit) = task {
        let propsWithAttrs = getUniqueProperties typeof<'T>
        
        // 转换为数组以避免多次枚举
        let itemsArray = items |> Seq.toArray

        // 如果列表为空或只有1项，无需检查内部重复
        if itemsArray.Length <= 1 then
            return FsValidationResult<'T seq>.Success items
        else
            let errors = ResizeArray<BusinessError>()
            
            // 对每个标记为 Unique 的属性，检查集合内是否有重复值
            // 这里可以是并行的候选点，但通常 GroupBy 在内存中非常快，除非列表极大
            // 如果确实需要处理超大列表 (e.g. 10w+)，可以考虑 PLINQ
            for (prop, attr) in propsWithAttrs do
                let duplicates = 
                    itemsArray
                    |> Seq.groupBy (fun item -> prop.GetValue(item))
                    |> Seq.filter (fun (key, group) -> 
                        // 忽略 null 值的重复（除非业务明确禁止 null，通常由 Required 控制）
                        // 且组内元素数量 > 1 表示有重复
                        key <> null && Seq.length group > 1)
                    |> Seq.map (fun (key, _) -> key)
                    |> Seq.toList

                if not (List.isEmpty duplicates) then
                    let msg = if isNull attr.ErrorMessage then "Duplicate values found in list" else attr.ErrorMessage
                    let duplicateValues = String.Join(", ", duplicates)
                    let error = ValidatorHelpers.createError 
                                    "Unique.CollectionDuplicate" 
                                    $"{msg} (Values: {duplicateValues})" 
                                    prop.Name
                    errors.Add(error)

            if errors.Count = 0 then
                return FsValidationResult<'T seq>.Success items
            else
                return FsValidationResult<'T seq>.Failure items (List.ofSeq errors)
    }
