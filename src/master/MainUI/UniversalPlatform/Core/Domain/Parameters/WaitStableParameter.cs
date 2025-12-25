using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 等待稳定参数
    /// </summary>
    [Serializable]
    public class WaitStableParameter
    {
        /// <summary>
        /// 步骤描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; } = "等待稳定";

        /// <summary>
        /// 监测源类型
        /// </summary>
        [JsonProperty("MonitorSourceType")]
        public MonitorSourceType MonitorSourceType { get; set; } = MonitorSourceType.Variable;

        /// <summary>
        /// 监测的变量名（当MonitorSourceType为Variable时使用）
        /// </summary>
        [JsonProperty("MonitorVariable")]
        public string MonitorVariable { get; set; } = "";

        /// <summary>
        /// PLC模块名（当MonitorSourceType为PLC时使用）
        /// </summary>
        [JsonProperty("PlcModuleName")]
        public string PlcModuleName { get; set; } = "";

        /// <summary>
        /// PLC地址（当MonitorSourceType为PLC时使用）
        /// </summary>
        [JsonProperty("PlcAddress")]
        public string PlcAddress { get; set; } = "";

        /// <summary>
        /// 稳定判据：变化率阈值
        /// </summary>
        [JsonProperty("StabilityThreshold")]
        public double StabilityThreshold { get; set; } = 0.1;

        /// <summary>
        /// 采样间隔（秒）
        /// </summary>
        [JsonProperty("SamplingInterval")]
        public int SamplingInterval { get; set; } = 1;

        /// <summary>
        /// 连续稳定次数
        /// </summary>
        [JsonProperty("StableCount")]
        public int StableCount { get; set; } = 3;

        /// <summary>
        /// 超时时间（秒），0表示无限等待
        /// </summary>
        [JsonProperty("TimeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 稳定后将当前值赋值给指定变量
        /// </summary>
        [JsonProperty("AssignToVariable")]
        public string AssignToVariable { get; set; } = "";

        /// <summary>
        /// 超时后的动作
        /// </summary>
        [JsonProperty("OnTimeout")]
        public TimeoutAction OnTimeout { get; set; } = TimeoutAction.ContinueAndLog;

        /// <summary>
        /// 超时后跳转的步骤号
        /// </summary>
        [JsonProperty("TimeoutJumpToStep")]
        public int TimeoutJumpToStep { get; set; } = -1;
    }
}
