namespace BusinessValidation.Basement

open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent
open System.Linq
open DevParadigm.Common.Results

module ValidatorCombinators =
    
    /// 运行所有校验器并收集所有错误 
    /// (并行优化：使用 Parallel.ForEachAsync 限制并发度，避免 Task.WhenAll 导致的线程池/CPU爆炸)
    let all (validators: Validator<'T, 'Context> list) : Validator<'T, 'Context> =
        fun target context -> task {
            // 使用 ConcurrentBag 线程安全地收集错误，同时记录索引以保证最终顺序
            let errorsBag = ConcurrentBag<int * BusinessError list>()
            let indexedValidators = validators |> List.indexed
            
            // 自动根据 CPU 核心数进行并行节流 (Throttling)
            do! Parallel.ForEachAsync(indexedValidators, Func<_,_,_>(fun (index, validator) _ ->
                ValueTask(task {
                    let! result = validator target context
                    if not result.IsValid then
                        errorsBag.Add(index, result.Errors)
                })
            ))

            if errorsBag.IsEmpty then
                return FsValidationResult<'T>.Success target
            else
                // 恢复错误顺序 (Deterministic Order)
                let allErrors = 
                    errorsBag 
                    |> Seq.sortBy fst 
                    |> Seq.collect snd 
                    |> List.ofSeq
                return FsValidationResult<'T>.Failure target allErrors
        }

    /// 顺序运行校验器，遇到第一个失败即停止（短路模式）
    /// 注意：failFast 本质是顺序依赖的，不适合并行
    let failFast (validators: Validator<'T, 'Context> list) : Validator<'T, 'Context> =
        fun target context -> task {
            let mutable finalResult = FsValidationResult<'T>.Success target
            let mutable isDone = false
            
            for v in validators do
                if not isDone then
                    let! result = v target context
                    if not result.IsValid then
                        finalResult <- result
                        isDone <- true
            
            return finalResult
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

    /// 针对集合元素的校验适配器 (并行优化：Parallel.ForEachAsync)
    let forEach (selector: 'T -> 'Item seq) (validator: Validator<'Item, 'Context>) : Validator<'T, 'Context> =
        fun target context -> task {
            let items = selector target |> Seq.indexed
            let errorsBag = ConcurrentBag<int * BusinessError list>()
            
            // 针对大数据量集合，Parallel.ForEachAsync 优势巨大
            do! Parallel.ForEachAsync(items, Func<_,_,_>(fun (index, item) _ ->
                ValueTask(task {
                    let! result = validator item context
                    if not result.IsValid then
                        errorsBag.Add(index, result.Errors)
                })
            ))
            
            if errorsBag.IsEmpty then
                return FsValidationResult<'T>.Success target
            else
                let errors =
                    errorsBag
                    |> Seq.sortBy fst // 保证错误顺序与列表顺序一致
                    |> Seq.collect snd
                    |> List.ofSeq
                return FsValidationResult<'T>.Failure target errors
        }

    /// 针对集合元素的校验适配器（带索引信息，并行优化）
    let forEachIndexed (selector: 'T -> 'Item seq) (validator: Validator<'Item, 'Context>) : Validator<'T, 'Context> =
        fun target context -> task {
            let items = selector target |> Seq.indexed
            let errorsBag = ConcurrentBag<int * BusinessError list>()
            
            do! Parallel.ForEachAsync(items, Func<_,_,_>(fun (index, item) _ ->
                ValueTask(task {
                    let! result = validator item context
                    if not result.IsValid then
                        // 在并行任务内部处理错误消息格式化，分摊 CPU 开销
                        let indexedErrors = 
                            result.Errors 
                            |> List.map (fun e -> BusinessError(e.Code, $"[{index}] {e.Message}"))
                        errorsBag.Add(index, indexedErrors)
                })
            ))

            if errorsBag.IsEmpty then
                return FsValidationResult<'T>.Success target
            else
                let errors =
                    errorsBag
                    |> Seq.sortBy fst
                    |> Seq.collect snd
                    |> List.ofSeq
                return FsValidationResult<'T>.Failure target errors
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

    /// 校验值必须在指定的集合中
    let isInList (allowedValues: 'T seq) (error: BusinessError) : Validator<'T, 'Context> =
        must (fun v -> Seq.contains v allowedValues) error

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
