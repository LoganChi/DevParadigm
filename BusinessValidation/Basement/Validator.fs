namespace BusinessValidation.Basement

open DevParadigm.Basement.Units
open DevParadigm.Common.Results

type Validator<'T, 'Context> = 'T -> 'Context -> FsValidationResult<'T>

// 创建一个专门的辅助模块来包含这些通用函数
module ValidatorHelpers =
    let tryGet<'T> (key: string) (unit: BusinessUnit) =
        match unit.Extensions.TryGetValue key with
        | true, value ->
            match value with
            | :? 'T as typed -> Some typed
            | _ -> None
        | _ -> None

    let getOrDefault<'T> (key: string) (defaultValue: 'T) (unit: BusinessUnit) : 'T =
        tryGet<'T> key unit |> Option.defaultValue defaultValue

    let createError (code: string) (message: string) (field: string) =
        BusinessError(code, message, field)

[<AutoOpen>]
module BusinessUnitExtensions =
    type BusinessUnit with
        member this.Get<'T>(key: string, ?defaultValue: 'T) =
            let def = defaultArg defaultValue Unchecked.defaultof<'T>
            ValidatorHelpers.getOrDefault<'T> key def this
