using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 消息通知参数
    /// </summary>
    [Serializable]
    public class MessageParameter
    {
        /// <summary>
        /// 消息内容
        /// </summary>
        [JsonProperty("Message")]
        public string Message { get; set; } = "";

        /// <summary>
        /// 消息类型：Info, Warning, Error, Success
        /// </summary>
        [JsonProperty("MessageType")]
        public string MessageType { get; set; } = "Info";

        /// <summary>
        /// 是否阻塞等待确认
        /// </summary>
        [JsonProperty("WaitForConfirm")]
        public bool WaitForConfirm { get; set; } = true;

        /// <summary>
        /// 超时时间（秒），0表示无限等待
        /// </summary>
        [JsonProperty("TimeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 0;
    }
}
