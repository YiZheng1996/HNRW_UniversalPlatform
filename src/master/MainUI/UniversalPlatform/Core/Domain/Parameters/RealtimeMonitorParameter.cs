using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 实时监控提示参数
    /// </summary>
    [Serializable]
    public class RealtimeMonitorParameter
    {
        /// <summary>
        /// 窗体标题
        /// </summary>
        [JsonProperty("Title")]
        public string Title { get; set; } = "实时监控";

        /// <summary>
        /// 监测源类型
        /// </summary>
        [JsonProperty("MonitorSourceType")]
        public MonitorSourceType MonitorSourceType { get; set; } = MonitorSourceType.Variable;

        /// <summary>
        /// 监测的变量名
        /// </summary>
        [JsonProperty("MonitorVariable")]
        public string MonitorVariable { get; set; } = "";

        /// <summary>
        /// PLC模块名
        /// </summary>
        [JsonProperty("PlcModuleName")]
        public string PlcModuleName { get; set; } = "";

        /// <summary>
        /// PLC地址
        /// </summary>
        [JsonProperty("PlcAddress")]
        public string PlcAddress { get; set; } = "";

        /// <summary>
        /// 刷新间隔（毫秒）
        /// </summary>
        [JsonProperty("RefreshInterval")]
        public int RefreshInterval { get; set; } = 500;

        /// <summary>
        /// 数值格式
        /// </summary>
        [JsonProperty("ValueFormat")]
        public string ValueFormat { get; set; } = "F1";

        /// <summary>
        /// 按钮文本
        /// </summary>
        [JsonProperty("ButtonText")]
        public string ButtonText { get; set; } = "确定";

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        [JsonProperty("TimeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 0;

        /// <summary>
        /// 是否显示数值标签
        /// </summary>
        [JsonProperty("ShowValueLabel")]
        public bool ShowValueLabel { get; set; } = true;

        /// <summary>
        /// 数值标签文本
        /// </summary>
        [JsonProperty("ValueLabelText")]
        public string ValueLabelText { get; set; } = "";
    }

}
