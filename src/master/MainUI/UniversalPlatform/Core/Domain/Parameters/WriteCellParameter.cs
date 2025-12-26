using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 写入单元格参数
    /// </summary>
    [Serializable]
    public class WriteCellParameter
    {
        /// <summary>
        /// 工作表名称
        /// </summary>
        [JsonProperty("SheetName")]
        public string SheetName { get; set; } = "Sheet1";

        /// <summary>
        /// 单元格地址
        /// </summary>
        [JsonProperty("CellAddress")]
        public string CellAddress { get; set; } = "";

        /// <summary>
        /// 值来源类型
        /// </summary>
        [JsonProperty("ValueType")]
        public CellValueType ValueType { get; set; } = CellValueType.Direct;

        /// <summary>
        /// 直接值
        /// </summary>
        [JsonProperty("DirectValue")]
        public object DirectValue { get; set; }

        /// <summary>
        /// 变量名
        /// </summary>
        [JsonProperty("VariableName")]
        public string VariableName { get; set; } = "";

        /// <summary>
        /// 表达式
        /// </summary>
        [JsonProperty("Expression")]
        public string Expression { get; set; } = "";

        /// <summary>
        /// 数据格式（如：0.00, yyyy-MM-dd）
        /// </summary>
        [JsonProperty("Format")]
        public string Format { get; set; } = "";

        /// <summary>
        /// 描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// 批量写入单元格参数
    /// </summary>
    [Serializable]
    public class WriteCellsParameter
    {
        /// <summary>
        /// 工作表名称
        /// </summary>
        [JsonProperty("SheetName")]
        public string SheetName { get; set; } = "Sheet1";

        /// <summary>
        /// 写入项列表
        /// </summary>
        [JsonProperty("Items")]
        public List<WriteCellItem> Items { get; set; } = new();

        /// <summary>
        /// 描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// 单个写入项
    /// </summary>
    [Serializable]
    public class WriteCellItem
    {
        [JsonProperty("CellAddress")]
        public string CellAddress { get; set; } = "";

        [JsonProperty("ValueType")]
        public CellValueType ValueType { get; set; } = CellValueType.Direct;

        [JsonProperty("Value")]
        public object Value { get; set; }

        [JsonProperty("Format")]
        public string Format { get; set; } = "";
    }
}
