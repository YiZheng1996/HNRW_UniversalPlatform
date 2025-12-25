using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// PLC写入参数
    /// </summary>
    [Serializable]
    public class PLCWriteParameter
    {
        /// <summary>
        /// PLC模块名
        /// </summary>
        [JsonProperty("ModuleName")]
        public string ModuleName { get; set; } = "";

        /// <summary>
        /// PLC地址/标签
        /// </summary>
        [JsonProperty("Address")]
        public string Address { get; set; } = "";

        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonProperty("DataType")]
        public string DataType { get; set; } = "Int32";

        /// <summary>
        /// 写入值类型
        /// </summary>
        [JsonProperty("ValueType")]
        public PLCValueType ValueType { get; set; } = PLCValueType.Direct;

        /// <summary>
        /// 直接值
        /// </summary>
        [JsonProperty("DirectValue")]
        public object DirectValue { get; set; }

        /// <summary>
        /// 变量名（当ValueType为Variable时使用）
        /// </summary>
        [JsonProperty("VariableName")]
        public string VariableName { get; set; } = "";

        /// <summary>
        /// 表达式（当ValueType为Expression时使用）
        /// </summary>
        [JsonProperty("Expression")]
        public string Expression { get; set; } = "";

        /// <summary>
        /// 超时时间（毫秒）
        /// </summary>
        [JsonProperty("TimeoutMs")]
        public int TimeoutMs { get; set; } = 3000;

        /// <summary>
        /// 是否验证写入
        /// </summary>
        [JsonProperty("VerifyWrite")]
        public bool VerifyWrite { get; set; } = false;

        /// <summary>
        /// 描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; } = "";
    }
}
