using AntdUI;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Core.Domain.Parameters;
using MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Panel = System.Windows.Forms.Panel;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms
{
    /// <summary>
    /// 参数表单基类（增强版）
    /// 提供统一的参数加载、保存、验证逻辑
    /// 集成 ExpressionInputPanel 通用表达式输入功能
    /// </summary>
    public abstract partial class BaseParameterForm : UIForm, IParameterForm
    {
        #region 服务依赖

        /// <summary>
        /// 变量服务
        /// </summary>
        protected IVariableService VariableService { get; private set; }

        /// <summary>
        /// 日志服务
        /// </summary>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// 表达式输入面板工厂
        /// </summary>
        protected IExpressionInputFactory ExpressionInputFactory { get; private set; }

        /// <summary>
        /// 服务提供者
        /// </summary>
        protected IServiceProvider ServiceProvider { get; private set; }

        #endregion

        #region IParameterForm 属性

        /// <summary>
        /// 步骤类型名称 - 子类必须实现
        /// </summary>
        public abstract string StepType { get; }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [Browsable(false)]
        public bool IsLoading { get; protected set; } = true;

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        [Browsable(false)]
        public bool HasUnsavedChanges { get; protected set; } = false;

        /// <summary>
        /// 对话框结果参数
        /// </summary>
        [Browsable(false)]
        public object ResultParameter { get; protected set; }

        #endregion

        #region 事件

        /// <summary>
        /// 参数变更事件
        /// </summary>
        public event EventHandler<ParameterChangedEventArgs> ParameterChanged;

        /// <summary>
        /// 验证完成事件
        /// </summary>
        public event EventHandler<ValidationCompletedEventArgs> ValidationCompleted;

        #endregion

        #region 私有字段

        /// <summary>
        /// 已附加表达式输入的控件列表
        /// </summary>
        private readonly List<Control> _expressionInputControls = [];

        /// <summary>
        /// 验证定时器（防抖）
        /// </summary>
        private System.Windows.Forms.Timer _validationTimer;

        /// <summary>
        /// 按钮面板
        /// </summary>
        private Panel _buttonPanel;

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数（设计器使用）
        /// </summary>
        protected BaseParameterForm()
        {
            if (DesignMode) return;

            InitializeServices();
            InitializeValidationTimer();
        }

        /// <summary>
        /// 依赖注入构造函数
        /// </summary>
        protected BaseParameterForm(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            if (DesignMode) return;

            InitializeServicesFromProvider();
            InitializeValidationTimer();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化服务（从全局容器）
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                ServiceProvider = Program.ServiceProvider;
                InitializeServicesFromProvider();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BaseParameterForm 服务初始化警告: {ex.Message}");
            }
        }

        /// <summary>
        /// 从服务提供者初始化服务
        /// </summary>
        private void InitializeServicesFromProvider()
        {
            if (ServiceProvider == null) return;

            VariableService = ServiceProvider.GetService<IVariableService>();
            Logger = ServiceProvider.GetService<ILogger<BaseParameterForm>>();
            ExpressionInputFactory = ServiceProvider.GetService<IExpressionInputFactory>();
        }

        /// <summary>
        /// 初始化验证定时器
        /// </summary>
        private void InitializeValidationTimer()
        {
            _validationTimer = new System.Windows.Forms.Timer
            {
                Interval = 300  // 300ms 防抖
            };
            _validationTimer.Tick += (s, e) =>
            {
                _validationTimer.Stop();
                PerformValidation();
            };
        }

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode) return;

            try
            {
                IsLoading = true;

                // 初始化窗体样式
                InitializeFormStyle();

                // 初始化表达式输入面板（子类实现）
                InitializeExpressionInputs();

                // 初始化按钮面板
                InitializeButtonPanel();

                // 绑定通用事件
                BindCommonEvents();

                // 加载参数到界面
                LoadParameterToForm();

                Logger?.LogDebug("{FormType} 窗体加载完成", GetType().Name);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "{FormType} 窗体加载失败", GetType().Name);
                MessageHelper.MessageOK(this, $"初始化失败：{ex.Message}", TType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 初始化窗体样式 - 统一风格
        /// </summary>
        protected virtual void InitializeFormStyle()
        {
            Text = $"{StepType} - 参数配置";
            TitleColor = UIColors.Primary;
            RectColor = UIColors.Primary;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
        }

        /// <summary>
        /// 初始化表达式输入面板 - 子类重写此方法配置表达式输入
        /// </summary>
        protected virtual void InitializeExpressionInputs()
        {
            // 子类重写此方法，为需要表达式输入的控件附加面板
            // 示例：
            // txtExpression.WithExpressionInput();
            // txtCondition.WithConditionInput();
            // txtVariable.WithVariableInput();
        }

        /// <summary>
        /// 初始化按钮面板
        /// </summary>
        protected virtual void InitializeButtonPanel()
        {
            // 检查是否已经有按钮面板
            if (_buttonPanel != null) return;

            _buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = UIColors.BackgroundGray,
                Padding = new Padding(15, 10, 15, 10)
            };

            // 保存按钮
            var btnSave = new AntdUI.Button
            {
                Text = "保存",
                Type = TTypeMini.Primary,
                Size = new Size(90, 36),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom
            };
            btnSave.Click += (s, e) => SaveAndClose();

            // 取消按钮
            var btnCancel = new AntdUI.Button
            {
                Text = "取消",
                Size = new Size(90, 36),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom
            };
            btnCancel.Click += (s, e) => CancelAndClose();

            // 重置按钮
            var btnReset = new AntdUI.Button
            {
                Text = "重置",
                Size = new Size(90, 36),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            btnReset.Click += (s, e) => ResetToDefault();

            // 布局
            btnCancel.Location = new Point(_buttonPanel.Width - btnCancel.Width - 15, 12);
            btnSave.Location = new Point(btnCancel.Left - btnSave.Width - 10, 12);
            btnReset.Location = new Point(15, 12);

            _buttonPanel.Controls.AddRange(new Control[] { btnSave, btnCancel, btnReset });
            Controls.Add(_buttonPanel);

            // 处理面板大小变化时重新布局按钮
            _buttonPanel.Resize += (s, e) =>
            {
                btnCancel.Location = new Point(_buttonPanel.Width - btnCancel.Width - 15, 12);
                btnSave.Location = new Point(btnCancel.Left - btnSave.Width - 10, 12);
            };
        }

        /// <summary>
        /// 绑定通用事件
        /// </summary>
        private void BindCommonEvents()
        {
            FormClosing += OnFormClosing;
            KeyDown += OnKeyDown;
        }

        #endregion

        #region IParameterForm 方法

        /// <summary>
        /// 设置参数
        /// </summary>
        public virtual void SetParameter(object parameter)
        {
            try
            {
                IsLoading = true;
                OnSetParameter(parameter);
                LoadParameterToForm();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        public virtual object GetParameter()
        {
            SaveFormToParameter();
            return OnGetParameter();
        }

        /// <summary>
        /// 验证输入
        /// </summary>
        public virtual ParameterValidationResult Validate()
        {
            var result = OnValidate();
            ValidationCompleted?.Invoke(this, new ValidationCompletedEventArgs(result));
            return result;
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public virtual void ResetToDefault()
        {
            try
            {
                IsLoading = true;
                OnResetToDefault();
                LoadParameterToForm();
                HasUnsavedChanges = false;

                Logger?.LogDebug("{FormType} 已重置为默认值", GetType().Name);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region 子类重写方法

        /// <summary>
        /// 设置参数时调用 - 子类重写
        /// </summary>
        protected virtual void OnSetParameter(object parameter) { }

        /// <summary>
        /// 获取参数时调用 - 子类重写
        /// </summary>
        protected virtual object OnGetParameter() => null;

        /// <summary>
        /// 验证时调用 - 子类重写
        /// </summary>
        protected virtual ParameterValidationResult OnValidate() => ParameterValidationResult.Valid();

        /// <summary>
        /// 重置时调用 - 子类重写
        /// </summary>
        protected virtual void OnResetToDefault() { }

        /// <summary>
        /// 加载参数到界面 - 子类重写
        /// </summary>
        protected virtual void LoadParameterToForm() { }

        /// <summary>
        /// 从界面保存到参数 - 子类重写
        /// </summary>
        protected virtual void SaveFormToParameter() { }

        #endregion

        #region 表达式输入辅助方法

        /// <summary>
        /// 为文本框附加表达式输入面板
        /// </summary>
        /// <param name="textBox">目标文本框</param>
        /// <param name="options">配置选项</param>
        protected void AttachExpressionInput(UITextBox textBox, InputPanelOptions options = null)
        {
            if (textBox == null) return;

            options ??= new InputPanelOptions();
            ExpressionInputPanel.AttachTo(textBox, options);
            _expressionInputControls.Add(textBox);
        }

        /// <summary>
        /// 为文本框附加条件输入面板
        /// </summary>
        protected void AttachConditionInput(UITextBox textBox)
        {
            AttachExpressionInput(textBox, InputPanelOptions.ForCondition());
        }

        /// <summary>
        /// 为文本框附加变量选择面板
        /// </summary>
        protected void AttachVariableInput(UITextBox textBox)
        {
            AttachExpressionInput(textBox, InputPanelOptions.ForVariable());
        }

        /// <summary>
        /// 为文本框附加 PLC 地址选择面板
        /// </summary>
        protected void AttachPLCInput(UITextBox textBox)
        {
            AttachExpressionInput(textBox, InputPanelOptions.ForPLC());
        }

        /// <summary>
        /// 为文本框附加数值输入面板
        /// </summary>
        protected void AttachNumericInput(UITextBox textBox)
        {
            AttachExpressionInput(textBox, InputPanelOptions.ForNumeric());
        }

        #endregion

        #region 保存/取消逻辑

        /// <summary>
        /// 保存并关闭
        /// </summary>
        protected virtual void SaveAndClose()
        {
            try
            {
                // 验证
                var validationResult = Validate();
                if (!validationResult.IsValid)
                {
                    MessageHelper.MessageOK(this, validationResult.Message, TType.Error);
                    return;
                }

                // 显示警告（如果有）
                if (validationResult.Warnings.Count > 0)
                {
                    var warningMessage = string.Join("\n", validationResult.Warnings);
                    var dialogResult = MessageBox.Show(
                        $"存在以下警告:\n{warningMessage}\n\n是否继续保存?",
                        "警告",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (dialogResult != DialogResult.Yes)
                        return;
                }

                // 保存参数
                SaveFormToParameter();
                ResultParameter = OnGetParameter();

                Logger?.LogInformation("{FormType} 参数保存成功", GetType().Name);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "{FormType} 保存参数失败", GetType().Name);
                MessageHelper.MessageOK(this, $"保存失败：{ex.Message}", TType.Error);
            }
        }

        /// <summary>
        /// 取消并关闭
        /// </summary>
        protected virtual void CancelAndClose()
        {
            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "有未保存的更改，确定要关闭吗？",
                    "确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;
            }

            DialogResult = DialogResult.Cancel;
            Close();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            // 清理表达式输入面板
            foreach (var control in _expressionInputControls)
            {
                if (control is UITextBox textBox)
                {
                    ExpressionInputPanel.DetachFrom(textBox);
                }
            }
            _expressionInputControls.Clear();

            // 清理定时器
            _validationTimer?.Stop();
            _validationTimer?.Dispose();
        }

        /// <summary>
        /// 按键事件
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CancelAndClose();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                SaveAndClose();
                e.Handled = true;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 标记为已修改
        /// </summary>
        protected void MarkAsModified()
        {
            if (!IsLoading)
            {
                HasUnsavedChanges = true;
                ScheduleValidation();
            }
        }

        /// <summary>
        /// 触发参数变更事件
        /// </summary>
        protected void RaiseParameterChanged(string propertyName, object oldValue, object newValue)
        {
            if (!IsLoading)
            {
                ParameterChanged?.Invoke(this, new ParameterChangedEventArgs(propertyName, oldValue, newValue));
                MarkAsModified();
            }
        }

        /// <summary>
        /// 计划验证（防抖）
        /// </summary>
        protected void ScheduleValidation()
        {
            _validationTimer?.Stop();
            _validationTimer?.Start();
        }

        /// <summary>
        /// 执行验证
        /// </summary>
        private void PerformValidation()
        {
            var result = Validate();
            UpdateValidationUI(result);
        }

        /// <summary>
        /// 更新验证 UI - 子类可重写
        /// </summary>
        protected virtual void UpdateValidationUI(ParameterValidationResult result)
        {
            // 子类可重写此方法更新验证状态显示
        }

        /// <summary>
        /// 获取所有变量名列表
        /// </summary>
        protected List<string> GetVariableNames()
        {
            if (VariableService == null)
                return new List<string>();

            return VariableService.GetAllVariables()
                .Select(v => v.Name)
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>
        /// 填充变量下拉框
        /// </summary>
        protected void FillVariableComboBox(ComboBox comboBox, string selectedValue = null)
        {
            if (comboBox == null) return;

            comboBox.Items.Clear();
            comboBox.Items.AddRange(GetVariableNames().ToArray());

            if (!string.IsNullOrEmpty(selectedValue) && comboBox.Items.Contains(selectedValue))
            {
                comboBox.SelectedItem = selectedValue;
            }
        }

        /// <summary>
        /// 参数类型转换
        /// </summary>
        protected T ConvertParameter<T>(object parameter) where T : class, new()
        {
            return ParameterManager.GetParameter<T>(parameter);
        }

        #endregion

        #region UI 颜色定义

        /// <summary>
        /// UI 颜色配置
        /// </summary>
        protected static class UIColors
        {
            // 主题色
            public static readonly Color Primary = Color.FromArgb(24, 144, 255);
            public static readonly Color PrimaryLight = Color.FromArgb(230, 244, 255);
            public static readonly Color PrimaryHover = Color.FromArgb(64, 169, 255);

            // 状态色
            public static readonly Color Success = Color.FromArgb(82, 196, 26);
            public static readonly Color SuccessLight = Color.FromArgb(246, 255, 237);
            public static readonly Color Error = Color.FromArgb(255, 77, 79);
            public static readonly Color ErrorLight = Color.FromArgb(255, 241, 240);
            public static readonly Color Warning = Color.FromArgb(250, 173, 20);

            // 背景色
            public static readonly Color Background = Color.White;
            public static readonly Color BackgroundGray = Color.FromArgb(248, 249, 250);
            public static readonly Color BackgroundLight = Color.FromArgb(245, 247, 250);

            // 边框色
            public static readonly Color Border = Color.FromArgb(217, 217, 217);
            public static readonly Color BorderLight = Color.FromArgb(240, 240, 240);
            public static readonly Color BorderHover = Color.FromArgb(24, 144, 255);

            // 文字色
            public static readonly Color TextPrimary = Color.FromArgb(38, 38, 38);
            public static readonly Color TextSecondary = Color.FromArgb(115, 115, 115);
            public static readonly Color TextDisabled = Color.FromArgb(191, 191, 191);
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _validationTimer?.Dispose();
                _validationTimer = null;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
