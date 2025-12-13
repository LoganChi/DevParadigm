namespace BusinessValidation.Basement

open DevParadigm.Common.Results

module ValidatorCombinators =
    /// 运行所有校验器并收集所有错误
    let all (validators: Validator<'T, 'Context> list) : Validator<'T, 'Context> =
        fun target context ->
            validators
            |> List.fold (fun acc v -> acc.Combine (v target context)) (FsValidationResult<'T>.Success target)

    /// 顺序运行校验器，遇到第一个失败即停止（短路模式）
    let failFast (validators: Validator<'T, 'Context> list) : Validator<'T, 'Context> =
        fun target context ->
            let rec loop remaining =
                match remaining with
                | [] -> FsValidationResult<'T>.Success target
                | v :: vs ->
                    let result = v target context
                    if result.IsValid then loop vs
                    else result
            loop validators

    /// 仅当条件满足时执行校验
    let when' (condition: 'T -> 'Context -> bool) (validator: Validator<'T, 'Context>) : Validator<'T, 'Context> =
        fun target context ->
            if condition target context then validator target context
            else FsValidationResult<'T>.Success target

    /// 条件分支校验 (if-else)
    let iff (condition: 'T -> 'Context -> bool) (ifValidator: Validator<'T, 'Context>) (elseValidator: Validator<'T, 'Context>) : Validator<'T, 'Context> =
        fun target context ->
            if condition target context then ifValidator target context
            else elseValidator target context

    /// 基础断言校验器
    let must (predicate: 'T -> bool) (error: BusinessError) : Validator<'T, 'Context> =
        fun target _ ->
            if predicate target then FsValidationResult<'T>.Success target
            else FsValidationResult<'T>.Failure target [error]

    /// 针对属性的校验适配器
    let prop (selector: 'T -> 'Prop) (validator: Validator<'Prop, 'Context>) : Validator<'T, 'Context> =
        fun target context ->
            let propValue = selector target
            let result = validator propValue context
            if result.IsValid then FsValidationResult<'T>.Success target
            else FsValidationResult<'T>.Failure target result.Errors

    /// 针对集合元素的校验适配器
    let forEach (selector: 'T -> 'Item seq) (validator: Validator<'Item, 'Context>) : Validator<'T, 'Context> =
        fun target context ->
            let items = selector target
            let errors =
                items
                |> Seq.collect (fun item -> (validator item context).Errors)
                |> List.ofSeq
            
            if List.isEmpty errors then FsValidationResult<'T>.Success target
            else FsValidationResult<'T>.Failure target errors

    /// 针对集合元素的校验适配器（带索引信息）
    let forEachIndexed (selector: 'T -> 'Item seq) (validator: Validator<'Item, 'Context>) : Validator<'T, 'Context> =
        fun target context ->
            let items = selector target
            let errors =
                items
                |> Seq.mapi (fun i item ->
                    let result = validator item context
                    if result.IsValid then []
                    else 
                        result.Errors 
                        |> List.map (fun e -> BusinessError(e.Code, $"[{i}] {e.Message}"))
                )
                |> Seq.concat
                |> List.ofSeq
            
            if List.isEmpty errors then FsValidationResult<'T>.Success target
            else FsValidationResult<'T>.Failure target errors

    /// 上下文依赖断言（支持动态错误消息）
    let verify (predicate: 'T -> 'Context -> bool) (errorFactory: 'T -> 'Context -> BusinessError) : Validator<'T, 'Context> =
        fun target context ->
            if predicate target context then FsValidationResult<'T>.Success target
            else FsValidationResult<'T>.Failure target [errorFactory target context]

    /// 管道操作符
    module Operators =
        /// 组合校验（收集所有错误）: v1 <&> v2
        let ( <&> ) v1 v2 = all [v1; v2]
        
        /// 顺序校验（短路模式）: v1 >=> v2
        let ( >=> ) v1 v2 = failFast [v1; v2]

