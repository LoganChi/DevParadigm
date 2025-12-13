namespace BusinessValidation.Basement

type Validator<'T, 'Context> = 'T -> 'Context -> FsValidationResult<'T>
