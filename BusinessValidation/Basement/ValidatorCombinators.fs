namespace BusinessValidation.Basement

module ValidatorCombinators =
    let all (validators: Validator<'T, 'Context> list) : Validator<'T, 'Context> =
        fun target context ->
            validators
            |> List.fold (fun acc v -> acc.Combine (v target context)) (ValidationResult<'T>.Success target)

    let when' (condition: 'T -> 'Context -> bool) (validator: Validator<'T, 'Context>) : Validator<'T, 'Context> =
        fun target context ->
            if condition target context then validator target context
            else ValidationResult<'T>.Success target

