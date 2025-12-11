namespace DevParadigm.Common.Enum
{
    /// <summary>
    /// 校验分级（全局统一）
    /// </summary>
    public enum ValidationLevel
    {
        /// <summary>
        /// 强制校验：失败直接终止
        /// </summary>
        Mandatory = 1,
        /// <summary>
        /// 非强制校验：失败可选择继续
        /// </summary>
        NonMandatory = 2
    }
}
