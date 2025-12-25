using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// PLC读取参数
    /// </summary>
    [Serializable]
    public class PLCReadParameter
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
        /// 数据类型：Bool, Int16, Int32, Float, Double, String
        /// </summary>
        [JsonProperty("DataType")]
        public string DataType { get; set; } = "Int32";

        /// <summary>
        /// 读取后赋值给的变量名
        /// </summary>
        [JsonProperty("TargetVariable")]
        public string TargetVariable { get; set; } = "";

        /// <summary>
        /// 读取失败时的默认值
        /// </summary>
        [JsonProperty("DefaultValue")]
        public object DefaultValue { get; set; }

        /// <summary>
        /// 超时时间（毫秒）
        /// </summary>
        [JsonProperty("TimeoutMs")]
        public int TimeoutMs { get; set; } = 3000;

        /// <summary>
        /// 描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; } = "";
    }
}
