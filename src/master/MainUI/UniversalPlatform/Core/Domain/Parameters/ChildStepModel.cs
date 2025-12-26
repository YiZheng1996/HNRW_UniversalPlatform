using Newtonsoft.Json;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 子步骤模型（用于条件和循环中的嵌套步骤）
    /// </summary>
    [Serializable]
    public class ChildStepModel
    {
        /// <summary>
        /// 步骤序号
        /// </summary>
        [JsonProperty("StepNum")]
        public int StepNum { get; set; }

        /// <summary>
        /// 步骤名称/类型
        /// </summary>
        [JsonProperty("StepName")]
        public string StepName { get; set; } = "";

        /// <summary>
        /// 步骤参数
        /// </summary>
        [JsonProperty("StepParameter")]
        public object StepParameter { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [JsonProperty("Remark")]
        public string Remark { get; set; } = "";

        /// <summary>
        /// 状态
        /// </summary>
        [JsonProperty("Status")]
        public int Status { get; set; } = 0;

        /// <summary>
        /// 错误信息
        /// </summary>
        [JsonProperty("ErrorMessage")]
        public string ErrorMessage { get; set; } = "";
    }

}
