using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Core.Domain.Variables;
using MainUI.UniversalPlatform.Core.Domain.Workflows;
using MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms;
using Microsoft.Extensions.Logging;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner
{
    /// <summary>
    /// 工作流设计器Presenter
    /// 处理所有业务逻辑，协调View和Model
    /// </summary>
    public class WorkflowDesignerPresenter : IDisposable
    {
        #region 字段

        private readonly IWorkflowDesignerView _view;
        private readonly IWorkflowAppService _workflowService;
        private readonly IVariableService _variableService;
        private readonly IStepConfigService _stepConfigService;
        private readonly ILogger<WorkflowDesignerPresenter> _logger;

        private Workflow _currentWorkflow;
        private bool _hasUnsavedChanges;
        private bool _disposed;

        #endregion

        #region 属性

        /// <summary>
        /// 当前工作流
        /// </summary>
        public Workflow CurrentWorkflow => _currentWorkflow;

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        public bool HasUnsavedChanges => _hasUnsavedChanges;

        #endregion

        #region 构造函数

        public WorkflowDesignerPresenter(
            IWorkflowDesignerView view,
            IWorkflowAppService workflowService,
            IVariableService variableService,
            IStepConfigService stepConfigService,
            ILogger<WorkflowDesignerPresenter> logger)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
            _stepConfigService = stepConfigService ?? throw new ArgumentNullException(nameof(stepConfigService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 订阅视图事件
            SubscribeToViewEvents();

            // 订阅服务事件
            SubscribeToServiceEvents();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 订阅视图事件
        /// </summary>
        private void SubscribeToViewEvents()
        {
            _view.AddStepRequested += OnAddStepRequested;
            _view.DeleteStepRequested += OnDeleteStepRequested;
            _view.MoveStepRequested += OnMoveStepRequested;
            _view.ConfigureStepRequested += OnConfigureStepRequested;
            _view.StepSelected += OnStepSelected;
            _view.ExecuteRequested += OnExecuteRequested;
            _view.StopRequested += OnStopRequested;
            _view.SaveRequested += OnSaveRequested;
            _view.CloseRequested += OnCloseRequested;
            _view.AddVariableRequested += OnAddVariableRequested;
            _view.DeleteVariableRequested += OnDeleteVariableRequested;
        }

        /// <summary>
        /// 订阅服务事件
        /// </summary>
        private void SubscribeToServiceEvents()
        {
            _workflowService.StepStatusChanged += OnStepStatusChanged;
            _workflowService.ExecutionProgress += OnExecutionProgress;
            _workflowService.ExecutionCompleted += OnExecutionCompleted;
            _variableService.VariableChanged += OnVariableChanged;
        }

        /// <summary>
        /// 加载工作流
        /// </summary>
        public async Task LoadWorkflowAsync(string modelType, string modelName, string itemName)
        {
            try
            {
                _logger.LogInformation("加载工作流: {ModelType}/{ModelName}/{ItemName}",
                    modelType, modelName, itemName);

                _currentWorkflow = await _workflowService.LoadWorkflowAsync(modelType, modelName, itemName);

                // 更新视图
                _view.Title = $"产品类型：{modelType}，产品型号：{modelName}，项点名称：{itemName}";
                _view.DisplaySteps(_currentWorkflow.Steps);

                // 加载变量
                await LoadVariablesAsync();

                // 刷新工具箱
                _view.RefreshToolbox(_stepConfigService.GetStepTypes());

                _hasUnsavedChanges = false;

                _logger.LogInformation("工作流加载完成，共 {StepCount} 个步骤", _currentWorkflow.StepCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载工作流失败");
                _view.ShowMessage($"加载失败：{ex.Message}", MessageLevel.Error);
            }
        }

        /// <summary>
        /// 加载变量
        /// </summary>
        private async Task LoadVariablesAsync()
        {
            // 从仓储加载变量并注入到变量服务
            // 这里简化处理，实际应该从仓储加载
            _view.DisplayVariables(_variableService.GetAllVariables());
        }

        #endregion

        #region 步骤操作

        private async void OnAddStepRequested(string stepName, int? insertIndex)
        {
            try
            {
                _logger.LogDebug("添加步骤请求: {StepName}, 位置: {Index}", stepName, insertIndex);

                WorkflowStep step;
                if (insertIndex.HasValue)
                {
                    step = await _workflowService.InsertStepAsync(_currentWorkflow, insertIndex.Value, stepName);
                }
                else
                {
                    step = await _workflowService.AddStepAsync(_currentWorkflow, stepName);
                }

                _view.DisplaySteps(_currentWorkflow.Steps);
                _view.SelectedStepIndex = step.StepNumber - 1;
                _hasUnsavedChanges = true;

                // 自动打开配置窗口
                OnConfigureStepRequested(step.StepNumber - 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加步骤失败");
                _view.ShowMessage($"添加步骤失败：{ex.Message}", MessageLevel.Error);
            }
        }

        private async void OnDeleteStepRequested(int index)
        {
            try
            {
                if (!_view.ShowConfirm("确定要删除选中的步骤吗？"))
                    return;

                await _workflowService.RemoveStepAsync(_currentWorkflow, index);
                _view.DisplaySteps(_currentWorkflow.Steps);
                _hasUnsavedChanges = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除步骤失败");
                _view.ShowMessage($"删除步骤失败：{ex.Message}", MessageLevel.Error);
            }
        }

        private async void OnMoveStepRequested(int fromIndex, int toIndex)
        {
            try
            {
                await _workflowService.MoveStepAsync(_currentWorkflow, fromIndex, toIndex);
                _view.DisplaySteps(_currentWorkflow.Steps);
                _view.SelectedStepIndex = toIndex;
                _hasUnsavedChanges = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移动步骤失败");
                _view.ShowMessage($"移动步骤失败：{ex.Message}", MessageLevel.Error);
            }
        }

        private void OnConfigureStepRequested(int index)
        {
            try
            {
                var step = _currentWorkflow.GetStep(index);
                if (step == null) return;

                // TODO: 打开配置窗口
                // 这里需要根据步骤类型打开对应的配置窗口
                _logger.LogDebug("配置步骤: {Index} - {StepName}", index, step.StepName);

                //var step = _currentWorkflow.Steps[index];
                //var form = StepFormFactory.CreateConfigForm(step.StepName, step.Parameter);

                //if (form != null && form.ShowDialog() == DialogResult.OK)
                //{
                //    var newParam = ((BaseParameterForm)form).ResultParameter;
                //    _workflowService.UpdateStepParameterAsync(_currentWorkflow, index, newParam);
                //    _view.RefreshView();
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开配置窗口失败");
            }
        }

        private void OnStepSelected(int index)
        {
            try
            {
                var step = _currentWorkflow.GetStep(index);
                if (step != null)
                {
                    var previewText = _stepConfigService.GetPreviewText(step.StepName, step.Parameter);
                    _view.UpdateStepDetails(index, previewText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新步骤详情失败");
            }
        }

        #endregion

        #region 执行操作

        private async void OnExecuteRequested()
        {
            if (_workflowService.IsExecuting)
            {
                _view.ShowMessage("工作流正在执行中", MessageLevel.Warning);
                return;
            }

            if (_currentWorkflow.StepCount == 0)
            {
                _view.ShowMessage("工作流没有步骤可执行", MessageLevel.Warning);
                return;
            }

            try
            {
                _view.IsExecuting = true;

                // 重置所有步骤状态
                for (int i = 0; i < _currentWorkflow.StepCount; i++)
                {
                    _currentWorkflow.GetStep(i)?.ResetStatus();
                }
                _view.DisplaySteps(_currentWorkflow.Steps);

                var result = await _workflowService.ExecuteWorkflowAsync(_currentWorkflow);

                if (result.Success)
                {
                    _view.ShowMessage($"执行完成，耗时 {result.Duration.TotalSeconds:F1}s", MessageLevel.Success);
                }
                else
                {
                    _view.ShowMessage($"执行失败：{result.Message}", MessageLevel.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行工作流失败");
                _view.ShowMessage($"执行错误：{ex.Message}", MessageLevel.Error);
            }
            finally
            {
                _view.IsExecuting = false;
            }
        }

        private void OnStopRequested()
        {
            _workflowService.StopExecution();
            _view.ShowMessage("已请求停止执行", MessageLevel.Info);
        }

        private void OnStepStatusChanged(WorkflowStep step, int index)
        {
            _view.UpdateStepStatus(index, step);
            _view.HighlightExecutingStep(index);
        }

        private void OnExecutionProgress(int current, int total, string stepName)
        {
            _view.ShowProgress(current, total, $"正在执行: {stepName}");
        }

        private void OnExecutionCompleted(bool success, string message)
        {
            _view.IsExecuting = false;
            _view.ShowMessage(message, success ? MessageLevel.Success : MessageLevel.Error);
        }

        #endregion

        #region 保存操作

        private async void OnSaveRequested()
        {
            try
            {
                await _workflowService.SaveWorkflowAsync(_currentWorkflow);
                _hasUnsavedChanges = false;
                _view.ShowMessage("保存成功", MessageLevel.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存失败");
                _view.ShowMessage($"保存失败：{ex.Message}", MessageLevel.Error);
            }
        }

        private bool OnCloseRequested()
        {
            if (_hasUnsavedChanges)
            {
                return _view.ShowConfirm("有未保存的更改，确定要关闭吗？");
            }
            return true;
        }

        #endregion

        #region 变量操作

        private void OnAddVariableRequested()
        {
            // TODO: 打开变量添加对话框
        }

        private void OnDeleteVariableRequested(string variableName)
        {
            if (_view.ShowConfirm($"确定要删除变量 '{variableName}' 吗？"))
            {
                _variableService.RemoveVariable(variableName);
                _view.DisplayVariables(_variableService.GetAllVariables());
                _hasUnsavedChanges = true;
            }
        }

        private void OnVariableChanged(Variable variable)
        {
            _view.DisplayVariables(_variableService.GetAllVariables());
        }

        #endregion

        #region 资源释放

        public void Dispose()
        {
            if (_disposed) return;

            // 取消订阅视图事件
            _view.AddStepRequested -= OnAddStepRequested;
            _view.DeleteStepRequested -= OnDeleteStepRequested;
            _view.MoveStepRequested -= OnMoveStepRequested;
            _view.ConfigureStepRequested -= OnConfigureStepRequested;
            _view.StepSelected -= OnStepSelected;
            _view.ExecuteRequested -= OnExecuteRequested;
            _view.StopRequested -= OnStopRequested;
            _view.SaveRequested -= OnSaveRequested;
            _view.CloseRequested -= OnCloseRequested;
            _view.AddVariableRequested -= OnAddVariableRequested;
            _view.DeleteVariableRequested -= OnDeleteVariableRequested;

            // 取消订阅服务事件
            _workflowService.StepStatusChanged -= OnStepStatusChanged;
            _workflowService.ExecutionProgress -= OnExecutionProgress;
            _workflowService.ExecutionCompleted -= OnExecutionCompleted;
            _variableService.VariableChanged -= OnVariableChanged;

            _disposed = true;
        }

        #endregion
    }
}
