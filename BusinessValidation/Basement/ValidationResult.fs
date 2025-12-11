namespace BusinessValidation.Basement

// 不可变校验结果
type ValidationResult<'T> = {
    IsValid: bool
    Target: 'T
    Errors: string list
} with
    static member Success target = { IsValid = true; Target = target; Errors = [] }
    static member Failure target errors = { IsValid = false; Target = target; Errors = errors }
    member this.Combine other =
        if this.IsValid && other.IsValid then ValidationResult<'T>.Success this.Target
        else { IsValid = false; Target = this.Target; Errors = this.Errors @ other.Errors }