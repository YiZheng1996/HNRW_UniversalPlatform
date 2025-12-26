using AntdUI;
using MainUI.UniversalPlatform.Core.Domain.Parameters;
using MainUI.UniversalPlatform.UI.Common.Controls;
using MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Windows.Forms;
using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.Logic
{
    /// <summary>
    /// 延时等待参数
    /// </summary>
    public class DelayParameter
    {
        /// <summary>
        /// 延时类型
        /// </summary>
        public DelayType DelayType { get; set; } = DelayType.Fixed;

        /// <summary>
        /// 固定延时时间（毫秒）
        /// </summary>
        public int DelayMs { get; set; } = 1000;

        /// <summary>
        /// 延时表达式（动态延时时使用）
        /// </summary>
        public string DelayExpression { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// 延时类型
    /// </summary>
    public enum DelayType
    {
        /// <summary>固定延时</summary>
        Fixed,
        /// <summary>动态延时（表达式）</summary>
        Dynamic,
        /// <summary>随机延时</summary>
        Random
    }

    /// <summary>
    /// 延时等待配置表单
    /// 演示如何继承 BaseParameterForm 并使用 ExpressionInputPanel
    /// </summary>
    [StepForm("延时等待", Aliases = new[] { "Delay", "DelayWait" })]
    public partial class DelayConfigForm : BaseParameterForm, IParameterForm<DelayParameter>
    {
        #region 私有字段

        private DelayParameter _parameter;

        // UI 控件
        private RadioButton _rbFixed;
        private RadioButton _rbDynamic;
        private RadioButton _rbRandom;
        private NumericUpDown _numDelayMs;
        private UITextBox _txtDelayExpression;
        private NumericUpDown _numMinDelay;
        private NumericUpDown _numMaxDelay;
        private UITextBox _txtDescription;
        private CheckBox _chkEnabled;
        private Panel _panelFixed;
        private Panel _panelDynamic;
        private Panel _panelRandom;

        #endregion

        #region 属性

        /// <summary>
        /// 步骤类型
        /// </summary>
        public override string StepType => "延时等待";

        /// <summary>
        /// 强类型参数
        /// </summary>
        public new DelayParameter ResultParameter => _parameter;

        #endregion

        #region 构造函数

        public DelayConfigForm()
        {
            InitializeFormControls();
        }

        public DelayConfigForm(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            InitializeFormControls();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化表单控件
        /// </summary>
        private void InitializeFormControls()
        {
            SuspendLayout();

            // 设置窗体基本属性
            Size = new Size(500, 450);
            Text = "延时等待 - 参数配置";

            // 创建主面板
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 20, 20, 10),
                AutoScroll = true
            };

            int yPos = 10;

            // 延时类型选择组
            var typeGroupBox = CreateDelayTypeGroup(ref yPos);
            mainPanel.Controls.Add(typeGroupBox);

            // 固定延时面板
            _panelFixed = CreateFixedDelayPanel(ref yPos);
            mainPanel.Controls.Add(_panelFixed);

            // 动态延时面板
            _panelDynamic = CreateDynamicDelayPanel(ref yPos);
            mainPanel.Controls.Add(_panelDynamic);

            // 随机延时面板
            _panelRandom = CreateRandomDelayPanel(ref yPos);
            mainPanel.Controls.Add(_panelRandom);

            // 描述和启用
            CreateDescriptionPanel(mainPanel, ref yPos);

            Controls.Add(mainPanel);

            ResumeLayout(false);

            // 初始化默认状态
            _rbFixed.Checked = true;
            UpdatePanelVisibility();
        }

        /// <summary>
        /// 创建延时类型选择组
        /// </summary>
        private GroupBox CreateDelayTypeGroup(ref int yPos)
        {
            var groupBox = new GroupBox
            {
                Text = "延时类型",
                Location = new Point(10, yPos),
                Size = new Size(440, 70),
                Font = new Font("微软雅黑", 9)
            };

            _rbFixed = new RadioButton
            {
                Text = "固定延时",
                Location = new Point(20, 25),
                AutoSize = true
            };
            _rbFixed.CheckedChanged += (s, e) => { if (_rbFixed.Checked) UpdatePanelVisibility(); MarkAsModified(); };

            _rbDynamic = new RadioButton
            {
                Text = "动态延时",
                Location = new Point(140, 25),
                AutoSize = true
            };
            _rbDynamic.CheckedChanged += (s, e) => { if (_rbDynamic.Checked) UpdatePanelVisibility(); MarkAsModified(); };

            _rbRandom = new RadioButton
            {
                Text = "随机延时",
                Location = new Point(260, 25),
                AutoSize = true
            };
            _rbRandom.CheckedChanged += (s, e) => { if (_rbRandom.Checked) UpdatePanelVisibility(); MarkAsModified(); };

            groupBox.Controls.AddRange(new Control[] { _rbFixed, _rbDynamic, _rbRandom });

            yPos += 80;
            return groupBox;
        }

        /// <summary>
        /// 创建固定延时面板
        /// </summary>
        private Panel CreateFixedDelayPanel(ref int yPos)
        {
            var panel = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(440, 70),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblDelay = new Label
            {
                Text = "延时时间 (毫秒):",
                Location = new Point(15, 25),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };

            _numDelayMs = new NumericUpDown
            {
                Location = new Point(130, 22),
                Size = new Size(120, 28),
                Minimum = 0,
                Maximum = 3600000,  // 最大 1 小时
                Value = 1000,
                Increment = 100,
                Font = new Font("微软雅黑", 10)
            };
            _numDelayMs.ValueChanged += (s, e) => MarkAsModified();

            var lblMs = new Label
            {
                Text = "ms",
                Location = new Point(255, 25),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };

            // 快捷按钮
            var quickPanel = new FlowLayoutPanel
            {
                Location = new Point(290, 18),
                Size = new Size(140, 35),
                FlowDirection = FlowDirection.LeftToRight
            };

            var quickValues = new[] { 500, 1000, 2000, 5000 };
            foreach (var value in quickValues)
            {
                var btn = new AntdUI.Button
                {
                    Text = $"{value / 1000.0}s",
                    Size = new Size(32, 25),
                    Margin = new Padding(1)
                };
                btn.Click += (s, e) => { _numDelayMs.Value = value; };
                quickPanel.Controls.Add(btn);
            }

            panel.Controls.AddRange(new Control[] { lblDelay, _numDelayMs, lblMs, quickPanel });

            yPos += 80;
            return panel;
        }

        /// <summary>
        /// 创建动态延时面板
        /// </summary>
        private Panel CreateDynamicDelayPanel(ref int yPos)
        {
            var panel = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(440, 90),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            var lblExpr = new Label
            {
                Text = "延时表达式:",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };

            _txtDelayExpression = new UITextBox
            {
                Location = new Point(15, 38),
                Size = new Size(410, 36),
                Watermark = "输入表达式，返回毫秒数，例如: {延时变量} * 1000"
            };
            _txtDelayExpression.TextChanged += (s, e) => MarkAsModified();

            var lblHint = new Label
            {
                Text = "提示: 点击输入框打开表达式编辑器",
                Location = new Point(15, 72),
                ForeColor = Color.Gray,
                AutoSize = true,
                Font = new Font("微软雅黑", 8)
            };

            panel.Controls.AddRange(new Control[] { lblExpr, _txtDelayExpression, lblHint });

            return panel;
        }

        /// <summary>
        /// 创建随机延时面板
        /// </summary>
        private Panel CreateRandomDelayPanel(ref int yPos)
        {
            var panel = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(440, 70),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            var lblMin = new Label
            {
                Text = "最小延时:",
                Location = new Point(15, 25),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };

            _numMinDelay = new NumericUpDown
            {
                Location = new Point(90, 22),
                Size = new Size(100, 28),
                Minimum = 0,
                Maximum = 3600000,
                Value = 500,
                Font = new Font("微软雅黑", 10)
            };
            _numMinDelay.ValueChanged += (s, e) => MarkAsModified();

            var lblMax = new Label
            {
                Text = "最大延时:",
                Location = new Point(220, 25),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };

            _numMaxDelay = new NumericUpDown
            {
                Location = new Point(295, 22),
                Size = new Size(100, 28),
                Minimum = 0,
                Maximum = 3600000,
                Value = 2000,
                Font = new Font("微软雅黑", 10)
            };
            _numMaxDelay.ValueChanged += (s, e) => MarkAsModified();

            var lblMs = new Label
            {
                Text = "ms",
                Location = new Point(400, 25),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };

            panel.Controls.AddRange(new Control[] { lblMin, _numMinDelay, lblMax, _numMaxDelay, lblMs });

            return panel;
        }

        /// <summary>
        /// 创建描述面板
        /// </summary>
        private void CreateDescriptionPanel(System.Windows.Forms.Panel parent, ref int yPos)
        {
            yPos += 100;

            var lblDesc = new Label
            {
                Text = "描述:",
                Location = new Point(10, yPos),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };
            parent.Controls.Add(lblDesc);

            _txtDescription = new UITextBox
            {
                Location = new Point(10, yPos + 22),
                Size = new Size(440, 36),
                Watermark = "输入步骤描述（可选）"
            };
            _txtDescription.TextChanged += (s, e) => MarkAsModified();
            parent.Controls.Add(_txtDescription);

            yPos += 70;

            _chkEnabled = new CheckBox
            {
                Text = "启用此步骤",
                Location = new Point(10, yPos),
                Checked = true,
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };
            _chkEnabled.CheckedChanged += (s, e) => MarkAsModified();
            parent.Controls.Add(_chkEnabled);
        }

        #endregion

        #region 表达式输入配置

        /// <summary>
        /// 初始化表达式输入面板 - 重写基类方法
        /// ⭐ 这是使用 ExpressionInputPanel 的关键代码
        /// </summary>
        protected override void InitializeExpressionInputs()
        {
            // 为动态延时表达式输入框附加表达式面板
            // 使用自定义配置
            _txtDelayExpression.WithExpressionInput(options =>
            {
                options.Mode = InputMode.Numeric;
                options.EnabledModules = InputModules.Variable | InputModules.Expression | InputModules.Constant;
                options.Title = "配置延时表达式";
                options.InitialExpression = "输入返回毫秒数的表达式";
                options.ShowValidation = true;
                options.ShowPreview = true;

                // 自定义验证
                options.CustomValidator = expression =>
                {
                    if (string.IsNullOrWhiteSpace(expression))
                    {
                        return ValidationInfo.Invalid("表达式不能为空");
                    }

                    // 检查是否包含变量引用
                    if (!expression.Contains("{") && !double.TryParse(expression, out _))
                    {
                        return ValidationInfo.Invalid("请输入数值或包含变量的表达式");
                    }

                    return ValidationInfo.Valid("表达式有效");
                };
            });

            Logger?.LogDebug("DelayConfigForm 表达式输入面板已配置");
        }

        #endregion

        #region 参数处理

        /// <summary>
        /// 设置参数
        /// </summary>
        public void SetParameter(DelayParameter parameter)
        {
            _parameter = parameter ?? new DelayParameter();
            LoadParameterToForm();
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        public new DelayParameter GetParameter()
        {
            SaveFormToParameter();
            return _parameter;
        }

        /// <summary>
        /// 设置参数（重写基类）
        /// </summary>
        protected override void OnSetParameter(object parameter)
        {
            _parameter = ConvertParameter<DelayParameter>(parameter) ?? new DelayParameter();
        }

        /// <summary>
        /// 获取参数（重写基类）
        /// </summary>
        protected override object OnGetParameter()
        {
            return _parameter;
        }

        /// <summary>
        /// 加载参数到界面
        /// </summary>
        protected override void LoadParameterToForm()
        {
            if (_parameter == null)
            {
                _parameter = new DelayParameter();
            }

            try
            {
                IsLoading = true;

                // 延时类型
                switch (_parameter.DelayType)
                {
                    case DelayType.Fixed:
                        _rbFixed.Checked = true;
                        break;
                    case DelayType.Dynamic:
                        _rbDynamic.Checked = true;
                        break;
                    case DelayType.Random:
                        _rbRandom.Checked = true;
                        break;
                }

                // 固定延时
                _numDelayMs.Value = Math.Clamp(_parameter.DelayMs, 0, 3600000);

                // 动态延时表达式
                _txtDelayExpression.Text = _parameter.DelayExpression ?? "";

                // 描述和启用
                _txtDescription.Text = _parameter.Description ?? "";
                _chkEnabled.Checked = _parameter.IsEnabled;

                UpdatePanelVisibility();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 从界面保存到参数
        /// </summary>
        protected override void SaveFormToParameter()
        {
            _parameter ??= new DelayParameter();

            // 延时类型
            if (_rbFixed.Checked)
                _parameter.DelayType = DelayType.Fixed;
            else if (_rbDynamic.Checked)
                _parameter.DelayType = DelayType.Dynamic;
            else if (_rbRandom.Checked)
                _parameter.DelayType = DelayType.Random;

            // 固定延时
            _parameter.DelayMs = (int)_numDelayMs.Value;

            // 动态延时表达式
            _parameter.DelayExpression = _txtDelayExpression.Text?.Trim();

            // 描述和启用
            _parameter.Description = _txtDescription.Text?.Trim();
            _parameter.IsEnabled = _chkEnabled.Checked;
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        protected override void OnResetToDefault()
        {
            _parameter = new DelayParameter();
        }

        #endregion

        #region 验证

        /// <summary>
        /// 验证输入
        /// </summary>
        protected override ParameterValidationResult OnValidate()
        {
            var errors = new List<string>();

            if (_rbDynamic.Checked)
            {
                if (string.IsNullOrWhiteSpace(_txtDelayExpression.Text))
                {
                    errors.Add("请输入延时表达式");
                }
            }
            else if (_rbRandom.Checked)
            {
                if (_numMinDelay.Value >= _numMaxDelay.Value)
                {
                    errors.Add("最大延时必须大于最小延时");
                }
            }

            if (errors.Count > 0)
            {
                return ParameterValidationResult.Invalid(errors.ToArray());
            }

            return ParameterValidationResult.Valid();
        }

        #endregion

        #region UI 更新

        /// <summary>
        /// 更新面板可见性
        /// </summary>
        private void UpdatePanelVisibility()
        {
            _panelFixed.Visible = _rbFixed.Checked;
            _panelDynamic.Visible = _rbDynamic.Checked;
            _panelRandom.Visible = _rbRandom.Checked;

            // 调整面板位置
            var visiblePanel = _rbFixed.Checked ? _panelFixed :
                               _rbDynamic.Checked ? _panelDynamic :
                               _panelRandom;

            visiblePanel.Location = new Point(10, 90);
        }

        #endregion
    }
}
