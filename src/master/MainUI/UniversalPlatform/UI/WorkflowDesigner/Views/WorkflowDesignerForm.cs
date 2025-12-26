using AntdUI;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Core.Domain.Variables;
using MainUI.UniversalPlatform.Core.Domain.Workflows;
using MainUI.UniversalPlatform.UI.WorkflowDesigner.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;
using TabPage = System.Windows.Forms.TabPage;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner.Views
{
    /// <summary>
    /// 工作流设计器窗体 - MVP模式的View实现
    /// 只负责UI显示和事件触发，不包含业务逻辑
    /// </summary>
    public partial class WorkflowDesignerForm : UIForm, IWorkflowDesignerView
    {
        #region 私有字段

        private WorkflowDesignerPresenter _presenter;
        private readonly ILogger<WorkflowDesignerForm> _logger;

        // UI控件
        private ToolboxControl _toolboxControl;
        private StepGridControl _stepGridControl;
        private VariablePanelControl _variablePanelControl;
        private Panel _detailPanel;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripProgressBar _progressBar;

        // 工作流信息
        private string _modelType;
        private string _modelName;
        private string _itemName;

        #endregion

        #region IWorkflowDesignerView 属性实现

        /// <summary>
        /// 当前选中的步骤索引
        /// </summary>
        public int SelectedStepIndex
        {
            get => _stepGridControl?.SelectedIndex ?? -1;
            set
            {
                if (_stepGridControl != null)
                    _stepGridControl.SelectedIndex = value;
            }
        }

        /// <summary>
        /// 是否处于执行状态
        /// </summary>
        public bool IsExecuting
        {
            set
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => IsExecuting = value));
                    return;
                }

                btnExecute.Enabled = !value;
                btnStop.Enabled = value;
                _toolboxControl.Enabled = !value;
                _stepGridControl.AllowEdit = !value;
                _progressBar.Visible = value;

                if (!value)
                {
                    _progressBar.Value = 0;
                }
            }
        }

        /// <summary>
        /// 窗体标题
        /// </summary>
        public new string Title
        {
            set => Text = value;
        }

        #endregion

        #region IWorkflowDesignerView 事件实现

        public event Action<string, int?> AddStepRequested;
        public event Action<int> DeleteStepRequested;
        public event Action<int, int> MoveStepRequested;
        public event Action<int> ConfigureStepRequested;
        public event Action<int> StepSelected;
        public event Action ExecuteRequested;
        public event Action StopRequested;
        public event Action SaveRequested;
        public event Func<bool> CloseRequested;
        public event Action AddVariableRequested;
        public event Action<string> DeleteVariableRequested;

        #endregion

        #region 构造函数

        /// <summary>
        /// 设计器构造函数
        /// </summary>
        public WorkflowDesignerForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 依赖注入构造函数
        /// </summary>
        public WorkflowDesignerForm(
            IWorkflowAppService workflowService,
            IVariableService variableService,
            IStepConfigService stepConfigService,
            ILogger<WorkflowDesignerForm> logger,
            string modelType,
            string modelName,
            string itemName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelType = modelType;
            _modelName = modelName;
            _itemName = itemName;

            InitializeComponent();
            InitializeCustomControls();

            // 创建Presenter
            _presenter = new WorkflowDesignerPresenter(
                this,
                workflowService,
                variableService,
                stepConfigService,
                Program.ServiceProvider.GetRequiredService<ILogger<WorkflowDesignerPresenter>>()
            );

            // 窗体加载时加载工作流
            Load += async (s, e) =>
            {
                await _presenter.LoadWorkflowAsync(_modelType, _modelName, _itemName);
            };
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化自定义控件
        /// </summary>
        private void InitializeCustomControls()
        {
            // 创建主布局
            var mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 220,
                FixedPanel = FixedPanel.Panel1
            };

            // 左侧面板 - 工具箱
            _toolboxControl = new ToolboxControl
            {
                Dock = DockStyle.Fill
            };
            _toolboxControl.ToolSelected += OnToolSelected;
            _toolboxControl.ToolDragStart += OnToolDragStart;
            mainSplitContainer.Panel1.Controls.Add(_toolboxControl);

            // 右侧面板
            var rightSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400
            };

            // 上部 - 步骤列表
            _stepGridControl = new StepGridControl
            {
                Dock = DockStyle.Fill
            };
            _stepGridControl.StepSelected += OnStepSelected;
            _stepGridControl.StepDoubleClicked += OnStepDoubleClicked;
            _stepGridControl.StepMoved += OnStepMoved;
            _stepGridControl.StepDeleted += OnStepDeleted;
            _stepGridControl.DragDropped += OnDragDropped;
            rightSplitContainer.Panel1.Controls.Add(_stepGridControl);

            // 下部 - 详情和变量
            var bottomTabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 详情选项卡
            var detailTab = new TabPage("步骤详情");
            _detailPanel = new Panel { Dock = DockStyle.Fill };
            detailTab.Controls.Add(_detailPanel);
            bottomTabControl.TabPages.Add(detailTab);

            // 变量选项卡
            var variableTab = new TabPage("变量管理");
            _variablePanelControl = new VariablePanelControl
            {
                Dock = DockStyle.Fill
            };
            _variablePanelControl.AddVariableClicked += () => AddVariableRequested?.Invoke();
            _variablePanelControl.DeleteVariableClicked += (name) => DeleteVariableRequested?.Invoke(name);
            variableTab.Controls.Add(_variablePanelControl);
            bottomTabControl.TabPages.Add(variableTab);

            rightSplitContainer.Panel2.Controls.Add(bottomTabControl);
            mainSplitContainer.Panel2.Controls.Add(rightSplitContainer);

            // 添加到主面板
            panelMain.Controls.Add(mainSplitContainer);

            // 初始化状态栏
            InitializeStatusBar();
        }

        /// <summary>
        /// 初始化状态栏
        /// </summary>
        private void InitializeStatusBar()
        {
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("就绪")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _progressBar = new ToolStripProgressBar
            {
                Visible = false,
                Width = 200
            };

            _statusStrip.Items.Add(_statusLabel);
            _statusStrip.Items.Add(_progressBar);

            Controls.Add(_statusStrip);
        }

        #endregion

        #region IWorkflowDesignerView 方法实现

        /// <summary>
        /// 显示步骤列表
        /// </summary>
        public void DisplaySteps(IReadOnlyList<WorkflowStep> steps)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => DisplaySteps(steps)));
                return;
            }

            _stepGridControl.SetSteps(steps);
            _statusLabel.Text = $"共 {steps.Count} 个步骤";
        }

        /// <summary>
        /// 显示变量列表
        /// </summary>
        public void DisplayVariables(IEnumerable<Variable> variables)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => DisplayVariables(variables)));
                return;
            }

            _variablePanelControl.SetVariables(variables);
        }

        /// <summary>
        /// 更新步骤状态
        /// </summary>
        public void UpdateStepStatus(int index, WorkflowStep step)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStepStatus(index, step)));
                return;
            }

            _stepGridControl.UpdateStepStatus(index, step.Status, step.ErrorMessage);
        }

        /// <summary>
        /// 更新步骤详情
        /// </summary>
        public void UpdateStepDetails(int index, string previewText)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStepDetails(index, previewText)));
                return;
            }

            // 更新详情面板
            _detailPanel.Controls.Clear();

            var label = new Label
            {
                Text = previewText,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Font = new Font("微软雅黑", 10)
            };
            _detailPanel.Controls.Add(label);
        }

        /// <summary>
        /// 显示执行进度
        /// </summary>
        public void ShowProgress(int current, int total, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowProgress(current, total, message)));
                return;
            }

            _progressBar.Visible = true;
            _progressBar.Maximum = total;
            _progressBar.Value = current;
            _statusLabel.Text = $"[{current}/{total}] {message}";
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        public void ShowMessage(string message, MessageLevel level = MessageLevel.Info)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowMessage(message, level)));
                return;
            }

            var type = level switch
            {
                MessageLevel.Success => TType.Success,
                MessageLevel.Warning => TType.Warn,
                MessageLevel.Error => TType.Error,
                _ => TType.Info
            };

            AntdUI.Message.info(this, message, autoClose: 3);
            _statusLabel.Text = message;
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public bool ShowConfirm(string message, string title = "确认")
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                   == DialogResult.Yes;
        }

        /// <summary>
        /// 刷新工具箱
        /// </summary>
        public void RefreshToolbox(IEnumerable<StepTypeInfo> stepTypes)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => RefreshToolbox(stepTypes)));
                return;
            }

            _toolboxControl.SetStepTypes(stepTypes);
        }

        /// <summary>
        /// 刷新整个视图
        /// </summary>
        public void RefreshView()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RefreshView));
                return;
            }

            _stepGridControl.Refresh();
            _variablePanelControl.Refresh();
        }

        /// <summary>
        /// 滚动到指定步骤
        /// </summary>
        public void ScrollToStep(int index)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ScrollToStep(index)));
                return;
            }

            _stepGridControl.ScrollToStep(index);
        }

        /// <summary>
        /// 高亮执行中的步骤
        /// </summary>
        public void HighlightExecutingStep(int index)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => HighlightExecutingStep(index)));
                return;
            }

            _stepGridControl.HighlightStep(index);
            ScrollToStep(index);
        }

        #endregion

        #region 事件处理 - 触发Presenter

        private void OnToolSelected(string stepName)
        {
            // 工具箱选中工具时添加步骤
            AddStepRequested?.Invoke(stepName, null);
        }

        private void OnToolDragStart(string stepName, DragEventArgs e)
        {
            // 开始拖拽
        }

        private void OnStepSelected(int index)
        {
            StepSelected?.Invoke(index);
        }

        private void OnStepDoubleClicked(int index)
        {
            ConfigureStepRequested?.Invoke(index);
        }

        private void OnStepMoved(int fromIndex, int toIndex)
        {
            MoveStepRequested?.Invoke(fromIndex, toIndex);
        }

        private void OnStepDeleted(int index)
        {
            DeleteStepRequested?.Invoke(index);
        }

        private void OnDragDropped(string stepName, int? insertIndex)
        {
            AddStepRequested?.Invoke(stepName, insertIndex);
        }

        #endregion

        #region 按钮事件

        private void BtnExecute_Click(object sender, EventArgs e)
        {
            ExecuteRequested?.Invoke();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopRequested?.Invoke();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveRequested?.Invoke();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion

        #region 窗体事件

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (CloseRequested != null)
            {
                bool canClose = CloseRequested.Invoke();
                if (!canClose)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // 清理 Presenter
            _presenter?.Dispose();

            base.OnFormClosing(e);
        }

        #endregion
    }
}
