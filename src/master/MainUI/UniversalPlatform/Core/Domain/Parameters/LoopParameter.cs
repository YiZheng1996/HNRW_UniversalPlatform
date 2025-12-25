using MainUI.UniversalPlatform.Core.Domain.Workflows;
using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 循环参数
    /// </summary>
    [Serializable]
    public class LoopParameter
    {
        /// <summary>
        /// 循环次数表达式（可以是数字或变量，如：10 或 {MaxRetryCount}）
        /// </summary>
        [JsonProperty("LoopCountExpression")]
        public string LoopCountExpression { get; set; } = "10";

        /// <summary>
        /// 循环次数（直接数值）
        /// </summary>
        [JsonProperty("LoopCount")]
        public int LoopCount { get; set; } = 10;

        /// <summary>
        /// 循环计数器变量名
        /// </summary>
        [JsonProperty("CounterVariableName")]
        public string CounterVariableName { get; set; } = "LoopIndex";

        /// <summary>
        /// 是否启用计数器变量
        /// </summary>
        [JsonProperty("EnableCounter")]
        public bool EnableCounter { get; set; } = true;

        /// <summary>
        /// 循环体子步骤列表
        /// </summary>
        [JsonProperty("ChildSteps")]
        public List<WorkflowStep> ChildSteps { get; set; } = new();

        /// <summary>
        /// 是否启用提前退出
        /// </summary>
        [JsonProperty("EnableEarlyExit")]
        public bool EnableEarlyExit { get; set; } = false;

        /// <summary>
        /// 提前退出条件表达式
        /// </summary>
        [JsonProperty("ExitConditionExpression")]
        public string ExitCondition { get; set; } = "";

        /// <summary>
        /// 退出条件说明
        /// </summary>
        [JsonProperty("ExitConditionDescription")]
        public string ExitConditionDescription { get; set; } = "";

        /// <summary>
        /// 描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; } = "";
    }
}
