using AntdUI;
using MainUI.UniversalPlatform.UI.Common.Controls;
using MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.Base;
using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.Loop
{
    /// <summary>
    /// 循环工具配置表单
    /// 支持计数循环、条件循环、集合遍历
    /// </summary>
    public partial class LoopConfigForm : BaseParameterForm
    {
        #region 属性

        public override string StepType => "循环工具";

        private LoopParameter _parameter;
        public LoopParameter Parameter
        {
            get => _parameter;
            set
            {
                _parameter = value ?? new LoopParameter();
                if (!DesignMode && !IsLoading && IsHandleCreated)
                {
                    LoadParameterToForm();
                }
            }
        }

        #endregion

        #region 控件声明

        private Panel _mainPanel;
        private Panel _loopTypePanel;
        private RadioButton _rbCountLoop;
        private RadioButton _rbConditionLoop;
        private RadioButton _rbForEachLoop;

        // 计数循环面板
        private Panel _countLoopPanel;
        private UITextBox _txtStartValue;
        private UITextBox _txtEndValue;
        private UITextBox _txtStepValue;
        private UITextBox _txtCounterVariable;

        // 条件循环面板
        private Panel _conditionLoopPanel;
        private UITextBox _txtConditionExpression;
        private CheckBox _chkPreCheck;

        // 集合遍历面板
        private Panel _forEachPanel;
        private UITextBox _txtCollectionExpression;
        private UITextBox _txtItemVariable;

        // 公共选项
        private Panel _optionsPanel;
        private UITextBox _txtMaxIterations;
        private UITextBox _txtTimeout;
        private CheckBox _chkBreakOnError;
        private CheckBox _chkEnabled;

        #endregion

        #region 构造函数

        public LoopConfigForm()
        {
            InitializeComponent();
            if (!DesignMode) InitializeForm();
        }

        public LoopConfigForm(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            InitializeComponent();
            InitializeForm();
        }

        #endregion

        #region 初始化

        private void InitializeForm()
        {
            Size = new Size(560, 580);
            CreateControls();
            BindEvents();
        }

        private void CreateControls()
        {
            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            Controls.Add(_mainPanel);

            int yPos = 20;

            // 循环类型选择
            CreateLoopTypePanel(ref yPos);

            // 计数循环面板
            CreateCountLoopPanel(ref yPos);

            // 条件循环面板
            CreateConditionLoopPanel(ref yPos);

            // 集合遍历面板
            CreateForEachPanel(ref yPos);

            // 公共选项面板
            CreateOptionsPanel(ref yPos);

            // 启用复选框
            _chkEnabled = new CheckBox
            {
                Text = "启用此步骤",
                Location = new Point(20, yPos + 10),
                Size = new Size(200, 24),
                Checked = true,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            _mainPanel.Controls.Add(_chkEnabled);
        }

        private void CreateLoopTypePanel(ref int yPos)
        {
            _loopTypePanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(500, 90),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = "循环类型",
                Location = new Point(10, 10),
                Size = new Size(480, 20),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _loopTypePanel.Controls.Add(lblTitle);

            _rbCountLoop = new RadioButton
            {
                Text = "计数循环 (For)",
                Location = new Point(20, 40),
                Size = new Size(130, 24),
                Checked = true,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            _loopTypePanel.Controls.Add(_rbCountLoop);

            _rbConditionLoop = new RadioButton
            {
                Text = "条件循环 (While)",
                Location = new Point(170, 40),
                Size = new Size(140, 24),
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            _loopTypePanel.Controls.Add(_rbConditionLoop);

            _rbForEachLoop = new RadioButton
            {
                Text = "集合遍历 (ForEach)",
                Location = new Point(330, 40),
                Size = new Size(150, 24),
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            _loopTypePanel.Controls.Add(_rbForEachLoop);

            _mainPanel.Controls.Add(_loopTypePanel);
            yPos += 100;
        }

        private void CreateCountLoopPanel(ref int yPos)
        {
            _countLoopPanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(500, 140),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = "计数循环设置",
                Location = new Point(10, 10),
                Size = new Size(480, 20),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _countLoopPanel.Controls.Add(lblTitle);

            // 起始值
            var lblStart = new Label { Text = "起始值:", Location = new Point(20, 45), Size = new Size(70, 24) };
            _txtStartValue = new UITextBox { Location = new Point(95, 40), Size = new Size(120, 32), Watermark = "例: 0" };
            _countLoopPanel.Controls.Add(lblStart);
            _countLoopPanel.Controls.Add(_txtStartValue);

            // 结束值
            var lblEnd = new Label { Text = "结束值:", Location = new Point(240, 45), Size = new Size(70, 24) };
            _txtEndValue = new UITextBox { Location = new Point(315, 40), Size = new Size(120, 32), Watermark = "例: 10" };
            _countLoopPanel.Controls.Add(lblEnd);
            _countLoopPanel.Controls.Add(_txtEndValue);

            // 步进值
            var lblStep = new Label { Text = "步进值:", Location = new Point(20, 85), Size = new Size(70, 24) };
            _txtStepValue = new UITextBox { Location = new Point(95, 80), Size = new Size(120, 32), Watermark = "例: 1" };
            _countLoopPanel.Controls.Add(lblStep);
            _countLoopPanel.Controls.Add(_txtStepValue);

            // 计数器变量
            var lblCounter = new Label { Text = "计数器:", Location = new Point(240, 85), Size = new Size(70, 24) };
            _txtCounterVariable = new UITextBox { Location = new Point(315, 80), Size = new Size(120, 32), Watermark = "例: i" };
            _countLoopPanel.Controls.Add(lblCounter);
            _countLoopPanel.Controls.Add(_txtCounterVariable);

            _mainPanel.Controls.Add(_countLoopPanel);
            yPos += 150;
        }

        private void CreateConditionLoopPanel(ref int yPos)
        {
            _conditionLoopPanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(500, 100),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            var lblTitle = new Label
            {
                Text = "条件循环设置",
                Location = new Point(10, 10),
                Size = new Size(480, 20),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _conditionLoopPanel.Controls.Add(lblTitle);

            var lblCondition = new Label { Text = "循环条件:", Location = new Point(20, 45), Size = new Size(80, 24) };
            _txtConditionExpression = new UITextBox
            {
                Location = new Point(105, 40),
                Size = new Size(330, 32),
                Watermark = "例: ${counter} < 100"
            };
            _conditionLoopPanel.Controls.Add(lblCondition);
            _conditionLoopPanel.Controls.Add(_txtConditionExpression);

            _chkPreCheck = new CheckBox
            {
                Text = "先检查条件再执行 (while)",
                Location = new Point(20, 75),
                Size = new Size(300, 20),
                Checked = true
            };
            _conditionLoopPanel.Controls.Add(_chkPreCheck);

            _mainPanel.Controls.Add(_conditionLoopPanel);
        }

        private void CreateForEachPanel(ref int yPos)
        {
            _forEachPanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(500, 100),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            var lblTitle = new Label
            {
                Text = "集合遍历设置",
                Location = new Point(10, 10),
                Size = new Size(480, 20),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _forEachPanel.Controls.Add(lblTitle);

            var lblCollection = new Label { Text = "集合表达式:", Location = new Point(20, 45), Size = new Size(90, 24) };
            _txtCollectionExpression = new UITextBox
            {
                Location = new Point(115, 40),
                Size = new Size(320, 32),
                Watermark = "例: ${dataList}"
            };
            _forEachPanel.Controls.Add(lblCollection);
            _forEachPanel.Controls.Add(_txtCollectionExpression);

            var lblItem = new Label { Text = "当前项变量:", Location = new Point(20, 80), Size = new Size(90, 24) };
            _txtItemVariable = new UITextBox
            {
                Location = new Point(115, 75),
                Size = new Size(150, 32),
                Watermark = "例: item"
            };
            _forEachPanel.Controls.Add(lblItem);
            _forEachPanel.Controls.Add(_txtItemVariable);

            _mainPanel.Controls.Add(_forEachPanel);
        }

        private void CreateOptionsPanel(ref int yPos)
        {
            _optionsPanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(500, 100),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = "循环选项",
                Location = new Point(10, 10),
                Size = new Size(480, 20),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _optionsPanel.Controls.Add(lblTitle);

            // 最大迭代次数
            var lblMax = new Label { Text = "最大次数:", Location = new Point(20, 45), Size = new Size(70, 24) };
            _txtMaxIterations = new UITextBox
            {
                Location = new Point(95, 40),
                Size = new Size(100, 32),
                Watermark = "默认1000"
            };
            _optionsPanel.Controls.Add(lblMax);
            _optionsPanel.Controls.Add(_txtMaxIterations);

            // 超时时间
            var lblTimeout = new Label { Text = "超时(秒):", Location = new Point(220, 45), Size = new Size(70, 24) };
            _txtTimeout = new UITextBox
            {
                Location = new Point(295, 40),
                Size = new Size(100, 32),
                Watermark = "默认60"
            };
            _optionsPanel.Controls.Add(lblTimeout);
            _optionsPanel.Controls.Add(_txtTimeout);

            // 错误时中断
            _chkBreakOnError = new CheckBox
            {
                Text = "错误时中断循环",
                Location = new Point(20, 75),
                Size = new Size(150, 20),
                Checked = true
            };
            _optionsPanel.Controls.Add(_chkBreakOnError);

            _mainPanel.Controls.Add(_optionsPanel);
            yPos += 110;
        }

        private void BindEvents()
        {
            _rbCountLoop.CheckedChanged += LoopTypeChanged;
            _rbConditionLoop.CheckedChanged += LoopTypeChanged;
            _rbForEachLoop.CheckedChanged += LoopTypeChanged;

            // 绑定修改事件
            _txtStartValue.TextChanged += (s, e) => MarkAsModified();
            _txtEndValue.TextChanged += (s, e) => MarkAsModified();
            _txtStepValue.TextChanged += (s, e) => MarkAsModified();
            _txtCounterVariable.TextChanged += (s, e) => MarkAsModified();
            _txtConditionExpression.TextChanged += (s, e) => MarkAsModified();
            _txtCollectionExpression.TextChanged += (s, e) => MarkAsModified();
            _txtItemVariable.TextChanged += (s, e) => MarkAsModified();
            _txtMaxIterations.TextChanged += (s, e) => MarkAsModified();
            _txtTimeout.TextChanged += (s, e) => MarkAsModified();
            _chkBreakOnError.CheckedChanged += (s, e) => MarkAsModified();
            _chkEnabled.CheckedChanged += (s, e) => MarkAsModified();
        }

        private void LoopTypeChanged(object sender, EventArgs e)
        {
            _countLoopPanel.Visible = _rbCountLoop.Checked;
            _conditionLoopPanel.Visible = _rbConditionLoop.Checked;
            _forEachPanel.Visible = _rbForEachLoop.Checked;

            // 调整公共选项面板位置
            int newY = _rbCountLoop.Checked ? _countLoopPanel.Bottom + 10 :
                       _rbConditionLoop.Checked ? _conditionLoopPanel.Top + 110 :
                       _forEachPanel.Top + 110;

            _optionsPanel.Location = new Point(20, newY);
            _chkEnabled.Location = new Point(20, _optionsPanel.Bottom + 10);

            MarkAsModified();
        }

        #endregion

        #region 表达式输入初始化

        protected override void InitializeExpressionInputs()
        {
            // 计数循环表达式
            _txtStartValue.WithExpressionInput(options =>
            {
                options.Mode = InputMode.Numeric;
                options.EnabledModules = InputModules.Variable | InputModules.Constant;
                options.Title = "设置起始值";
            });

            _txtEndValue.WithExpressionInput(options =>
            {
                options.Mode = InputMode.Numeric;
                options.EnabledModules = InputModules.Variable | InputModules.Constant;
                options.Title = "设置结束值";
            });

            _txtStepValue.WithExpressionInput(options =>
            {
                options.Mode = InputMode.Numeric;
                options.EnabledModules = InputModules.Variable | InputModules.Constant;
                options.Title = "设置步进值";
            });

            // 计数器变量
            _txtCounterVariable.WithVariableInput();

            // 条件循环表达式
            _txtConditionExpression.WithConditionInput();

            // 集合遍历
            _txtCollectionExpression.WithExpressionInput(options =>
            {
                options.Mode = InputMode.Expression;
                options.EnabledModules = InputModules.Variable | InputModules.Expression;
                options.Title = "设置集合表达式";
            });

            _txtItemVariable.WithVariableInput();
        }

        #endregion

        #region 参数操作

        protected override void OnSetParameter(object parameter)
        {
            _parameter = ConvertParameter<LoopParameter>(parameter);
        }

        protected override object OnGetParameter()
        {
            return _parameter;
        }

        protected override void LoadParameterToForm()
        {
            if (_parameter == null)
            {
                _parameter = new LoopParameter();
            }

            try
            {
                IsLoading = true;

                // 循环类型
                switch (_parameter.LoopType)
                {
                    case LoopType.Count:
                        _rbCountLoop.Checked = true;
                        break;
                    case LoopType.Condition:
                        _rbConditionLoop.Checked = true;
                        break;
                    case LoopType.ForEach:
                        _rbForEachLoop.Checked = true;
                        break;
                }

                // 计数循环参数
                _txtStartValue.Text = _parameter.StartValue;
                _txtEndValue.Text = _parameter.EndValue;
                _txtStepValue.Text = _parameter.StepValue;
                _txtCounterVariable.Text = _parameter.CounterVariable;

                // 条件循环参数
                _txtConditionExpression.Text = _parameter.ConditionExpression;
                _chkPreCheck.Checked = _parameter.PreCheck;

                // 集合遍历参数
                _txtCollectionExpression.Text = _parameter.CollectionExpression;
                _txtItemVariable.Text = _parameter.ItemVariable;

                // 公共选项
                _txtMaxIterations.Text = _parameter.MaxIterations.ToString();
                _txtTimeout.Text = _parameter.TimeoutSeconds.ToString();
                _chkBreakOnError.Checked = _parameter.BreakOnError;
                _chkEnabled.Checked = _parameter.IsEnabled;

                HasUnsavedChanges = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected override void SaveFormToParameter()
        {
            // 循环类型
            _parameter.LoopType = _rbCountLoop.Checked ? LoopType.Count :
                                  _rbConditionLoop.Checked ? LoopType.Condition :
                                  LoopType.ForEach;

            // 计数循环参数
            _parameter.StartValue = _txtStartValue.Text;
            _parameter.EndValue = _txtEndValue.Text;
            _parameter.StepValue = _txtStepValue.Text;
            _parameter.CounterVariable = _txtCounterVariable.Text;

            // 条件循环参数
            _parameter.ConditionExpression = _txtConditionExpression.Text;
            _parameter.PreCheck = _chkPreCheck.Checked;

            // 集合遍历参数
            _parameter.CollectionExpression = _txtCollectionExpression.Text;
            _parameter.ItemVariable = _txtItemVariable.Text;

            // 公共选项
            if (int.TryParse(_txtMaxIterations.Text, out int maxIter))
                _parameter.MaxIterations = maxIter;
            if (int.TryParse(_txtTimeout.Text, out int timeout))
                _parameter.TimeoutSeconds = timeout;
            _parameter.BreakOnError = _chkBreakOnError.Checked;
            _parameter.IsEnabled = _chkEnabled.Checked;
        }

        protected override ParameterValidationResult OnValidate()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (_rbCountLoop.Checked)
            {
                // 验证计数循环参数
                if (string.IsNullOrWhiteSpace(_txtStartValue.Text))
                    errors.Add("请设置起始值");
                if (string.IsNullOrWhiteSpace(_txtEndValue.Text))
                    errors.Add("请设置结束值");
                if (string.IsNullOrWhiteSpace(_txtCounterVariable.Text))
                    errors.Add("请设置计数器变量");

                // 验证步进值不能为0
                if (_txtStepValue.Text == "0")
                    errors.Add("步进值不能为0");
            }
            else if (_rbConditionLoop.Checked)
            {
                // 验证条件循环参数
                if (string.IsNullOrWhiteSpace(_txtConditionExpression.Text))
                    errors.Add("请设置循环条件");
            }
            else if (_rbForEachLoop.Checked)
            {
                // 验证集合遍历参数
                if (string.IsNullOrWhiteSpace(_txtCollectionExpression.Text))
                    errors.Add("请设置集合表达式");
                if (string.IsNullOrWhiteSpace(_txtItemVariable.Text))
                    errors.Add("请设置当前项变量");
            }

            // 验证最大迭代次数
            if (!string.IsNullOrWhiteSpace(_txtMaxIterations.Text))
            {
                if (!int.TryParse(_txtMaxIterations.Text, out int maxIter) || maxIter <= 0)
                    errors.Add("最大迭代次数必须是正整数");
                else if (maxIter > 100000)
                    warnings.Add("最大迭代次数设置过大，可能影响性能");
            }

            // 验证超时时间
            if (!string.IsNullOrWhiteSpace(_txtTimeout.Text))
            {
                if (!int.TryParse(_txtTimeout.Text, out int timeout) || timeout <= 0)
                    errors.Add("超时时间必须是正整数");
            }

            if (errors.Count > 0)
            {
                return ParameterValidationResult.Failed(string.Join("\n", errors));
            }

            var result = ParameterValidationResult.Success();
            foreach (var warning in warnings)
            {
                result.Warnings.Add(warning);
            }
            return result;
        }

        protected override void OnResetToDefault()
        {
            _parameter = new LoopParameter();
            LoadParameterToForm();
        }

        #endregion
    }

    #region 参数类

    /// <summary>
    /// 循环类型
    /// </summary>
    public enum LoopType
    {
        /// <summary>
        /// 计数循环 (for i = start to end step step)
        /// </summary>
        Count,

        /// <summary>
        /// 条件循环 (while condition)
        /// </summary>
        Condition,

        /// <summary>
        /// 集合遍历 (foreach item in collection)
        /// </summary>
        ForEach
    }

    /// <summary>
    /// 循环参数
    /// </summary>
    public class LoopParameter
    {
        /// <summary>
        /// 循环类型
        /// </summary>
        public LoopType LoopType { get; set; } = LoopType.Count;

        #region 计数循环参数

        /// <summary>
        /// 起始值表达式
        /// </summary>
        public string StartValue { get; set; } = "0";

        /// <summary>
        /// 结束值表达式
        /// </summary>
        public string EndValue { get; set; } = "10";

        /// <summary>
        /// 步进值表达式
        /// </summary>
        public string StepValue { get; set; } = "1";

        /// <summary>
        /// 计数器变量名
        /// </summary>
        public string CounterVariable { get; set; } = "i";

        #endregion

        #region 条件循环参数

        /// <summary>
        /// 循环条件表达式
        /// </summary>
        public string ConditionExpression { get; set; }

        /// <summary>
        /// 是否在循环开始前检查条件
        /// </summary>
        public bool PreCheck { get; set; } = true;

        #endregion

        #region 集合遍历参数

        /// <summary>
        /// 集合表达式
        /// </summary>
        public string CollectionExpression { get; set; }

        /// <summary>
        /// 当前项变量名
        /// </summary>
        public string ItemVariable { get; set; } = "item";

        #endregion

        #region 公共选项

        /// <summary>
        /// 最大迭代次数（防止死循环）
        /// </summary>
        public int MaxIterations { get; set; } = 1000;

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 错误时是否中断循环
        /// </summary>
        public bool BreakOnError { get; set; } = true;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 子步骤列表
        /// </summary>
        public List<object> ChildSteps { get; set; } = new();

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        #endregion
    }

    #endregion
}
