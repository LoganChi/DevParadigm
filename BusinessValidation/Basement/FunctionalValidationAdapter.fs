namespace BusinessValidation.Basement

open DevParadigm.Basement.Units
open DevParadigm.Enum
open DevParadigm.Interface

// C#接口适配层
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
        member this.Validate(input, context) =
            let fsContext = context :?> 'Context
            validator input fsContext |> fun r -> r.IsValid
        member this.GetErrors(input, context) =
            let fsContext = context :?> 'Context
            validator input fsContext |> fun r -> r.Errors :> seq<string>

