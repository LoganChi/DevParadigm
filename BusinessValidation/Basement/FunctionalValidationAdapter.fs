namespace BusinessValidation.Basement

open DevParadigm.Basement.Units
open DevParadigm.Enum
open DevParadigm.Interface

// C#接口适配层
type FunctionalValidationAdapter<'T>(
    validator: Validator<'T, BusinessUnit>,
    context: BusinessUnit,
    level: ValidationLevel) =
    
    interface IBusinessValidationRule<'T> with
        member this.RuleId = "FSharp_FunctionalValidation"
        member this.Level = level
        member this.FailureMessage = "F#函数式校验失败"
        member this.NonMandatoryTip = "业务规则未通过，是否继续？"
        member this.Validate(input, context) =
            validator input context |> fun r -> r.IsValid
        member this.GetErrors(input, context) =
            validator input context |> fun r -> r.Errors :> seq<string>