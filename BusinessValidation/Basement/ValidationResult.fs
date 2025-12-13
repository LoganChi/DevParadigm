namespace BusinessValidation.Basement

open DevParadigm.Common.Results

type FsValidationResult<'T> = {
    IsValid: bool
    Target: 'T
    Errors: BusinessError list
} with
    static member Success target = { IsValid = true; Target = target; Errors = [] }
    static member Failure target errors = { IsValid = false; Target = target; Errors = errors }
    member this.Combine other =
        if this.IsValid && other.IsValid then FsValidationResult<'T>.Success this.Target
        else { IsValid = false; Target = this.Target; Errors = this.Errors @ other.Errors }
