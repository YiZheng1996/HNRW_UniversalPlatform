using AntdUI;
using MainUI.LogicalConfiguration.LogicalManager;
using MainUI.LogicalConfiguration.Services;
using MainUI.LogicalConfiguration.Services.ServicesPLC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MainUI.LogicalConfiguration.Forms
{
    /// <summary>
    /// 参数表单基类 - 完整重构版
    /// 
    /// 改进点：
    /// 1. 延迟服务解析（设计器兼容）
    /// 2. 保留所有原有功能方法
    /// 3. 新增变量同步器支持
    /// 
    /// 子类只需定义 Parameter 属性并重写虚方法即可
    /// </summary>
    public abstract class BaseParameterForm : UIForm
    {
        #region 服务缓存字段

        private IPLCManager _plcManager;
        private IWorkflowStateService _workflowState;
        private GlobalVariableManager _globalVariable;
        private IVariableSynchronizer _variableSynchronizer;
        private Microsoft.Extensions.Logging.ILogger _logger;

        #endregion

        #region 受保护的服务属性（延迟解析）

        /// <summary>
        /// PLC 管理器
        /// </summary>
        protected IPLCManager PLCManager =>
            _plcManager ??= ResolveService<IPLCManager>();

        /// <summary>
        /// 工作流状态服务
        /// </summary>
        protected IWorkflowStateService WorkflowState =>
            _workflowState ??= ResolveRequiredService<IWorkflowStateService>();

        /// <summary>
        /// 全局变量管理器
        /// </summary>
        protected GlobalVariableManager GlobalVariable =>
            _globalVariable ??= ResolveService<GlobalVariableManager>();

        /// <summary>
        /// 变量同步器（新增）
        /// </summary>
        protected IVariableSynchronizer VariableSynchronizer =>
            _variableSynchronizer ??= ResolveService<IVariableSynchronizer>();

        /// <summary>
        /// 日志器
        /// </summary>
        protected Microsoft.Extensions.Logging.ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    var loggerType = typeof(ILogger<>).MakeGenericType(GetType());
                    _logger = ResolveService(loggerType) as Microsoft.Extensions.Logging.ILogger;
                }
                return _logger;
            }
        }

        /// <summary>
        /// 检查服务是否可用
        /// </summary>
        protected bool IsServiceAvailable => WorkflowState != null;

        #endregion

        #region 状态属性

        private bool _isLoading = true;

        /// <summary>
        /// PLC 管理器
        /// </summary>
        protected bool IsLoading
        {
            get => _isLoading;
            set => _isLoading = value;
        }

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        protected bool HasUnsavedChanges { get; set; }

        /// <summary>
        /// 工作流状态服务
        /// </summary>
        protected IWorkflowStateService WorkflowState =>
            _workflowState ??= ResolveRequiredService<IWorkflowStateService>();

        /// <summary>
        /// 全局变量管理器（只读访问器）
        /// </summary>
        protected GlobalVariableManager GlobalVariable =>
            _globalVariable ??= ResolveService<GlobalVariableManager>();

        /// <summary>
        /// 无参构造函数 - 供设计器使用
        /// 服务将在首次访问时延迟解析
        /// </summary>
        public BaseParameterForm()
        {
            // 设计时不做任何操作，服务延迟解析
        }

        /// <summary>
        /// 依赖注入构造函数（推荐）
        /// </summary>
        protected BaseParameterForm(
            IWorkflowStateService workflowState,
            Microsoft.Extensions.Logging.ILogger logger)
        {
            _workflowState = workflowState ?? throw new ArgumentNullException(nameof(workflowState));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 服务解析

        /// <summary>
        /// 解析必需服务（不存在则抛异常）
        /// </summary>
        private T ResolveRequiredService<T>() where T : class
        {
            if (DesignMode) return null;

            var service = Program.ServiceProvider?.GetService<T>();
            if (service == null)
            {
                var errorMessage = $"无法解析必需服务: {typeof(T).Name}。请确保已正确配置依赖注入。";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            return service;
        }

        /// <summary>
        /// 解析可选服务（不存在则返回null）
        /// </summary>
        private T ResolveService<T>() where T : class
        {
            if (DesignMode) return null;

            try
            {
                return Program.ServiceProvider?.GetService<T>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析服务 {typeof(T).Name} 失败: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 状态属性

        private bool _isLoading = true;

        /// <summary>
        /// 按类型解析服务
        /// </summary>
        private object ResolveService(Type serviceType)
        {
            if (DesignMode) return null;

            try
            {
                return Program.ServiceProvider?.GetService(serviceType);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析服务 {serviceType.Name} 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        protected bool HasUnsavedChanges { get; set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 无参构造函数 - 供设计器使用
        /// 服务将在首次访问时延迟解析
        /// </summary>
        protected BaseParameterForm()
        {
            // 设计时不做任何操作
        }

        /// <summary>
        /// 依赖注入构造函数 - 运行时使用
        /// </summary>
        protected BaseParameterForm(
            IWorkflowStateService workflowState,
            ILogger logger)
        {
            _workflowState = workflowState ?? throw new ArgumentNullException(nameof(workflowState));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 服务解析

        /// <summary>
        /// 解析必需服务（不存在则抛异常）
        /// </summary>
        private T ResolveRequiredService<T>() where T : class
        {
            if (DesignMode) return null;

            var service = Program.ServiceProvider?.GetService<T>();
            if (service == null)
            {
                var errorMessage = $"无法解析必需服务: {typeof(T).Name}。请确保已正确配置依赖注入。";
                Logger?.LogError(message: errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            return service;
        }

        /// <summary>
        /// 解析可选服务（不存在则返回null）
        /// </summary>
        private T ResolveService<T>() where T : class
        {
            if (DesignMode) return null;

            try
            {
                return Program.ServiceProvider?.GetService<T>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析服务 {typeof(T).Name} 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 按类型解析服务
        /// </summary>
        private object ResolveService(Type serviceType)
        {
            if (DesignMode) return null;

            try
            {
                return Program.ServiceProvider?.GetService(serviceType);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析服务 {serviceType.Name} 失败: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 生命周期方法

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode) return;

        /// <summary>
        /// 通过反射设置子类的 Parameter 属性值
        /// </summary>
        protected virtual void SetParameterValue(object value)
        {
            var parameterProperty = GetType().GetProperty("Parameter");
            if (parameterProperty != null && parameterProperty.CanWrite)
            {
                parameterProperty.SetValue(this, value);
                Logger?.LogDebug("已设置 Parameter 属性: {FormType}", GetType().Name);
            }
            else
            {
                Logger?.LogWarning("未找到可写的 Parameter 属性: {FormType}", GetType().Name);
            }
        }

        /// <summary>
        /// 获取子类 Parameter 属性的类型
        /// </summary>
        protected virtual Type GetParameterType()
        {
            var parameterProperty = GetType().GetProperty("Parameter");
            return parameterProperty?.PropertyType;
        }

        #endregion

        #region 参数转换方法

        /// <summary>
        /// 转换参数 - 统一的参数转换逻辑
        /// 支持多种来源格式
        /// </summary>
        protected virtual object ConvertParameter(object stepParameter)
        {
            if (stepParameter == null) return null;

            var parameterType = GetParameterType();
            if (parameterType == null)
            {
                Logger?.LogWarning("无法获取参数类型");
                return null;
            }

            // 1. 如果已经是目标类型，直接返回
            if (parameterType.IsInstanceOfType(stepParameter))
            {
                return stepParameter;
            }

            // 2. 如果是JObject，尝试转换
            if (stepParameter is Newtonsoft.Json.Linq.JObject jObj)
            {
                try
                {
                    return jObj.ToObject(parameterType);
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(ex, "JObject转换失败");
                }
            }

        #region 辅助方法

        /// <summary>
        /// 安全执行操作（带异常处理）
        /// </summary>
        protected void SafeExecute(Action action, string operationName = null)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "操作失败: {Operation}", operationName ?? "未知操作");
                MessageHelper.MessageOK($"操作失败：{ex.Message}", TType.Error);
            }
        }

        /// <summary>
        /// 安全执行异步操作
        /// </summary>
        protected async Task SafeExecuteAsync(Func<Task> action, string operationName = null)
        {
            try
            {
                if (action != null)
                {
                    await action();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "异步操作失败: {Operation}", operationName ?? "未知操作");
                MessageHelper.MessageOK($"操作失败：{ex.Message}", TType.Error);
            }
        }

        /// <summary>
        /// 获取所有变量名称列表（用于下拉框绑定）
        /// </summary>
        protected List<string> GetVariableNames(bool includeSystemVariables = false)
        {
            var variables = includeSystemVariables
                ? GlobalVariable?.GetAllVariables()
                : GlobalVariable?.GetAllUserVariables();

            return variables?.Select(v => v.VarName).ToList() ?? [];
        }

        /// <summary>
        /// 获取变量下拉数据源
        /// </summary>
        protected List<SelectItem> GetVariableSelectItems(bool includeSystemVariables = false)
        {
            var variables = includeSystemVariables
                ? GlobalVariable?.GetAllVariables()
                : GlobalVariable?.GetAllUserVariables();

            return variables?.Select(v => new SelectItem(
                $"{v.VarName} ({v.VarType})",
                v.VarName
            )).ToList() ?? [];
        }

        /// <summary>
        /// 标记为已修改
        /// </summary>
        protected void MarkAsModified()
        {
            if (!_isLoading)
            {
                HasUnsavedChanges = true;
            }
        }

        /// <summary>
        /// 绑定文本框变更事件（自动标记修改）
        /// </summary>
        protected void BindTextChangedForModification(params Control[] controls)
        {
            if (WorkflowState == null) return null;

            try
            {
                var steps = WorkflowState.GetSteps();
                int idx = WorkflowState.StepNum;

                if (steps != null && idx >= 0 && idx < steps.Count)
                {
                    textBox.TextChanged += (s, e) => MarkAsModified();
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.SelectedIndexChanged += (s, e) => MarkAsModified();
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.CheckedChanged += (s, e) => MarkAsModified();
                }
                else if (control is NumericUpDown numericUpDown)
                {
                    numericUpDown.ValueChanged += (s, e) => MarkAsModified();
                }

                Logger?.LogWarning("步骤索引超出范围: Index={Index}, Count={Count}", idx, steps?.Count ?? 0);
                return null;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "获取当前步骤失败");
                return null;
            }
        }

        /// <summary>
        /// 清理对象属性中的花括号
        /// 防止表达式中的花括号被误认为是变量引用
        /// </summary>
        protected void CleanBracketsFromProperties(object obj)
        {
            if (obj == null) return;

            var properties = obj.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        // 清理开头和结尾的花括号（如果是变量引用格式）
                        var cleaned = CleanVariableBrackets(value);
                        if (cleaned != value)
                        {
                            prop.SetValue(obj, cleaned);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(ex, "清理属性花括号失败: {PropertyName}", prop.Name);
                }
            }
        }

        /// <summary>
        /// 清理变量引用中的花括号
        /// 例如: "{变量名}" -> "变量名"
        /// </summary>
        protected string CleanVariableBrackets(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            // 只处理 {变量名} 格式，不处理 {表达式} 或 PLC 引用
            if (value.StartsWith("{") && value.EndsWith("}") && !value.Contains("."))
            {
                var inner = value.Substring(1, value.Length - 2);
                // 确保不是表达式（不包含运算符）
                if (!inner.Contains("+") && !inner.Contains("-") &&
                    !inner.Contains("*") && !inner.Contains("/"))
                {
                    return inner;
                }
            }

            return value;
        }

        /// <summary>
        /// 获取所有变量名称列表（用于下拉框绑定）
        /// </summary>
        protected List<string> GetVariableNames(bool includeSystemVariables = false)
        {
            var variables = includeSystemVariables
                ? GlobalVariable?.GetAllVariables()
                : GlobalVariable?.GetAllUserVariables();

            return variables?.Select(v => v.VarName).ToList() ?? new List<string>();
        }

        /// <summary>
        /// 获取变量下拉数据源
        /// </summary>
        protected List<SelectItem> GetVariableSelectItems(bool includeSystemVariables = false)
        {
            var variables = includeSystemVariables
                ? GlobalVariable?.GetAllVariables()
                : GlobalVariable?.GetAllUserVariables();

            return variables?.Select(v => new SelectItem(
                $"{v.VarName} ({v.VarType})",
                v.VarName
            )).ToList() ?? new List<SelectItem>();
        }

        /// <summary>
        /// 标记为已修改
        /// </summary>
        protected void MarkAsModified()
        {
            if (!_isLoading)
            {
                HasUnsavedChanges = true;
            }
        }

        /// <summary>
        /// 绑定控件变更事件（自动标记修改）
        /// </summary>
        protected void BindTextChangedForModification(params Control[] controls)
        {
            foreach (var control in controls)
            {
                if (control is TextBox textBox)
                {
                    textBox.TextChanged += (s, e) => MarkAsModified();
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.SelectedIndexChanged += (s, e) => MarkAsModified();
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.CheckedChanged += (s, e) => MarkAsModified();
                }
                else if (control is NumericUpDown numericUpDown)
                {
                    numericUpDown.ValueChanged += (s, e) => MarkAsModified();
                }
            }
        }

        /// <summary>
        /// 安全执行操作（带异常处理）
        /// </summary>
        protected void SafeExecute(Action action, string operationName = null)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "操作失败: {Operation}", operationName ?? "未知操作");
                MessageHelper.MessageOK($"操作失败：{ex.Message}", TType.Error);
            }
        }

        /// <summary>
        /// 安全执行异步操作
        /// </summary>
        protected async Task SafeExecuteAsync(Func<Task> action, string operationName = null)
        {
            try
            {
                if (action != null)
                {
                    await action();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "异步操作失败: {Operation}", operationName ?? "未知操作");
                MessageHelper.MessageOK($"操作失败：{ex.Message}", TType.Error);
            }
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 下拉框选项
        /// </summary>
        protected class SelectItem
        {
            public string DisplayText { get; }
            public object Value { get; }

            public SelectItem(string displayText, object value)
            {
                DisplayText = displayText;
                Value = value;
            }

            public override string ToString() => DisplayText;
        }

        #endregion
    }
}
