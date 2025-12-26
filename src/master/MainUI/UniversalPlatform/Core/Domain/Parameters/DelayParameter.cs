using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 延时等待参数
    /// </summary>
    [Serializable]
    public class DelayParameter
    {
        /// <summary>
        /// 延时时间（毫秒）
        /// 兼容旧版本的 T 字段
        /// </summary>
        [JsonProperty("T")]
        public double DelayMs { get; set; } = 1000;

        /// <summary>
        /// 延时时间（秒）- 优先使用
        /// </summary>
        [JsonProperty("DelaySeconds")]
        public double? DelaySeconds { get; set; }

        /// <summary>
        /// 延时描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取实际延时毫秒数
        /// </summary>
        public int GetActualDelayMs()
        {
            if (DelaySeconds.HasValue)
                return (int)(DelaySeconds.Value * 1000);
            return (int)DelayMs;
        }
    }
}
