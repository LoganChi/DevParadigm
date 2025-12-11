namespace BusinessValidation.Basement

// 函数式校验器类型别名
type Validator<'T, 'Context> = 'T -> 'Context -> ValidationResult<'T>