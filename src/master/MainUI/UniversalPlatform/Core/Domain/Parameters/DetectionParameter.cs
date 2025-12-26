using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 检测判定参数
    /// </summary>
    [Serializable]
    public class DetectionParameter
    {
        /// <summary>
        /// 检测名称
        /// </summary>
        [JsonProperty("DetectionName")]
        public string DetectionName { get; set; } = "";

        /// <summary>
        /// 条件表达式
        /// </summary>
        [JsonProperty("ConditionExpression")]
        public string ConditionExpression { get; set; } = "";

        /// <summary>
        /// 检测值来源
        /// </summary>
        [JsonProperty("ValueSource")]
        public DetectionValueSource ValueSource { get; set; } = DetectionValueSource.Variable;

        /// <summary>
        /// 变量名（当ValueSource为Variable时使用）
        /// </summary>
        [JsonProperty("VariableName")]
        public string VariableName { get; set; } = "";

        /// <summary>
        /// PLC模块名（当ValueSource为PLC时使用）
        /// </summary>
        [JsonProperty("PLCModule")]
        public string PLCModule { get; set; } = "";

        /// <summary>
        /// PLC地址（当ValueSource为PLC时使用）
        /// </summary>
        [JsonProperty("PLCAddress")]
        public string PLCAddress { get; set; } = "";

        /// <summary>
        /// 判定类型
        /// </summary>
        [JsonProperty("JudgmentType")]
        public JudgmentType JudgmentType { get; set; } = JudgmentType.Range;

        /// <summary>
        /// 最小值（范围判定）
        /// </summary>
        [JsonProperty("MinValue")]
        public double MinValue { get; set; }

        /// <summary>
        /// 最大值（范围判定）
        /// </summary>
        [JsonProperty("MaxValue")]
        public double MaxValue { get; set; }

        /// <summary>
        /// 期望值（精确判定）
        /// </summary>
        [JsonProperty("ExpectedValue")]
        public object ExpectedValue { get; set; }

        /// <summary>
        /// 结果处理
        /// </summary>
        [JsonProperty("ResultHandling")]
        public ResultHandlingConfig ResultHandling { get; set; } = new();

        /// <summary>
        /// 描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; } = "";
    }
}
