using AntdUI;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Core.Domain.Parameters;
using MainUI.UniversalPlatform.UI.Common.Controls;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.PLC
{
    /// <summary>
    /// PLC读取配置表单
    /// 支持从PLC地址读取数据到变量
    /// </summary>
    public partial class PLCReadConfigForm : BaseParameterForm
    {
        #region 属性

        public override string StepType => "读取PLC";

        private PLCReadParameter _parameter;
        public PLCReadParameter Parameter
        {
            get => _parameter;
            set
            {
                _parameter = value ?? new PLCReadParameter();
                if (!DesignMode && !IsLoading && IsHandleCreated)
                {
                    LoadParameterToForm();
                }
            }
        }

        #endregion

        #region 控件声明

        private Panel _mainPanel;
        private ComboBox _cmbModule;
        private UITextBox _txtAddress;
        private ComboBox _cmbDataType;
        private UITextBox _txtLength;
        private UITextBox _txtTargetVariable;
        private UITextBox _txtTimeout;
        private CheckBox _chkRetryOnError;
        private UITextBox _txtRetryCount;
        private UITextBox _txtRetryDelay;
        private CheckBox _chkEnabled;
        private DataGridView _dgvBatchRead;
        private CheckBox _chkBatchMode;
        private Panel _singleReadPanel;
        private Panel _batchReadPanel;

        #endregion

        #region 构造函数

        public PLCReadConfigForm()
        {
            InitializeComponent();
            if (!DesignMode) InitializeForm();
        }

        public PLCReadConfigForm(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            InitializeComponent();
            InitializeForm();
        }

        #endregion

        #region 初始化

        private void InitializeForm()
        {
            Size = new Size(600, 650);
            CreateControls();
            BindEvents();
            LoadPLCModules();
        }

        private void CreateControls()
        {
            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true
            };
            Controls.Add(_mainPanel);

            int yPos = 20;

            // 批量模式切换
            _chkBatchMode = new CheckBox
            {
                Text = "批量读取模式",
                Location = new Point(20, yPos),
                Size = new Size(150, 24),
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            _mainPanel.Controls.Add(_chkBatchMode);
            yPos += 35;

            // 单个读取面板
            CreateSingleReadPanel(ref yPos);

            // 批量读取面板
            CreateBatchReadPanel(ref yPos);

            // 公共选项
            CreateCommonOptionsPanel(ref yPos);

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

        private void CreateSingleReadPanel(ref int yPos)
        {
            _singleReadPanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(540, 180),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = "PLC读取设置",
                Location = new Point(10, 10),
                Size = new Size(520, 20),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _singleReadPanel.Controls.Add(lblTitle);

            // 模块选择
            var lblModule = new Label { Text = "PLC模块:", Location = new Point(20, 45), Size = new Size(80, 24) };
            _cmbModule = new ComboBox
            {
                Location = new Point(105, 40),
                Size = new Size(150, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _singleReadPanel.Controls.Add(lblModule);
            _singleReadPanel.Controls.Add(_cmbModule);

            // 地址输入
            var lblAddress = new Label { Text = "PLC地址:", Location = new Point(280, 45), Size = new Size(80, 24) };
            _txtAddress = new UITextBox
            {
                Location = new Point(365, 40),
                Size = new Size(150, 32),
                Watermark = "例: D100"
            };
            _singleReadPanel.Controls.Add(lblAddress);
            _singleReadPanel.Controls.Add(_txtAddress);

            // 数据类型
            var lblDataType = new Label { Text = "数据类型:", Location = new Point(20, 85), Size = new Size(80, 24) };
            _cmbDataType = new ComboBox
            {
                Location = new Point(105, 80),
                Size = new Size(150, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbDataType.Items.AddRange(new object[] { "Bool", "Int16", "Int32", "Int64", "Float", "Double", "String" });
            _cmbDataType.SelectedIndex = 2; // Int32
            _singleReadPanel.Controls.Add(lblDataType);
            _singleReadPanel.Controls.Add(_cmbDataType);

            // 读取长度（字符串类型时使用）
            var lblLength = new Label { Text = "长度:", Location = new Point(280, 85), Size = new Size(80, 24) };
            _txtLength = new UITextBox
            {
                Location = new Point(365, 80),
                Size = new Size(80, 32),
                Watermark = "1",
                Enabled = false
            };
            _singleReadPanel.Controls.Add(lblLength);
            _singleReadPanel.Controls.Add(_txtLength);

            // 目标变量
            var lblTarget = new Label { Text = "保存到变量:", Location = new Point(20, 125), Size = new Size(80, 24) };
            _txtTargetVariable = new UITextBox
            {
                Location = new Point(105, 120),
                Size = new Size(200, 32),
                Watermark = "选择或输入变量名"
            };
            _singleReadPanel.Controls.Add(lblTarget);
            _singleReadPanel.Controls.Add(_txtTargetVariable);

            _mainPanel.Controls.Add(_singleReadPanel);
            yPos += 190;
        }

        private void CreateBatchReadPanel(ref int yPos)
        {
            _batchReadPanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(540, 200),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            var lblTitle = new Label
            {
                Text = "批量读取配置",
                Location = new Point(10, 10),
                Size = new Size(520, 20),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _batchReadPanel.Controls.Add(lblTitle);

            // 批量读取表格
            _dgvBatchRead = new DataGridView
            {
                Location = new Point(20, 40),
                Size = new Size(500, 120),
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 添加列
            _dgvBatchRead.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "Module",
                HeaderText = "PLC模块",
                Width = 100
            });
            _dgvBatchRead.Columns.Add("Address", "地址");
            _dgvBatchRead.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "DataType",
                HeaderText = "类型",
                Items = { "Bool", "Int16", "Int32", "Int64", "Float", "Double", "String" },
                Width = 80
            });
            _dgvBatchRead.Columns.Add("Variable", "保存变量");

            _batchReadPanel.Controls.Add(_dgvBatchRead);

            // 操作按钮
            var btnAdd = new AntdUI.Button
            {
                Text = "添加",
                Location = new Point(20, 165),
                Size = new Size(70, 28),
                Type = TTypeMini.Primary
            };
            btnAdd.Click += (s, e) => _dgvBatchRead.Rows.Add();
            _batchReadPanel.Controls.Add(btnAdd);

            var btnDelete = new AntdUI.Button
            {
                Text = "删除",
                Location = new Point(100, 165),
                Size = new Size(70, 28)
            };
            btnDelete.Click += (s, e) =>
            {
                if (_dgvBatchRead.SelectedRows.Count > 0 && !_dgvBatchRead.SelectedRows[0].IsNewRow)
                    _dgvBatchRead.Rows.Remove(_dgvBatchRead.SelectedRows[0]);
            };
            _batchReadPanel.Controls.Add(btnDelete);

            _mainPanel.Controls.Add(_batchReadPanel);
        }

        private void CreateCommonOptionsPanel(ref int yPos)
        {
            var optionsPanel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(540, 130),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = "读取选项",
                Location = new Point(10, 10),
                Size = new Size(520, 20),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            optionsPanel.Controls.Add(lblTitle);

            // 超时时间
            var lblTimeout = new Label { Text = "超时时间(ms):", Location = new Point(20, 45), Size = new Size(100, 24) };
            _txtTimeout = new UITextBox
            {
                Location = new Point(125, 40),
                Size = new Size(100, 32),
                Watermark = "5000"
            };
            optionsPanel.Controls.Add(lblTimeout);
            optionsPanel.Controls.Add(_txtTimeout);

            // 重试选项
            _chkRetryOnError = new CheckBox
            {
                Text = "错误时重试",
                Location = new Point(260, 45),
                Size = new Size(100, 24)
            };
            optionsPanel.Controls.Add(_chkRetryOnError);

            // 重试次数
            var lblRetryCount = new Label { Text = "重试次数:", Location = new Point(20, 85), Size = new Size(70, 24) };
            _txtRetryCount = new UITextBox
            {
                Location = new Point(95, 80),
                Size = new Size(80, 32),
                Watermark = "3",
                Enabled = false
            };
            optionsPanel.Controls.Add(lblRetryCount);
            optionsPanel.Controls.Add(_txtRetryCount);

            // 重试间隔
            var lblRetryDelay = new Label { Text = "重试间隔(ms):", Location = new Point(200, 85), Size = new Size(100, 24) };
            _txtRetryDelay = new UITextBox
            {
                Location = new Point(305, 80),
                Size = new Size(80, 32),
                Watermark = "1000",
                Enabled = false
            };
            optionsPanel.Controls.Add(lblRetryDelay);
            optionsPanel.Controls.Add(_txtRetryDelay);

            _mainPanel.Controls.Add(optionsPanel);
            yPos += 140;
        }

        private void BindEvents()
        {
            _chkBatchMode.CheckedChanged += BatchModeChanged;
            _cmbDataType.SelectedIndexChanged += DataTypeChanged;
            _chkRetryOnError.CheckedChanged += RetryOnErrorChanged;

            // 绑定修改事件
            _cmbModule.SelectedIndexChanged += (s, e) => MarkAsModified();
            _txtAddress.TextChanged += (s, e) => MarkAsModified();
            _cmbDataType.SelectedIndexChanged += (s, e) => MarkAsModified();
            _txtLength.TextChanged += (s, e) => MarkAsModified();
            _txtTargetVariable.TextChanged += (s, e) => MarkAsModified();
            _txtTimeout.TextChanged += (s, e) => MarkAsModified();
            _txtRetryCount.TextChanged += (s, e) => MarkAsModified();
            _txtRetryDelay.TextChanged += (s, e) => MarkAsModified();
            _chkEnabled.CheckedChanged += (s, e) => MarkAsModified();
        }

        private void BatchModeChanged(object sender, EventArgs e)
        {
            _singleReadPanel.Visible = !_chkBatchMode.Checked;
            _batchReadPanel.Visible = _chkBatchMode.Checked;
            MarkAsModified();
        }

        private void DataTypeChanged(object sender, EventArgs e)
        {
            // 字符串类型时启用长度输入
            _txtLength.Enabled = _cmbDataType.SelectedItem?.ToString() == "String";
        }

        private void RetryOnErrorChanged(object sender, EventArgs e)
        {
            _txtRetryCount.Enabled = _chkRetryOnError.Checked;
            _txtRetryDelay.Enabled = _chkRetryOnError.Checked;
        }

        private void LoadPLCModules()
        {
            _cmbModule.Items.Clear();
            // 这里应该从 PLCAdapter 加载可用的模块列表
            // 暂时添加示例数据
            _cmbModule.Items.AddRange(["PLC1", "PLC2", "IO模块"]);
            if (_cmbModule.Items.Count > 0)
                _cmbModule.SelectedIndex = 0;

            // 同步到批量读取表格
            if (_dgvBatchRead.Columns["Module"] is DataGridViewComboBoxColumn moduleColumn)
            {
                moduleColumn.Items.Clear();
                foreach (var item in _cmbModule.Items)
                {
                    moduleColumn.Items.Add(item);
                }
            }
        }

        #endregion

        #region 表达式输入初始化

        protected override void InitializeExpressionInputs()
        {
            // PLC地址选择
            _txtAddress.WithPLCInput();

            // 目标变量选择
            _txtTargetVariable.WithVariableInput();

            // 数值表达式
            _txtTimeout.WithExpressionInput(options =>
            {
                options.Mode = InputMode.Numeric;
                options.EnabledModules = InputModules.Variable | InputModules.Constant;
                options.Title = "设置超时时间";
            });
        }

        #endregion

        #region 参数操作

        protected override void OnSetParameter(object parameter)
        {
            _parameter = ConvertParameter<PLCReadParameter>(parameter);
        }

        protected override object OnGetParameter()
        {
            return _parameter;
        }

        protected override void LoadParameterToForm()
        {
            if (_parameter == null)
            {
                _parameter = new PLCReadParameter();
            }

            try
            {
                IsLoading = true;

                _chkBatchMode.Checked = _parameter.BatchMode;

                // 单个读取参数
                if (_cmbModule.Items.Contains(_parameter.ModuleName))
                    _cmbModule.SelectedItem = _parameter.ModuleName;
                _txtAddress.Text = _parameter.Address;
                _cmbDataType.SelectedItem = _parameter.DataType;
                _txtLength.Text = _parameter.Length.ToString();
                _txtTargetVariable.Text = _parameter.TargetVariable;

                // 批量读取参数
                _dgvBatchRead.Rows.Clear();
                foreach (var item in _parameter.BatchItems)
                {
                    _dgvBatchRead.Rows.Add(item.ModuleName, item.Address, item.DataType, item.TargetVariable);
                }

                // 公共选项
                _txtTimeout.Text = _parameter.TimeoutMs.ToString();
                _chkRetryOnError.Checked = _parameter.RetryOnError;
                _txtRetryCount.Text = _parameter.RetryCount.ToString();
                _txtRetryDelay.Text = _parameter.RetryDelayMs.ToString();
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
            _parameter.BatchMode = _chkBatchMode.Checked;

            // 单个读取参数
            _parameter.ModuleName = _cmbModule.SelectedItem?.ToString() ?? "";
            _parameter.Address = _txtAddress.Text;
            _parameter.DataType = _cmbDataType.SelectedItem?.ToString() ?? "Int32";
            if (int.TryParse(_txtLength.Text, out int length))
                _parameter.Length = length;
            _parameter.TargetVariable = _txtTargetVariable.Text;

            // 批量读取参数
            _parameter.BatchItems.Clear();
            foreach (DataGridViewRow row in _dgvBatchRead.Rows)
            {
                if (row.IsNewRow) continue;

                _parameter.BatchItems.Add(new PLCReadItem
                {
                    ModuleName = row.Cells["Module"].Value?.ToString(),
                    Address = row.Cells["Address"].Value?.ToString(),
                    DataType = row.Cells["DataType"].Value?.ToString() ?? "Int32",
                    TargetVariable = row.Cells["Variable"].Value?.ToString()
                });
            }

            // 公共选项
            if (int.TryParse(_txtTimeout.Text, out int timeout))
                _parameter.TimeoutMs = timeout;
            _parameter.RetryOnError = _chkRetryOnError.Checked;
            if (int.TryParse(_txtRetryCount.Text, out int retryCount))
                _parameter.RetryCount = retryCount;
            if (int.TryParse(_txtRetryDelay.Text, out int retryDelay))
                _parameter.RetryDelayMs = retryDelay;
            _parameter.IsEnabled = _chkEnabled.Checked;
        }

        protected override ParameterValidationResult OnValidate()
        {
            var errors = new List<string>();

            if (_chkBatchMode.Checked)
            {
                // 验证批量读取
                int validRows = 0;
                foreach (DataGridViewRow row in _dgvBatchRead.Rows)
                {
                    if (row.IsNewRow) continue;
                    validRows++;

                    if (string.IsNullOrWhiteSpace(row.Cells["Address"].Value?.ToString()))
                        errors.Add($"第{row.Index + 1}行：请设置PLC地址");
                    if (string.IsNullOrWhiteSpace(row.Cells["Variable"].Value?.ToString()))
                        errors.Add($"第{row.Index + 1}行：请设置保存变量");
                }

                if (validRows == 0)
                    errors.Add("批量读取模式至少需要配置一项");
            }
            else
            {
                // 验证单个读取
                if (string.IsNullOrWhiteSpace(_txtAddress.Text))
                    errors.Add("请设置PLC地址");
                if (string.IsNullOrWhiteSpace(_txtTargetVariable.Text))
                    errors.Add("请设置保存变量");
            }

            // 验证超时时间
            if (!string.IsNullOrWhiteSpace(_txtTimeout.Text))
            {
                if (!int.TryParse(_txtTimeout.Text, out int timeout) || timeout < 100)
                    errors.Add("超时时间必须大于100毫秒");
            }

            if (errors.Count > 0)
            {
                return ParameterValidationResult.Failed(string.Join("\n", errors));
            }

            return ParameterValidationResult.Success();
        }

        protected override void OnResetToDefault()
        {
            _parameter = new PLCReadParameter();
            LoadParameterToForm();
        }

        #endregion
    }

    #region 参数类

    /// <summary>
    /// PLC读取参数
    /// </summary>
    public class PLCReadParameter
    {
        /// <summary>
        /// 是否批量模式
        /// </summary>
        public bool BatchMode { get; set; }

        #region 单个读取参数

        /// <summary>
        /// PLC模块名称
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// PLC地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; } = "Int32";

        /// <summary>
        /// 读取长度（字符串类型时使用）
        /// </summary>
        public int Length { get; set; } = 1;

        /// <summary>
        /// 目标变量名
        /// </summary>
        public string TargetVariable { get; set; }

        #endregion

        #region 批量读取参数

        /// <summary>
        /// 批量读取项
        /// </summary>
        public List<PLCReadItem> BatchItems { get; set; } = new();

        #endregion

        #region 公共选项

        /// <summary>
        /// 超时时间（毫秒）
        /// </summary>
        public int TimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 错误时重试
        /// </summary>
        public bool RetryOnError { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// 重试间隔（毫秒）
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        #endregion
    }

    /// <summary>
    /// PLC读取项（批量读取时使用）
    /// </summary>
    public class PLCReadItem
    {
        public string ModuleName { get; set; }
        public string Address { get; set; }
        public string DataType { get; set; } = "Int32";
        public int Length { get; set; } = 1;
        public string TargetVariable { get; set; }
    }

    #endregion
}
