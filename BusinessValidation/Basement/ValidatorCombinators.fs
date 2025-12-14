namespace BusinessValidation.Basement

open System.Threading.Tasks
open DevParadigm.Common.Results

module ValidatorCombinators =
    
    /// 运行所有校验器并收集所有错误 (并行)
    let all (validators: Validator<'T, 'Context> list) : Validator<'T, 'Context> =
        fun target context -> task {
            let! results = Task.WhenAll(validators |> List.map (fun v -> v target context))
            let initialState = FsValidationResult<'T>.Success target
            return results |> Array.fold (fun (acc: FsValidationResult<'T>) r -> acc.Combine r) initialState
        }

    /// 顺序运行校验器，遇到第一个失败即停止（短路模式）
    let failFast (validators: Validator<'T, 'Context> list) : Validator<'T, 'Context> =
        fun target context -> task {
            let rec loop remaining = task {
                match remaining with
                | [] -> return FsValidationResult<'T>.Success target
                | v :: vs ->
                    let! result = v target context
                    if result.IsValid then return! loop vs
                    else return result
            }
            return! loop validators
        }

    /// 仅当条件满足时执行校验
    let when' (condition: 'T -> 'Context -> bool) (validator: Validator<'T, 'Context>) : Validator<'T, 'Context> =
        fun target context -> task {
            if condition target context then return! validator target context
            else return FsValidationResult<'T>.Success target
        }

    /// 条件分支校验 (if-else)
    let iff (condition: 'T -> 'Context -> bool) (ifValidator: Validator<'T, 'Context>) (elseValidator: Validator<'T, 'Context>) : Validator<'T, 'Context> =
        fun target context -> task {
            if condition target context then return! ifValidator target context
            else return! elseValidator target context
        }

    /// 基础断言校验器
    let must (predicate: 'T -> bool) (error: BusinessError) : Validator<'T, 'Context> =
        fun target _ -> Task.FromResult(
            if predicate target then FsValidationResult<'T>.Success target
            else FsValidationResult<'T>.Failure target [error]
        )

    /// 针对属性的校验适配器
    let prop (selector: 'T -> 'Prop) (validator: Validator<'Prop, 'Context>) : Validator<'T, 'Context> =
        fun target context -> task {
            let propValue = selector target
            let! result = validator propValue context
            if result.IsValid then return FsValidationResult<'T>.Success target
            else return FsValidationResult<'T>.Failure target result.Errors
        }

    /// 针对集合元素的校验适配器
    let forEach (selector: 'T -> 'Item seq) (validator: Validator<'Item, 'Context>) : Validator<'T, 'Context> =
        fun target context -> task {
            let items = selector target
            let! itemResults = Task.WhenAll(items |> Seq.map (fun item -> validator item context))
            
            let errors =
                itemResults
                |> Seq.collect (fun res -> res.Errors)
                |> List.ofSeq
            
            if List.isEmpty errors then return FsValidationResult<'T>.Success target
            else return FsValidationResult<'T>.Failure target errors
        }

    /// 针对集合元素的校验适配器（带索引信息）
    let forEachIndexed (selector: 'T -> 'Item seq) (validator: Validator<'Item, 'Context>) : Validator<'T, 'Context> =
        fun target context -> task {
            let items = selector target |> Seq.indexed
            let! itemResults = Task.WhenAll(
                items |> Seq.map (fun (i, item) -> task {
                    let! res = validator item context
                    return (i, res)
                })
            )

            let errors =
                itemResults
                |> Seq.map (fun (i, result) ->
                    if result.IsValid then []
                    else 
                        result.Errors 
                        |> List.map (fun e -> BusinessError(e.Code, $"[{i}] {e.Message}"))
                )
                |> Seq.concat
                |> List.ofSeq
            
            if List.isEmpty errors then return FsValidationResult<'T>.Success target
            else return FsValidationResult<'T>.Failure target errors
        }

    /// 上下文依赖断言（支持动态错误消息）
    let verify (predicate: 'T -> 'Context -> bool) (errorFactory: 'T -> 'Context -> BusinessError) : Validator<'T, 'Context> =
        fun target context -> Task.FromResult(
            if predicate target context then FsValidationResult<'T>.Success target
            else FsValidationResult<'T>.Failure target [errorFactory target context]
        )

    /// 提取上下文值并进行校验，避免重复提取
    let verifyVal (selector: 'T -> 'Context -> 'Val) (predicate: 'Val -> 'T -> bool) (errorFactory: 'Val -> 'T -> BusinessError) : Validator<'T, 'Context> =
        fun target context -> Task.FromResult(
            let value = selector target context
            if predicate value target then FsValidationResult<'T>.Success target
            else FsValidationResult<'T>.Failure target [errorFactory value target]
        )

    type ValidationBuilder() =
        member _.Yield(v: Validator<'T, 'Context>) = [v]
        member _.YieldFrom(vs: Validator<'T, 'Context> list) = vs
        member _.Combine(a, b) = a @ b
        member _.Delay(f) = f()
        member _.Zero() = []
        member _.Run(validators) = all validators

    let validation = ValidationBuilder()
    let asyncValidation = validation

    /// 管道操作符
    module Operators =
        /// 组合校验（收集所有错误）: v1 <&> v2
        let ( <&> ) v1 v2 = all [v1; v2]
        
        /// 顺序校验（短路模式）: v1 >=> v2
        let ( >=> ) v1 v2 = failFast [v1; v2]
