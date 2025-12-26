namespace MainUI.UniversalPlatform.Core.Domain.Workflows
{
    /// <summary>
    /// 步骤状态枚举
    /// </summary>
    public enum StepStatus
    {
        /// <summary>等待执行</summary>
        Pending = 0,

        /// <summary>正在执行</summary>
        Running = 1,

        /// <summary>执行成功</summary>
        Succeeded = 2,

        /// <summary>执行失败</summary>
        Failed = 3,

        /// <summary>已跳过</summary>
        Skipped = 4
    }
}
