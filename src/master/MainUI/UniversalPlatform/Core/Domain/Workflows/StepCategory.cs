namespace MainUI.UniversalPlatform.Core.Domain.Workflows
{
    /// <summary>
    /// 步骤类别枚举
    /// </summary>
    public enum StepCategory
    {
        /// <summary>逻辑控制</summary>
        Logic,

        /// <summary>条件判断</summary>
        Condition,

        /// <summary>循环</summary>
        Loop,

        /// <summary>变量操作</summary>
        Variable,

        /// <summary>通信操作</summary>
        Communication,

        /// <summary>报表操作</summary>
        Report,

        /// <summary>监控</summary>
        Monitor,

        /// <summary>其他</summary>
        Other
    }
}
