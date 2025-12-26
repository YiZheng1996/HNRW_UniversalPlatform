using MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.Loop;
using MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.PLC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.Base
{
    /// <summary>
    /// 步骤表单工厂接口
    /// </summary>
    public interface IStepFormFactory
    {
        /// <summary>
        /// 创建步骤配置表单
        /// </summary>
        /// <param name="stepType">步骤类型名称</param>
        /// <param name="currentParameter">当前参数（可选）</param>
        /// <returns>表单实例</returns>
        IParameterForm CreateForm(string stepType, object currentParameter = null);

        /// <summary>
        /// 打开步骤配置表单并返回结果
        /// </summary>
        /// <param name="stepType">步骤类型名称</param>
        /// <param name="currentParameter">当前参数</param>
        /// <param name="owner">父窗体</param>
        /// <returns>对话框结果和新参数</returns>
        (DialogResult Result, object Parameter) ShowConfigForm(
            string stepType,
            object currentParameter = null,
            IWin32Window owner = null);

        /// <summary>
        /// 检查是否支持指定的步骤类型
        /// </summary>
        bool IsSupported(string stepType);

        /// <summary>
        /// 获取所有已注册的步骤类型
        /// </summary>
        IEnumerable<string> GetRegisteredStepTypes();
    }

    /// <summary>
    /// 步骤表单工厂实现
    /// </summary>
    public class StepFormFactory : IStepFormFactory
    {
        #region 私有字段

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StepFormFactory> _logger;

        /// <summary>
        /// 步骤类型到表单类型的映射
        /// </summary>
        private readonly Dictionary<string, Type> _formTypeMap;

        /// <summary>
        /// 步骤类型别名映射
        /// </summary>
        private readonly Dictionary<string, string> _stepTypeAliases;

        #endregion

        #region 构造函数

        public StepFormFactory(
            IServiceProvider serviceProvider,
            ILogger<StepFormFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;

            _formTypeMap = InitializeFormTypeMap();
            _stepTypeAliases = InitializeAliases();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化表单类型映射
        /// </summary>
        private Dictionary<string, Type> InitializeFormTypeMap()
        {
            return new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                // 逻辑控制步骤
                { "延时等待", typeof(DelayConfigForm) },
                { "消息通知", typeof(MessageConfigForm) },
                { "等待稳定", typeof(WaitStableConfigForm) },

                // 条件判断步骤
                { "条件判断", typeof(ConditionConfigForm) },
                { "检测工具", typeof(DetectionConfigForm) },

                // 循环控制步骤
                { "循环工具", typeof(LoopConfigForm) },

                // 变量操作步骤
                { "变量定义", typeof(VariableDefineConfigForm) },
                { "变量赋值", typeof(VariableAssignConfigForm) },

                // PLC 通信步骤
                { "读取PLC", typeof(PLCReadConfigForm) },
                { "写入PLC", typeof(PLCWriteConfigForm) },

                // 报表操作步骤
                { "读取单元格", typeof(ReadCellsConfigForm) },
                { "写入单元格", typeof(WriteCellsConfigForm) },
                { "保存报表", typeof(SaveReportConfigForm) },

                // 监控步骤
                { "实时监控", typeof(RealtimeMonitorConfigForm) },
                { "变量监控", typeof(VariableMonitorConfigForm) }
            };
        }

        /// <summary>
        /// 初始化步骤类型别名
        /// </summary>
        private Dictionary<string, string> InitializeAliases()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // 英文别名
                { "Delay", "延时等待" },
                { "DelayWait", "延时等待" },
                { "Message", "消息通知" },
                { "MessageNotify", "消息通知" },
                { "WaitStable", "等待稳定" },
                { "Condition", "条件判断" },
                { "ConditionJudge", "条件判断" },
                { "Detection", "检测工具" },
                { "Loop", "循环工具" },
                { "CycleBegins", "循环工具" },
                { "VariableDefine", "变量定义" },
                { "DefineVar", "变量定义" },
                { "VariableAssign", "变量赋值" },
                { "Assignment", "变量赋值" },
                { "PLCRead", "读取PLC" },
                { "ReadPLC", "读取PLC" },
                { "PLCWrite", "写入PLC" },
                { "WritePLC", "写入PLC" },
                { "ReadCells", "读取单元格" },
                { "CellRead", "读取单元格" },
                { "WriteCells", "写入单元格" },
                { "CellWrite", "写入单元格" },
                { "SaveReport", "保存报表" },
                { "ReportSave", "保存报表" },
                { "RealtimeMonitor", "实时监控" },
                { "MonitorTool", "实时监控" },
                { "VariableMonitor", "变量监控" }
            };
        }

        #endregion

        #region IStepFormFactory 实现

        /// <summary>
        /// 创建步骤配置表单
        /// </summary>
        public IParameterForm CreateForm(string stepType, object currentParameter = null)
        {
            if (string.IsNullOrWhiteSpace(stepType))
            {
                throw new ArgumentNullException(nameof(stepType));
            }

            // 解析别名
            var normalizedType = ResolveStepType(stepType);

            if (!_formTypeMap.TryGetValue(normalizedType, out var formType))
            {
                _logger?.LogWarning("未找到步骤类型 '{StepType}' 的配置表单", stepType);
                throw new NotSupportedException($"不支持的步骤类型: {stepType}");
            }

            try
            {
                _logger?.LogDebug("创建步骤配置表单: {StepType} -> {FormType}", stepType, formType.Name);

                // 通过 DI 容器创建表单
                var form = (IParameterForm)ActivatorUtilities.CreateInstance(_serviceProvider, formType);

                // 设置当前参数
                if (currentParameter != null)
                {
                    form.SetParameter(currentParameter);
                }

                return form;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建步骤配置表单失败: {StepType}", stepType);
                throw;
            }
        }

        /// <summary>
        /// 打开步骤配置表单并返回结果
        /// </summary>
        public (DialogResult Result, object Parameter) ShowConfigForm(
            string stepType,
            object currentParameter = null,
            IWin32Window owner = null)
        {
            try
            {
                using var form = CreateForm(stepType, currentParameter);

                DialogResult result;
                if (form is Form winForm)
                {
                    result = owner != null
                        ? winForm.ShowDialog(owner)
                        : winForm.ShowDialog();
                }
                else
                {
                    result = form.ShowDialog(owner);
                }

                if (result == DialogResult.OK)
                {
                    return (result, form.ResultParameter);
                }

                return (result, null);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开步骤配置表单失败: {StepType}", stepType);
                return (DialogResult.Cancel, null);
            }
        }

        /// <summary>
        /// 检查是否支持指定的步骤类型
        /// </summary>
        public bool IsSupported(string stepType)
        {
            if (string.IsNullOrWhiteSpace(stepType))
                return false;

            var normalizedType = ResolveStepType(stepType);
            return _formTypeMap.ContainsKey(normalizedType);
        }

        /// <summary>
        /// 获取所有已注册的步骤类型
        /// </summary>
        public IEnumerable<string> GetRegisteredStepTypes()
        {
            return _formTypeMap.Keys;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 解析步骤类型（处理别名）
        /// </summary>
        private string ResolveStepType(string stepType)
        {
            // 首先检查是否是别名
            if (_stepTypeAliases.TryGetValue(stepType, out var resolvedType))
            {
                return resolvedType;
            }

            // 否则返回原始类型
            return stepType;
        }

        /// <summary>
        /// 注册新的表单类型
        /// </summary>
        public void RegisterFormType(string stepType, Type formType)
        {
            if (string.IsNullOrWhiteSpace(stepType))
                throw new ArgumentNullException(nameof(stepType));

            if (formType == null)
                throw new ArgumentNullException(nameof(formType));

            if (!typeof(IParameterForm).IsAssignableFrom(formType))
                throw new ArgumentException($"类型 {formType.Name} 必须实现 IParameterForm 接口", nameof(formType));

            _formTypeMap[stepType] = formType;
            _logger?.LogInformation("注册步骤配置表单: {StepType} -> {FormType}", stepType, formType.Name);
        }

        /// <summary>
        /// 注册步骤类型别名
        /// </summary>
        public void RegisterAlias(string alias, string stepType)
        {
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentNullException(nameof(alias));

            if (string.IsNullOrWhiteSpace(stepType))
                throw new ArgumentNullException(nameof(stepType));

            _stepTypeAliases[alias] = stepType;
        }

        #endregion
    }

    #region 步骤表单特性

    /// <summary>
    /// 步骤表单特性 - 用于自动注册
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class StepFormAttribute : Attribute
    {
        /// <summary>
        /// 步骤类型名称
        /// </summary>
        public string StepType { get; }

        /// <summary>
        /// 别名列表
        /// </summary>
        public string[] Aliases { get; set; }

        public StepFormAttribute(string stepType)
        {
            StepType = stepType;
        }
    }

    #endregion
}
