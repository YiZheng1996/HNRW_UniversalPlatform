namespace MainUI.UniversalPlatform.Core.Domain.Workflows
{
    /// <summary>
    /// 工作流步骤实体
    /// </summary>
    public class WorkflowStep
    {
        #region 属性

        /// <summary>
        /// 步骤唯一标识
        /// </summary>
        public Guid Id { get;  set; }

        /// <summary>
        /// 步骤序号（1开始）
        /// </summary>
        public int StepNumber { get;  set; }

        /// <summary>
        /// 步骤名称（类型）
        /// </summary>
        public string StepName { get; set; }

        /// <summary>
        /// 步骤参数
        /// </summary>
        public object Parameter { get;  set; }

        /// <summary>
        /// 步骤备注
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 执行状态
        /// </summary>
        public StepStatus Status { get;  set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get;  set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 步骤类型（用于分类显示）
        /// </summary>
        public StepCategory Category => GetCategory(StepName);

        #endregion

        #region 构造函数
        public WorkflowStep()
        {
            // 用于重建对象
        }

        /// <summary>
        /// 创建新步骤
        /// </summary>
        public WorkflowStep(int stepNumber, string stepName, object parameter = null, string remark = null)
        {
            Id = Guid.NewGuid();
            StepNumber = stepNumber;
            StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
            Parameter = parameter;
            Remark = remark;
            Status = StepStatus.Pending;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 设置步骤序号
        /// </summary>
        internal void SetStepNumber(int number)
        {
            StepNumber = number;
        }

        /// <summary>
        /// 更新参数
        /// </summary>
        public void UpdateParameter(object parameter)
        {
            Parameter = parameter;
        }

        /// <summary>
        /// 设置状态为执行中
        /// </summary>
        public void MarkAsRunning()
        {
            Status = StepStatus.Running;
            ErrorMessage = null;
        }

        /// <summary>
        /// 设置状态为成功
        /// </summary>
        public void MarkAsSucceeded()
        {
            Status = StepStatus.Succeeded;
            ErrorMessage = null;
        }

        /// <summary>
        /// 设置状态为失败
        /// </summary>
        public void MarkAsFailed(string errorMessage)
        {
            Status = StepStatus.Failed;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 设置状态为跳过
        /// </summary>
        public void MarkAsSkipped()
        {
            Status = StepStatus.Skipped;
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void ResetStatus()
        {
            Status = StepStatus.Pending;
            ErrorMessage = null;
        }

        /// <summary>
        /// 获取类型化的参数
        /// </summary>
        public T GetParameter<T>() where T : class, new()
        {
            if (Parameter == null)
                return new T();

            if (Parameter is T typedParam)
                return typedParam;

            // 尝试JSON转换
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(Parameter);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return new T();
            }
        }

        /// <summary>
        /// 获取步骤类别
        /// </summary>
        private static StepCategory GetCategory(string stepName)
        {
            return stepName switch
            {
                "延时等待" or "等待稳定" or "消息通知" => StepCategory.Logic,
                "条件判断" => StepCategory.Condition,
                "循环工具" => StepCategory.Loop,
                "变量赋值" or "变量定义" => StepCategory.Variable,
                "读取PLC" or "写入PLC" => StepCategory.Communication,
                "读取单元格" or "写入单元格" => StepCategory.Report,
                "实时监控" => StepCategory.Monitor,
                _ => StepCategory.Other
            };
        }

        #endregion

        #region 工厂方法

        /// <summary>
        /// 从现有数据重建步骤
        /// </summary>
        public static WorkflowStep Reconstitute(
            Guid id,
            int stepNumber,
            string stepName,
            object parameter,
            string remark,
            StepStatus status,
            string errorMessage) => new()
            {
                Id = id,
                StepNumber = stepNumber,
                StepName = stepName,
                Parameter = parameter,
                Remark = remark,
                Status = status,
                ErrorMessage = errorMessage
            };

        #endregion
    }

}
