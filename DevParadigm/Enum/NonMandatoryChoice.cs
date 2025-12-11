using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevParadigm.Enum
{
    /// <summary>
    /// 非强制校验失败后的用户选择
    /// </summary>
    public enum NonMandatoryChoice
    {
        /// <summary>
        /// 终止进程
        /// </summary>
        TerminateProcess = 0,
        /// <summary>
        /// 确认继续
        /// </summary>
        ConfirmContinue = 1
    }
}
