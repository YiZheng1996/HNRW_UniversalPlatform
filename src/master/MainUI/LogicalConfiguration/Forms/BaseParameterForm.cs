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
    /// 参数表单基类 - 改进的依赖注入模式
    /// 
    /// 设计原则：
    /// 1. 设计器兼容：保留无参构造函数
    /// 2. 延迟解析：服务在首次使用时解析
    /// 3. 缓存服务：避免重复解析
    /// 4. 统一访问：通过受保护属性提供服务
    /// 
    /// 使用方式：
    /// - 子类通过受保护的属性访问服务（如 PLCManager, WorkflowState）
    /// - 子类重写 OnFormLoading() 进行初始化
    /// - 子类重写 LoadParameterToForm() 和 SaveParameterFromForm()
    /// </summary>
    public abstract class BaseParameterForm : UIForm
    {
        #region 服务缓存字段

        private IPLCManager _plcManager;
        private IWorkflowStateService _workflowState;
        private GlobalVariableManager _globalVariable;
        private IVariableSynchronizer _variableSynchronizer;
        private ILogger _logger;

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
        /// 全局变量管理器（只读访问器）
        /// </summary>
        protected GlobalVariableManager GlobalVariable =>
            _globalVariable ??= ResolveService<GlobalVariableManager>();

        /// <summary>
        /// 变量同步器
        /// </summary>
        protected IVariableSynchronizer VariableSynchronizer =>
            _variableSynchronizer ??= ResolveService<IVariableSynchronizer>();

        /// <summary>
        /// 日志器
        /// </summary>
        protected ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    var loggerType = typeof(ILogger<>).MakeGenericType(GetType());
                    _logger = ResolveService(loggerType) as ILogger;
                }
                return _logger;
            }
        }

        #endregion

        #region 状态属性

        private bool _isLoading = true;

        /// <summary>
        /// 是否正在加载中
        /// 在加载期间，不应触发保存逻辑
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

            try
            {
                _isLoading = true;

                // 调用子类的初始化逻辑
                OnFormLoading();

                // 加载参数到表单
                LoadParameterToForm();

                Logger?.LogDebug("表单加载完成: {FormType}", GetType().Name);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "表单加载失败: {FormType}", GetType().Name);
                MessageHelper.MessageOK($"加载失败：{ex.Message}", TType.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (DesignMode) return;

            // 如果是确定关闭，保存参数
            if (DialogResult == DialogResult.OK)
            {
                try
                {
                    SaveParameterFromForm();
                    Logger?.LogDebug("参数已保存: {FormType}", GetType().Name);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "保存参数失败: {FormType}", GetType().Name);
                    MessageHelper.MessageOK($"保存失败：{ex.Message}", TType.Error);
                    e.Cancel = true;
                }
            }
        }

        #endregion

        #region 可重写的方法

        /// <summary>
        /// 子类重写此方法进行初始化
        /// 在 LoadParameterToForm 之前调用
        /// </summary>
        protected virtual void OnFormLoading()
        {
            // 子类实现
        }

        /// <summary>
        /// 加载参数到表单控件
        /// 子类必须实现此方法
        /// </summary>
        protected abstract void LoadParameterToForm();

        /// <summary>
        /// 从表单控件保存参数
        /// 子类必须实现此方法
        /// </summary>
        protected abstract void SaveParameterFromForm();

        #endregion

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

        #endregion

        #region 内部类

        /// <summary>
        /// 下拉框选项
        /// </summary>
        protected class SelectItem(string displayText, object value)
        {
            public string DisplayText { get; } = displayText;

            public object Value { get; } = value;

            public override string ToString() => DisplayText;
        }

        #endregion
    }
}
