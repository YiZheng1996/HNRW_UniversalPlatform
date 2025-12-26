using MainUI.UniversalPlatform.Core.Domain.Workflows;
using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 条件判断参数
    /// </summary>
    [Serializable]
    public class ConditionParameter
    {
        /// <summary>
        /// 完整条件表达式
        /// </summary>
        [JsonProperty("ConditionExpression")]
        public string ConditionExpression { get; set; } = "";

        /// <summary>
        /// 左值表达式
        /// </summary>
        [JsonProperty("LeftExpression")]
        public string LeftExpression { get; set; } = "";

        /// <summary>
        /// 运算符：==, !=, >, <, >=, <=
        /// </summary>
        [JsonProperty("Operator")]
        public string Operator { get; set; } = "==";

        /// <summary>
        /// 右值表达式
        /// </summary>
        [JsonProperty("RightExpression")]
        public string RightExpression { get; set; } = "";

        /// <summary>
        /// 条件满足时执行的子步骤
        /// </summary>
        [JsonProperty("TrueSteps")]
        public List<WorkflowStep> TrueSteps { get; set; } = new();

        /// <summary>
        /// 条件不满足时执行的子步骤
        /// </summary>
        [JsonProperty("FalseSteps")]
        public List<WorkflowStep> FalseSteps { get; set; } = new();

        /// <summary>
        /// 描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; } = "";

        /// <summary>
        /// 获取完整的条件表达式
        /// </summary>
        public string GetFullExpression()
        {
            if (!string.IsNullOrWhiteSpace(ConditionExpression))
                return ConditionExpression;

            if (!string.IsNullOrWhiteSpace(LeftExpression) && !string.IsNullOrWhiteSpace(Operator))
                return $"{LeftExpression} {Operator} {RightExpression}";

            return "";
        }
    }
}
