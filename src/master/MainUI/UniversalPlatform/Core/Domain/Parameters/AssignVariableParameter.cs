using MainUI.UniversalPlatform.Infrastructure.Execution.Executors;
using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 变量赋值参数
    /// </summary>
    [Serializable]
    public class AssignVariableParameter
    {
        /// <summary>
        /// 目标变量名
        /// </summary>
        [JsonProperty("TargetVariable")]
        public string TargetVariable { get; set; } = "";

        /// <summary>
        /// 赋值类型
        /// </summary>
        [JsonProperty("AssignType")]
        public AssignmentType AssignType { get; set; } = AssignmentType.Direct;

        /// <summary>
        /// 直接值（当AssignType为Direct时使用）
        /// </summary>
        [JsonProperty("DirectValue")]
        public object DirectValue { get; set; }

        /// <summary>
        /// 表达式（当AssignType为Expression时使用）
        /// </summary>
        [JsonProperty("Expression")]
        public string Expression { get; set; } = "";

        /// <summary>
        /// 源变量名（当AssignType为Variable时使用）
        /// </summary>
        [JsonProperty("SourceVariable")]
        public string SourceVariable { get; set; } = "";

        /// <summary>
        /// PLC模块名（当AssignType为PLC时使用）
        /// </summary>
        [JsonProperty("PLCModule")]
        public string PLCModule { get; set; } = "";

        /// <summary>
        /// PLC标签（当AssignType为PLC时使用）
        /// </summary>
        [JsonProperty("PLCTag")]
        public string PLCTag { get; set; } = "";

        /// <summary>
        /// 描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; } = "";
    }
}
