using AntdUI;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Core.Domain.Parameters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Panel = System.Windows.Forms.Panel;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms
{
    /// <summary>
    /// 参数窗体基类
    /// 提供统一的参数加载、保存、验证逻辑
    /// </summary>
    public partial class BaseParameterForm : UIForm
    {
        #region 服务

        protected readonly IVariableService VariableService;
        protected readonly ILogger Logger;

        #endregion

        #region 属性

        /// <summary>
        /// 是否正在加载
        /// </summary>
        protected bool IsLoading { get; set; } = true;

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        protected bool HasUnsavedChanges { get; set; } = false;

        /// <summary>
        /// 对话框结果参数
        /// </summary>
        public object ResultParameter { get; protected set; }

        #endregion

        #region 构造函数

        public BaseParameterForm()
        {
            if (DesignMode) return;

            try
            {
                VariableService = Program.ServiceProvider?.GetService<IVariableService>();
                Logger = Program.ServiceProvider?.GetService<ILogger<BaseParameterForm>>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BaseParameterForm 构造警告: {ex.Message}");
            }
        }

        #endregion

        #region 虚方法 - 子类重写

        /// <summary>
        /// 加载参数到界面
        /// </summary>
        protected virtual void LoadParameterToForm() { }

        /// <summary>
        /// 从界面保存到参数
        /// </summary>
        protected virtual void SaveFormToParameter() { }

        /// <summary>
        /// 验证输入
        /// </summary>
        protected virtual bool ValidateInput() => true;

        /// <summary>
        /// 获取参数对象
        /// </summary>
        protected virtual object GetParameter() => null;

        /// <summary>
        /// 设置参数对象
        /// </summary>
        protected virtual void SetParameter(object parameter) { }

        #endregion

        #region 保存逻辑

        /// <summary>
        /// 保存并关闭
        /// </summary>
        protected void SaveAndClose()
        {
            try
            {
                if (!ValidateInput())
                {
                    return;
                }

                SaveFormToParameter();
                ResultParameter = GetParameter();

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "保存参数失败");
                MessageHelper.MessageOK(this, $"保存失败：{ex.Message}", TType.Error);
            }
        }

        /// <summary>
        /// 取消并关闭
        /// </summary>
        protected void CancelAndClose()
        {
            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show("有未保存的更改，确定要关闭吗？", "确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;
            }

            DialogResult = DialogResult.Cancel;
            Close();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取所有变量名列表（用于下拉框）
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
            comboBox.Items.Clear();
            comboBox.Items.AddRange(GetVariableNames().ToArray());

            if (!string.IsNullOrEmpty(selectedValue) && comboBox.Items.Contains(selectedValue))
            {
                comboBox.SelectedItem = selectedValue;
            }
        }

        /// <summary>
        /// 参数转换
        /// </summary>
        protected T ConvertParameter<T>(object parameter) where T : class, new()
        {
            return ParameterManager.GetParameter<T>(parameter);
        }

        /// <summary>
        /// 标记已修改
        /// </summary>
        protected void MarkAsModified()
        {
            if (!IsLoading)
            {
                HasUnsavedChanges = true;
            }
        }

        #endregion

        #region 窗体样式

        /// <summary>
        /// 初始化窗体样式
        /// </summary>
        protected void InitializeFormStyle(string title, Color? titleColor = null)
        {
            Text = title;
            TitleColor = titleColor ?? Color.FromArgb(65, 100, 204);
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
        }

        /// <summary>
        /// 创建标准按钮面板
        /// </summary>
        protected Panel CreateButtonPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10)
            };

            var btnSave = new AntdUI.Button
            {
                Text = "保存",
                Type = TTypeMini.Primary,
                Size = new Size(80, 32),
                Anchor = AnchorStyles.Right
            };
            btnSave.Click += (s, e) => SaveAndClose();

            var btnCancel = new AntdUI.Button
            {
                Text = "取消",
                Size = new Size(80, 32),
                Anchor = AnchorStyles.Right
            };
            btnCancel.Click += (s, e) => CancelAndClose();

            // 布局
            btnCancel.Location = new Point(panel.Width - 90, 9);
            btnSave.Location = new Point(panel.Width - 180, 9);

            panel.Controls.AddRange(new Control[] { btnSave, btnCancel });

            return panel;
        }

        #endregion
    }
}