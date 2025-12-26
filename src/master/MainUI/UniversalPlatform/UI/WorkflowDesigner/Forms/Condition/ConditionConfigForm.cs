using AntdUI;
using MainUI.UniversalPlatform.Core.Domain.Parameters;
using MainUI.UniversalPlatform.UI.Common.Controls;
using MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.Condition
{
    /// <summary>
    /// 条件判断参数
    /// </summary>
    public class ConditionParameter
    {
        /// <summary>
        /// 条件表达式
        /// </summary>
        public string ConditionExpression { get; set; }

        /// <summary>
        /// 条件为真时的跳转目标
        /// </summary>
        public ConditionAction TrueAction { get; set; } = new();

        /// <summary>
        /// 条件为假时的跳转目标
        /// </summary>
        public ConditionAction FalseAction { get; set; } = new();

        /// <summary>
        /// 是否将结果保存到变量
        /// </summary>
        public bool SaveResultToVariable { get; set; }

        /// <summary>
        /// 结果变量名
        /// </summary>
        public string ResultVariableName { get; set; }

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
    /// 条件动作
    /// </summary>
    public class ConditionAction
    {
        /// <summary>
        /// 动作类型
        /// </summary>
        public ConditionActionType ActionType { get; set; } = ConditionActionType.Continue;

        /// <summary>
        /// 跳转目标步骤号
        /// </summary>
        public int? JumpToStep { get; set; }

        /// <summary>
        /// 要执行的表达式
        /// </summary>
        public string ExecuteExpression { get; set; }
    }

    /// <summary>
    /// 条件动作类型
    /// </summary>
    public enum ConditionActionType
    {
        /// <summary>继续执行</summary>
        Continue,
        /// <summary>跳转到指定步骤</summary>
        JumpTo,
        /// <summary>停止流程</summary>
        Stop,
        /// <summary>执行表达式</summary>
        Execute
    }

    /// <summary>
    /// 条件判断配置表单
    /// 演示复杂表达式输入场景
    /// </summary>
    [StepForm("条件判断", Aliases = new[] { "Condition", "ConditionJudge" })]
    public partial class ConditionConfigForm : BaseParameterForm, IParameterForm<ConditionParameter>
    {
        #region 私有字段

        private ConditionParameter _parameter;

        // 条件表达式
        private UITextBox _txtCondition;
        private Panel _conditionPreviewPanel;
        private Label _lblConditionPreview;

        // True 分支
        private GroupBox _grpTrueAction;
        private ComboBox _cmbTrueAction;
        private NumericUpDown _numTrueJumpStep;
        private UITextBox _txtTrueExpression;
        private Panel _panelTrueJump;
        private Panel _panelTrueExpression;

        // False 分支
        private GroupBox _grpFalseAction;
        private ComboBox _cmbFalseAction;
        private NumericUpDown _numFalseJumpStep;
        private UITextBox _txtFalseExpression;
        private Panel _panelFalseJump;
        private Panel _panelFalseExpression;

        // 结果保存
        private CheckBox _chkSaveResult;
        private UITextBox _txtResultVariable;

        // 其他
        private UITextBox _txtDescription;
        private CheckBox _chkEnabled;

        #endregion

        #region 属性

        public override string StepType => "条件判断";

        public new ConditionParameter ResultParameter => _parameter;

        #endregion

        #region 构造函数

        public ConditionConfigForm()
        {
            InitializeFormControls();
        }

        public ConditionConfigForm(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            InitializeFormControls();
        }

        #endregion

        #region 初始化

        private void InitializeFormControls()
        {
            SuspendLayout();

            Size = new Size(600, 650);
            Text = "条件判断 - 参数配置";

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true
            };

            int yPos = 10;

            // 条件表达式区域
            CreateConditionExpressionSection(mainPanel, ref yPos);

            // True 分支区域
            CreateTrueActionSection(mainPanel, ref yPos);

            // False 分支区域
            CreateFalseActionSection(mainPanel, ref yPos);

            // 结果保存区域
            CreateResultSaveSection(mainPanel, ref yPos);

            // 描述和启用
            CreateDescriptionSection(mainPanel, ref yPos);

            Controls.Add(mainPanel);

            ResumeLayout(false);
        }

        /// <summary>
        /// 创建条件表达式区域
        /// </summary>
        private void CreateConditionExpressionSection(Panel parent, ref int yPos)
        {
            var grpCondition = new GroupBox
            {
                Text = "条件表达式",
                Location = new Point(10, yPos),
                Size = new Size(540, 120),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            var lblCondition = new Label
            {
                Text = "判断条件 (返回 true/false):",
                Location = new Point(15, 25),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };

            _txtCondition = new UITextBox
            {
                Location = new Point(15, 48),
                Size = new Size(510, 36),
                Watermark = "例如: {变量A} > 100 && {变量B} == \"完成\"",
                Font = new Font("Consolas", 10)
            };
            _txtCondition.TextChanged += OnConditionChanged;

            // 条件预览面板
            _conditionPreviewPanel = new Panel
            {
                Location = new Point(15, 88),
                Size = new Size(510, 25),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            _lblConditionPreview = new Label
            {
                Text = "预览: ",
                Location = new Point(5, 4),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("微软雅黑", 8)
            };
            _conditionPreviewPanel.Controls.Add(_lblConditionPreview);

            grpCondition.Controls.AddRange(new Control[]
            {
                lblCondition, _txtCondition, _conditionPreviewPanel
            });

            parent.Controls.Add(grpCondition);
            yPos += 130;
        }

        /// <summary>
        /// 创建 True 分支区域
        /// </summary>
        private void CreateTrueActionSection(Panel parent, ref int yPos)
        {
            _grpTrueAction = new GroupBox
            {
                Text = "✓ 条件为真时",
                Location = new Point(10, yPos),
                Size = new Size(540, 100),
                Font = new Font("微软雅黑", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(82, 196, 26)
            };

            var lblAction = new Label
            {
                Text = "执行动作:",
                Location = new Point(15, 28),
                AutoSize = true,
                Font = new Font("微软雅黑", 9),
                ForeColor = Color.Black
            };

            _cmbTrueAction = new ComboBox
            {
                Location = new Point(90, 25),
                Size = new Size(150, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", 9)
            };
            _cmbTrueAction.Items.AddRange(new object[] { "继续执行", "跳转到步骤", "停止流程", "执行表达式" });
            _cmbTrueAction.SelectedIndex = 0;
            _cmbTrueAction.SelectedIndexChanged += (s, e) =>
            {
                UpdateTrueActionPanels();
                MarkAsModified();
            };

            // 跳转面板
            _panelTrueJump = new Panel
            {
                Location = new Point(250, 20),
                Size = new Size(200, 35),
                Visible = false
            };

            var lblJumpTo = new Label
            {
                Text = "步骤号:",
                Location = new Point(0, 8),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };

            _numTrueJumpStep = new NumericUpDown
            {
                Location = new Point(55, 3),
                Size = new Size(80, 28),
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                Font = new Font("微软雅黑", 9)
            };
            _numTrueJumpStep.ValueChanged += (s, e) => MarkAsModified();

            _panelTrueJump.Controls.AddRange(new Control[] { lblJumpTo, _numTrueJumpStep });

            // 执行表达式面板
            _panelTrueExpression = new Panel
            {
                Location = new Point(15, 55),
                Size = new Size(510, 40),
                Visible = false
            };

            _txtTrueExpression = new UITextBox
            {
                Location = new Point(0, 0),
                Size = new Size(510, 36),
                Watermark = "输入要执行的表达式"
            };
            _txtTrueExpression.TextChanged += (s, e) => MarkAsModified();

            _panelTrueExpression.Controls.Add(_txtTrueExpression);

            _grpTrueAction.Controls.AddRange(new Control[]
            {
                lblAction, _cmbTrueAction, _panelTrueJump, _panelTrueExpression
            });

            parent.Controls.Add(_grpTrueAction);
            yPos += 110;
        }

        /// <summary>
        /// 创建 False 分支区域
        /// </summary>
        private void CreateFalseActionSection(Panel parent, ref int yPos)
        {
            _grpFalseAction = new GroupBox
            {
                Text = "✗ 条件为假时",
                Location = new Point(10, yPos),
                Size = new Size(540, 100),
                Font = new Font("微软雅黑", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 77, 79)
            };

            var lblAction = new Label
            {
                Text = "执行动作:",
                Location = new Point(15, 28),
                AutoSize = true,
                Font = new Font("微软雅黑", 9),
                ForeColor = Color.Black
            };

            _cmbFalseAction = new ComboBox
            {
                Location = new Point(90, 25),
                Size = new Size(150, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", 9)
            };
            _cmbFalseAction.Items.AddRange(new object[] { "继续执行", "跳转到步骤", "停止流程", "执行表达式" });
            _cmbFalseAction.SelectedIndex = 0;
            _cmbFalseAction.SelectedIndexChanged += (s, e) =>
            {
                UpdateFalseActionPanels();
                MarkAsModified();
            };

            // 跳转面板
            _panelFalseJump = new Panel
            {
                Location = new Point(250, 20),
                Size = new Size(200, 35),
                Visible = false
            };

            var lblJumpTo = new Label
            {
                Text = "步骤号:",
                Location = new Point(0, 8),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };

            _numFalseJumpStep = new NumericUpDown
            {
                Location = new Point(55, 3),
                Size = new Size(80, 28),
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                Font = new Font("微软雅黑", 9)
            };
            _numFalseJumpStep.ValueChanged += (s, e) => MarkAsModified();

            _panelFalseJump.Controls.AddRange(new Control[] { lblJumpTo, _numFalseJumpStep });

            // 执行表达式面板
            _panelFalseExpression = new Panel
            {
                Location = new Point(15, 55),
                Size = new Size(510, 40),
                Visible = false
            };

            _txtFalseExpression = new UITextBox
            {
                Location = new Point(0, 0),
                Size = new Size(510, 36),
                Watermark = "输入要执行的表达式"
            };
            _txtFalseExpression.TextChanged += (s, e) => MarkAsModified();

            _panelFalseExpression.Controls.Add(_txtFalseExpression);

            _grpFalseAction.Controls.AddRange(new Control[]
            {
                lblAction, _cmbFalseAction, _panelFalseJump, _panelFalseExpression
            });

            parent.Controls.Add(_grpFalseAction);
            yPos += 110;
        }

        /// <summary>
        /// 创建结果保存区域
        /// </summary>
        private void CreateResultSaveSection(Panel parent, ref int yPos)
        {
            var grpResult = new GroupBox
            {
                Text = "结果保存",
                Location = new Point(10, yPos),
                Size = new Size(540, 80),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            _chkSaveResult = new CheckBox
            {
                Text = "将判断结果保存到变量",
                Location = new Point(15, 28),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };
            _chkSaveResult.CheckedChanged += (s, e) =>
            {
                _txtResultVariable.Enabled = _chkSaveResult.Checked;
                MarkAsModified();
            };

            var lblVar = new Label
            {
                Text = "变量名:",
                Location = new Point(230, 30),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };

            _txtResultVariable = new UITextBox
            {
                Location = new Point(290, 25),
                Size = new Size(230, 32),
                Watermark = "选择或输入变量名",
                Enabled = false
            };
            _txtResultVariable.TextChanged += (s, e) => MarkAsModified();

            grpResult.Controls.AddRange(new Control[]
            {
                _chkSaveResult, lblVar, _txtResultVariable
            });

            parent.Controls.Add(grpResult);
            yPos += 90;
        }

        /// <summary>
        /// 创建描述区域
        /// </summary>
        private void CreateDescriptionSection(Panel parent, ref int yPos)
        {
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
                Size = new Size(540, 36),
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
        /// ⭐ 初始化表达式输入面板 - 展示多种使用方式
        /// </summary>
        protected override void InitializeExpressionInputs()
        {
            // 1. 条件表达式 - 使用条件输入模式
            _txtCondition.WithConditionInput();

            // 2. True 分支执行表达式 - 使用通用表达式模式
            _txtTrueExpression.WithExpressionInput(InputModules.All);

            // 3. False 分支执行表达式 - 使用通用表达式模式
            _txtFalseExpression.WithExpressionInput(InputModules.All);

            // 4. 结果变量 - 使用变量选择模式
            _txtResultVariable.WithVariableInput();

            // 5. 使用构建器模式的高级配置示例
            ExpressionInputBuilder.For(_txtCondition)
                .WithMode(InputMode.Condition)
                .EnableAllModules()
                .WithTitle("配置条件表达式")
                .ShowValidation(true)
                .ShowPreview(true)
                .OnValidated(result =>
                {
                    // 更新预览
                    UpdateConditionPreview(result);
                })
                .Attach();

            Logger?.LogDebug("ConditionConfigForm 表达式输入面板已配置");
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 条件变更
        /// </summary>
        private void OnConditionChanged(object sender, EventArgs e)
        {
            MarkAsModified();
            // 验证会通过 ExpressionInputPanel 的回调触发
        }

        /// <summary>
        /// 更新条件预览
        /// </summary>
        private void UpdateConditionPreview(ValidationInfo validationResult)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateConditionPreview(validationResult)));
                return;
            }

            if (validationResult.IsValid)
            {
                _lblConditionPreview.Text = "预览: 条件表达式有效";
                _lblConditionPreview.ForeColor = Color.FromArgb(82, 196, 26);
                _conditionPreviewPanel.BackColor = Color.FromArgb(246, 255, 237);
            }
            else
            {
                _lblConditionPreview.Text = $"预览: {validationResult.Message}";
                _lblConditionPreview.ForeColor = Color.FromArgb(255, 77, 79);
                _conditionPreviewPanel.BackColor = Color.FromArgb(255, 241, 240);
            }
        }

        /// <summary>
        /// 更新 True 动作面板
        /// </summary>
        private void UpdateTrueActionPanels()
        {
            _panelTrueJump.Visible = _cmbTrueAction.SelectedIndex == 1;
            _panelTrueExpression.Visible = _cmbTrueAction.SelectedIndex == 3;

            // 调整 GroupBox 高度
            _grpTrueAction.Height = _cmbTrueAction.SelectedIndex == 3 ? 100 : 60;
        }

        /// <summary>
        /// 更新 False 动作面板
        /// </summary>
        private void UpdateFalseActionPanels()
        {
            _panelFalseJump.Visible = _cmbFalseAction.SelectedIndex == 1;
            _panelFalseExpression.Visible = _cmbFalseAction.SelectedIndex == 3;

            // 调整 GroupBox 高度
            _grpFalseAction.Height = _cmbFalseAction.SelectedIndex == 3 ? 100 : 60;
        }

        #endregion

        #region 参数处理

        public void SetParameter(ConditionParameter parameter)
        {
            _parameter = parameter ?? new ConditionParameter();
            LoadParameterToForm();
        }

        public new ConditionParameter GetParameter()
        {
            SaveFormToParameter();
            return _parameter;
        }

        protected override void OnSetParameter(object parameter)
        {
            _parameter = ConvertParameter<ConditionParameter>(parameter) ?? new ConditionParameter();
        }

        protected override object OnGetParameter()
        {
            return _parameter;
        }

        protected override void LoadParameterToForm()
        {
            if (_parameter == null)
            {
                _parameter = new ConditionParameter();
            }

            try
            {
                IsLoading = true;

                // 条件表达式
                _txtCondition.Text = _parameter.ConditionExpression ?? "";

                // True 动作
                LoadActionToUI(_parameter.TrueAction, _cmbTrueAction, _numTrueJumpStep, _txtTrueExpression);

                // False 动作
                LoadActionToUI(_parameter.FalseAction, _cmbFalseAction, _numFalseJumpStep, _txtFalseExpression);

                // 结果保存
                _chkSaveResult.Checked = _parameter.SaveResultToVariable;
                _txtResultVariable.Text = _parameter.ResultVariableName ?? "";
                _txtResultVariable.Enabled = _parameter.SaveResultToVariable;

                // 描述和启用
                _txtDescription.Text = _parameter.Description ?? "";
                _chkEnabled.Checked = _parameter.IsEnabled;

                UpdateTrueActionPanels();
                UpdateFalseActionPanels();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadActionToUI(ConditionAction action, ComboBox cmb, NumericUpDown num, UITextBox txt)
        {
            if (action == null) return;

            cmb.SelectedIndex = (int)action.ActionType;

            if (action.JumpToStep.HasValue)
            {
                num.Value = action.JumpToStep.Value;
            }

            txt.Text = action.ExecuteExpression ?? "";
        }

        protected override void SaveFormToParameter()
        {
            _parameter ??= new ConditionParameter();

            // 条件表达式
            _parameter.ConditionExpression = _txtCondition.Text?.Trim();

            // True 动作
            _parameter.TrueAction = SaveActionFromUI(_cmbTrueAction, _numTrueJumpStep, _txtTrueExpression);

            // False 动作
            _parameter.FalseAction = SaveActionFromUI(_cmbFalseAction, _numFalseJumpStep, _txtFalseExpression);

            // 结果保存
            _parameter.SaveResultToVariable = _chkSaveResult.Checked;
            _parameter.ResultVariableName = _txtResultVariable.Text?.Trim();

            // 描述和启用
            _parameter.Description = _txtDescription.Text?.Trim();
            _parameter.IsEnabled = _chkEnabled.Checked;
        }

        private ConditionAction SaveActionFromUI(ComboBox cmb, NumericUpDown num, UITextBox txt)
        {
            return new ConditionAction
            {
                ActionType = (ConditionActionType)cmb.SelectedIndex,
                JumpToStep = cmb.SelectedIndex == 1 ? (int)num.Value : null,
                ExecuteExpression = cmb.SelectedIndex == 3 ? txt.Text?.Trim() : null
            };
        }

        protected override void OnResetToDefault()
        {
            _parameter = new ConditionParameter();
        }

        #endregion

        #region 验证

        protected override ParameterValidationResult OnValidate()
        {
            var errors = new List<string>();

            // 条件表达式必填
            if (string.IsNullOrWhiteSpace(_txtCondition.Text))
            {
                errors.Add("请输入条件表达式");
            }

            // 跳转步骤号验证
            if (_cmbTrueAction.SelectedIndex == 1 && _numTrueJumpStep.Value < 1)
            {
                errors.Add("True 分支的跳转步骤号无效");
            }

            if (_cmbFalseAction.SelectedIndex == 1 && _numFalseJumpStep.Value < 1)
            {
                errors.Add("False 分支的跳转步骤号无效");
            }

            // 执行表达式验证
            if (_cmbTrueAction.SelectedIndex == 3 && string.IsNullOrWhiteSpace(_txtTrueExpression.Text))
            {
                errors.Add("请输入 True 分支的执行表达式");
            }

            if (_cmbFalseAction.SelectedIndex == 3 && string.IsNullOrWhiteSpace(_txtFalseExpression.Text))
            {
                errors.Add("请输入 False 分支的执行表达式");
            }

            // 结果变量验证
            if (_chkSaveResult.Checked && string.IsNullOrWhiteSpace(_txtResultVariable.Text))
            {
                errors.Add("请选择或输入结果变量名");
            }

            if (errors.Count > 0)
            {
                return ParameterValidationResult.Invalid(errors.ToArray());
            }

            return ParameterValidationResult.Valid();
        }

        #endregion
    }
}
