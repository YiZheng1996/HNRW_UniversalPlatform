using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Core.Domain.Variables;
using MainUI.UniversalPlatform.Core.Domain.Workflows;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner
{
    /// <summary>
    /// 工作流设计器视图接口
    /// 定义View必须实现的所有UI操作
    /// </summary>
    public interface IWorkflowDesignerView
    {
        #region 属性

        /// <summary>
        /// 当前选中的步骤索引
        /// </summary>
        int SelectedStepIndex { get; set; }

        /// <summary>
        /// 是否处于执行状态
        /// </summary>
        bool IsExecuting { set; }

        /// <summary>
        /// 窗体标题
        /// </summary>
        string Title { set; }

        #endregion

        #region 显示方法

        /// <summary>
        /// 显示步骤列表
        /// </summary>
        void DisplaySteps(IReadOnlyList<WorkflowStep> steps);

        /// <summary>
        /// 显示变量列表
        /// </summary>
        void DisplayVariables(IEnumerable<Variable> variables);

        /// <summary>
        /// 更新步骤状态
        /// </summary>
        void UpdateStepStatus(int index, WorkflowStep step);

        /// <summary>
        /// 更新步骤详情
        /// </summary>
        void UpdateStepDetails(int index, string previewText);

        /// <summary>
        /// 显示执行进度
        /// </summary>
        void ShowProgress(int current, int total, string message);

        /// <summary>
        /// 显示消息
        /// </summary>
        void ShowMessage(string message, MessageLevel level = MessageLevel.Info);

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        bool ShowConfirm(string message, string title = "确认");

        /// <summary>
        /// 刷新工具箱
        /// </summary>
        void RefreshToolbox(IEnumerable<StepTypeInfo> stepTypes);

        /// <summary>
        /// 刷新整个视图
        /// </summary>
        void RefreshView();

        /// <summary>
        /// 滚动到指定步骤
        /// </summary>
        void ScrollToStep(int index);

        /// <summary>
        /// 高亮执行中的步骤
        /// </summary>
        void HighlightExecutingStep(int index);

        #endregion

        #region 事件（View → Presenter）

        /// <summary>
        /// 添加步骤请求
        /// </summary>
        event Action<string, int?> AddStepRequested;

        /// <summary>
        /// 删除步骤请求
        /// </summary>
        event Action<int> DeleteStepRequested;

        /// <summary>
        /// 移动步骤请求
        /// </summary>
        event Action<int, int> MoveStepRequested;

        /// <summary>
        /// 配置步骤请求
        /// </summary>
        event Action<int> ConfigureStepRequested;

        /// <summary>
        /// 选择步骤事件
        /// </summary>
        event Action<int> StepSelected;

        /// <summary>
        /// 执行工作流请求
        /// </summary>
        event Action ExecuteRequested;

        /// <summary>
        /// 停止执行请求
        /// </summary>
        event Action StopRequested;

        /// <summary>
        /// 保存请求
        /// </summary>
        event Action SaveRequested;

        /// <summary>
        /// 关闭请求
        /// </summary>
        event Func<bool> CloseRequested;

        /// <summary>
        /// 添加变量请求
        /// </summary>
        event Action AddVariableRequested;

        /// <summary>
        /// 删除变量请求
        /// </summary>
        event Action<string> DeleteVariableRequested;

        #endregion
    }

    /// <summary>
    /// 消息级别
    /// </summary>
    public enum MessageLevel
    {
        Info,
        Success,
        Warning,
        Error
    }


}
