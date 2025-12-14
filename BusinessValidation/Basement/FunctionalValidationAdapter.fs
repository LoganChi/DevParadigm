namespace BusinessValidation.Basement

open System.Threading.Tasks
open DevParadigm.Basement.Units
open DevParadigm.Common.Enum
open DevParadigm.Common.Results
open DevParadigm.Interface

type FunctionalValidationAdapter<'T, 'Context when 'Context :> BusinessUnit>(
    validator: Validator<'T, 'Context>,
    context: 'Context,
    level: ValidationLevel,
    ruleId: string,
    failureMessage: string,
    nonMandatoryTip: string) =
    
    interface IBusinessValidationRule<'T> with
        member this.RuleId = ruleId
        member this.Level = level
        member this.FailureMessage = failureMessage
        member this.NonMandatoryTip = nonMandatoryTip
        member this.ValidateAsync(input, context) = task {
            let fsContext = context :?> 'Context
            let! result = validator input fsContext
            if result.IsValid then
                return ApiResult<bool>.Ok(true)
            else
                let messages = result.Errors |> List.map (fun e -> e.Message) |> List.toArray
                return ApiResult<bool>.Fail(failureMessage, System.Collections.Generic.List<string>(messages))
        }

