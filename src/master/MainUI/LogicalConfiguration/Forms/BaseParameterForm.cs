using AntdUI;
using MainUI.LogicalConfiguration.LogicalManager;
using MainUI.LogicalConfiguration.Services;
using MainUI.LogicalConfiguration.Services.ServicesPLC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
    public class BaseParameterForm : UIForm
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
        /// 是否正在加载中
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

        #endregion

        #region 构造函数

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
        /// 窗体加载事件 - 自动从工作流加载参数
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode) return;

            try
            {
                LoadParametersFromWorkflow();
            }
            finally
            {
                _isLoading = false;
            }
        }

        #endregion

        #region 参数管理核心方法

        /// <summary>
        /// 从工作流加载参数 - 统一逻辑
        /// </summary>
        protected virtual void LoadParametersFromWorkflow()
        {
            if (DesignMode || WorkflowState == null) return;

            var currentStep = GetCurrentStepSafely();
            if (currentStep?.StepParameter != null)
            {
                try
                {
                    // 调用子类的参数转换方法
                    var convertedParameter = ConvertParameter(currentStep.StepParameter);

                    // 设置参数（通过反射访问子类的 Parameter 属性）
                    SetParameterValue(convertedParameter);

                    Logger?.LogInformation("成功加载参数: {FormType}", GetType().Name);

                    // 调用子类的加载方法
                    LoadParameterToForm();
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "参数转换失败: {FormType}", GetType().Name);
                    SetDefaultValues();
                }
            }
            else
            {
                SetDefaultValues();
            }
        }

        /// <summary>
        /// 保存参数到工作流 - 统一的保存逻辑
        /// </summary>
        protected virtual void SaveParameters()
        {
            if (DesignMode || WorkflowState == null) return;

            try
            {
                var currentStep = GetCurrentStepSafely();
                if (currentStep == null)
                {
                    Logger?.LogWarning("步骤索引无效，无法保存参数: StepNum={StepNum}", WorkflowState.StepNum);
                    MessageHelper.MessageOK("步骤索引无效，无法保存参数。", TType.Error);
                    return;
                }

                if (!ValidateInput())
                {
                    Logger?.LogWarning("参数验证失败: {FormType}", GetType().Name);
                    return;
                }

                // 调用子类方法将界面数据保存到参数对象
                SaveFormToParameter();

                // 获取参数对象（通过反射访问子类的 Parameter 属性）
                var parameter = GetParameterValue();

                // 清理花括号
                CleanBracketsFromProperties(parameter);

                // 更新到工作流
                WorkflowState.UpdateStepParameter(WorkflowState.StepNum, parameter);

                Logger?.LogInformation("参数保存成功: {FormType}, StepNum={StepNum}",
                    GetType().Name, WorkflowState.StepNum);

                MessageHelper.MessageOK("参数已暂存，主界面点击保存后才会写入文件。", TType.Info);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "保存参数失败: {FormType}", GetType().Name);
                MessageHelper.MessageOK($"保存参数失败：{ex.Message}", TType.Error);
            }
        }

        #endregion

        #region 反射辅助方法

        /// <summary>
        /// 通过反射获取子类的 Parameter 属性值
        /// </summary>
        protected virtual object GetParameterValue()
        {
            var parameterProperty = GetType().GetProperty("Parameter");
            if (parameterProperty != null && parameterProperty.CanRead)
            {
                return parameterProperty.GetValue(this);
            }

            Logger?.LogWarning("未找到 Parameter 属性: {FormType}", GetType().Name);
            return null;
        }

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

            // 3. 尝试序列化再反序列化（处理匿名对象）
            try
            {
                string jsonString = JsonConvert.SerializeObject(stepParameter);
                return JsonConvert.DeserializeObject(jsonString, parameterType);
            }
            catch (JsonException ex)
            {
                Logger?.LogWarning(ex, "对象转换失败，使用默认参数");
            }

            // 4. 最终兜底 - 创建默认实例
            Logger?.LogDebug("所有转换方法失败，返回默认参数实例");
            try
            {
                return Activator.CreateInstance(parameterType);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "创建默认参数实例失败");
                return null;
            }
        }

        #endregion

        #region 虚方法 - 子类按需重写

        /// <summary>
        /// 加载参数到界面控件
        /// 子类必须重写此方法，将参数的值填充到界面控件
        /// </summary>
        protected virtual void LoadParameterToForm()
        {
            // 子类实现：从 Parameter 读取数据并填充到控件
        }

        /// <summary>
        /// 从界面控件保存到参数
        /// 子类必须重写此方法，将界面控件的值保存到 Parameter
        /// </summary>
        protected virtual void SaveFormToParameter()
        {
            // 子类实现：从控件读取数据并保存到 Parameter
        }

        /// <summary>
        /// 设置默认值
        /// 子类可以重写此方法，设置参数的默认值
        /// </summary>
        protected virtual void SetDefaultValues()
        {
            // 尝试创建默认的参数对象
            var parameterType = GetParameterType();
            if (parameterType != null)
            {
                try
                {
                    var defaultParameter = Activator.CreateInstance(parameterType);
                    SetParameterValue(defaultParameter);
                    Logger?.LogDebug("使用默认参数: {ParameterType}", parameterType.Name);
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(ex, "创建默认参数失败");
                }
            }

            LoadParameterToForm();
        }

        /// <summary>
        /// 验证输入
        /// 子类可以重写此方法，实现自定义验证逻辑
        /// </summary>
        protected virtual bool ValidateInput()
        {
            return true;
        }

        #endregion

        #region 按钮事件处理

        /// <summary>
        /// 确定按钮点击事件 - 保存参数并关闭窗体
        /// 子类可以在按钮事件中调用 SaveParameters()
        /// </summary>
        protected virtual void OnOkButtonClick(object sender, EventArgs e)
        {
            SaveParameters();
        }

        /// <summary>
        /// 取消按钮点击事件 - 直接关闭窗体不保存
        /// </summary>
        protected virtual void OnCancelButtonClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 安全获取当前步骤 - 防止索引越界异常
        /// </summary>
        protected ChildModel GetCurrentStepSafely()
        {
            if (WorkflowState == null) return null;

            try
            {
                var steps = WorkflowState.GetSteps();
                int idx = WorkflowState.StepNum;

                if (steps != null && idx >= 0 && idx < steps.Count)
                {
                    return steps[idx];
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