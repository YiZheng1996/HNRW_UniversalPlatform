using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 变量定义参数
    /// </summary>
    [Serializable]
    public class DefineVariableParameter
    {
        /// <summary>
        /// 变量名
        /// </summary>
        [JsonProperty("VarName")]
        public string VarName { get; set; } = "";

        /// <summary>
        /// 变量类型：String, Int, Double, Bool
        /// </summary>
        [JsonProperty("VarType")]
        public string VarType { get; set; } = "String";

        /// <summary>
        /// 初始值
        /// </summary>
        [JsonProperty("VarValue")]
        public object VarValue { get; set; }

        /// <summary>
        /// 变量描述
        /// </summary>
        [JsonProperty("VarText")]
        public string VarText { get; set; } = "";
    }
}
