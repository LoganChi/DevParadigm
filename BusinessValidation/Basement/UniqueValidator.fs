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
    /// 同时检查是否与数据库中已有的数据重复 (Key: ExistingValues_{PropertyName})
    let ValidateCollectionUnique (items: 'T seq) (context: BusinessUnit) = task {
        let propsWithAttrs = getUniqueProperties typeof<'T>
        
        // 转换为数组以避免多次枚举
        let itemsArray = items |> Seq.toArray

        // 如果列表为空，直接返回成功
        if itemsArray.Length = 0 then
            return FsValidationResult<'T seq>.Success items
        else
            let errors = ResizeArray<BusinessError>()
            
            for (prop, attr) in propsWithAttrs do
                // 1. 检查集合内部重复 (Internal Duplicates)
                if itemsArray.Length > 1 then
                    let duplicates = 
                        itemsArray
                        |> Seq.groupBy (fun item -> prop.GetValue(item))
                        |> Seq.filter (fun (key, group) -> 
                            // 忽略 null 值的重复
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

                // 2. 检查与已有数据重复 (External Duplicates)
                // 约定 Context Key 为 "ExistingValues_{PropertyName}"，值为 IEnumerable
                let existingKey = $"ExistingValues_{prop.Name}"
                if context.Extensions.ContainsKey(existingKey) then
                    let existingObj = context.Extensions.[existingKey]
                    match existingObj with
                    | :? System.Collections.IEnumerable as collection ->
                        // 强制转换为 String 进行比较，避免装箱/拆箱带来的类型不一致问题
                        // 尤其是当 Context 中的数据类型可能与实体属性类型存在细微差异时
                        let existingSet = HashSet<string>()
                        for item in collection |> Seq.cast<obj> do
                            if item <> null then existingSet.Add(item.ToString()) |> ignore
                        
                        let externalDuplicates =
                            itemsArray
                            |> Seq.map (fun item -> prop.GetValue(item))
                            |> Seq.filter (fun v -> v <> null && existingSet.Contains(v.ToString()))
                            |> Seq.map (fun v -> v.ToString())
                            |> Seq.distinct
                            |> Seq.toList
                        
                        if not (List.isEmpty externalDuplicates) then
                            let msg = if isNull attr.ErrorMessage then "Values already exist" else attr.ErrorMessage
                            let duplicateValues = String.Join(", ", externalDuplicates)
                            let error = ValidatorHelpers.createError 
                                            "Unique.ConstraintViolated" 
                                            $"{msg} (Existing Values: {duplicateValues})" 
                                            prop.Name
                            errors.Add(error)
                    | _ -> ()

            if errors.Count = 0 then
                return FsValidationResult<'T seq>.Success items
            else
                return FsValidationResult<'T seq>.Failure items (List.ofSeq errors)
    }
