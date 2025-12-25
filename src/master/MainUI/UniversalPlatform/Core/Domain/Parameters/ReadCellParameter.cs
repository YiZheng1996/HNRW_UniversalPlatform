using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 读取单元格参数
    /// </summary>
    [Serializable]
    public class ReadCellParameter
    {
        /// <summary>
        /// 工作表名称
        /// </summary>
        [JsonProperty("SheetName")]
        public string SheetName { get; set; } = "Sheet1";

        /// <summary>
        /// 单元格地址（如A1, B2）
        /// </summary>
        [JsonProperty("CellAddress")]
        public string CellAddress { get; set; } = "";

        /// <summary>
        /// 读取后赋值给的变量名
        /// </summary>
        [JsonProperty("TargetVariable")]
        public string TargetVariable { get; set; } = "";

        /// <summary>
        /// 数据类型：String, Int, Double, Bool
        /// </summary>
        [JsonProperty("DataType")]
        public string DataType { get; set; } = "String";

        /// <summary>
        /// 默认值
        /// </summary>
        [JsonProperty("DefaultValue")]
        public object DefaultValue { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; } = "";
    }

}
