using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Core.Domain.Workflows;
using Microsoft.Extensions.Logging;

namespace MainUI.UniversalPlatform.Infrastructure.DependencyInjection
{

    /// <summary>
    /// 工作流应用服务实现
    /// </summary>
    public class WorkflowAppService(
        IWorkflowRepository workflowRepository,
        IStepExecutorFactory executorFactory,
        IVariableService variableService,
        ILogger<WorkflowAppService> logger) : IWorkflowAppService
    {
        private CancellationTokenSource _executionCts;

        public bool IsExecuting { get; private set; }

        public event Action<WorkflowStep, int> StepStatusChanged;
        public event Action<int, int, string> ExecutionProgress;
        public event Action<bool, string> ExecutionCompleted;

        public async Task<Workflow> LoadWorkflowAsync(string modelType, string modelName, string itemName, CancellationToken cancellationToken = default)
        {
            return await workflowRepository.LoadAsync(modelType, modelName, itemName, cancellationToken);
        }

        public async Task SaveWorkflowAsync(Workflow workflow, CancellationToken cancellationToken = default)
        {
            await workflowRepository.SaveAsync(workflow, cancellationToken);
        }

        public Task<Workflow> CreateWorkflowAsync(string modelType, string modelName, string itemName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Workflow(modelType, modelName, itemName));
        }

        public async Task DeleteWorkflowAsync(string modelType, string modelName, string itemName, CancellationToken cancellationToken = default)
        {
            await workflowRepository.DeleteAsync(modelType, modelName, itemName, cancellationToken);
        }

        public async Task<IEnumerable<WorkflowSummaryDto>> GetWorkflowListAsync(string modelType, string modelName, CancellationToken cancellationToken = default)
        {
            var summaries = await workflowRepository.GetWorkflowsAsync(modelType, modelName, cancellationToken);
            return summaries.Select(s => new WorkflowSummaryDto
            {
                ModelType = s.ModelType,
                ModelName = s.ModelName,
                ItemName = s.ItemName,
                StepCount = s.StepCount,
                LastModified = s.LastModified
            });
        }

        public Task<WorkflowStep> AddStepAsync(Workflow workflow, string stepName, object parameter = null, string remark = null, CancellationToken cancellationToken = default)
        {
            var step = workflow.AddStep(stepName, parameter, remark);
            return Task.FromResult(step);
        }

        public Task<WorkflowStep> InsertStepAsync(Workflow workflow, int index, string stepName, object parameter = null, string remark = null, CancellationToken cancellationToken = default)
        {
            var step = workflow.InsertStep(index, stepName, parameter, remark);
            return Task.FromResult(step);
        }

        public Task<bool> RemoveStepAsync(Workflow workflow, int index, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(workflow.RemoveStep(index));
        }

        public Task<bool> MoveStepAsync(Workflow workflow, int fromIndex, int toIndex, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(workflow.MoveStep(fromIndex, toIndex));
        }

        public Task UpdateStepParameterAsync(Workflow workflow, int index, object parameter, CancellationToken cancellationToken = default)
        {
            workflow.UpdateStepParameter(index, parameter);
            return Task.CompletedTask;
        }

        public async Task<Core.Application.Interfaces.WorkflowExecutionResult> ExecuteWorkflowAsync(Workflow workflow, CancellationToken cancellationToken = default)
        {
            IsExecuting = true;
            _executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var startTime = DateTime.Now;
            var results = new List<StepExecutionResultDto>();

            try
            {
                logger.LogInformation("开始执行工作流: {Id}", workflow.Id);

                for (int i = 0; i < workflow.StepCount; i++)
                {
                    _executionCts.Token.ThrowIfCancellationRequested();

                    var step = workflow.GetStep(i);
                    step.MarkAsRunning();
                    StepStatusChanged?.Invoke(step, i);
                    ExecutionProgress?.Invoke(i + 1, workflow.StepCount, step.StepName);

                    var stepResult = await ExecuteStepAsync(workflow, i, _executionCts.Token);
                    results.Add(stepResult);

                    if (!stepResult.Success)
                    {
                        step.MarkAsFailed(stepResult.Message);
                        StepStatusChanged?.Invoke(step, i);

                        return new WorkflowExecutionResult
                        {
                            Success = false,
                            Message = $"步骤 {i + 1} 执行失败: {stepResult.Message}",
                            TotalSteps = workflow.StepCount,
                            CompletedSteps = i,
                            FailedStep = i,
                            Duration = DateTime.Now - startTime,
                            StepResults = results
                        };
                    }

                    step.MarkAsSucceeded();
                    StepStatusChanged?.Invoke(step, i);
                }

                var duration = DateTime.Now - startTime;
                ExecutionCompleted?.Invoke(true, $"工作流执行完成，共 {workflow.StepCount} 步，耗时 {duration.TotalSeconds:F1}s");

                return new WorkflowExecutionResult
                {
                    Success = true,
                    Message = "工作流执行成功",
                    TotalSteps = workflow.StepCount,
                    CompletedSteps = workflow.StepCount,
                    Duration = duration,
                    StepResults = results
                };
            }
            catch (OperationCanceledException)
            {
                ExecutionCompleted?.Invoke(false, "工作流执行被取消");
                return new WorkflowExecutionResult
                {
                    Success = false,
                    Message = "执行被取消",
                    TotalSteps = workflow.StepCount,
                    CompletedSteps = results.Count,
                    Duration = DateTime.Now - startTime,
                    StepResults = results
                };
            }
            finally
            {
                IsExecuting = false;
                _executionCts?.Dispose();
                _executionCts = null;
            }
        }

        public async Task<StepExecutionResultDto> ExecuteStepAsync(Workflow workflow, int stepIndex, CancellationToken cancellationToken = default)
        {
            var step = workflow.GetStep(stepIndex);
            if (step == null)
            {
                return new StepExecutionResultDto { Success = false, Message = "步骤不存在" };
            }

            var executor = executorFactory.GetExecutor(step.StepName);
            if (executor == null)
            {
                return new StepExecutionResultDto { Success = false, Message = $"不支持的步骤类型: {step.StepName}" };
            }

            var context = new StepExecutionContext
            {
                StepIndex = stepIndex,
                TotalSteps = workflow.StepCount,
                WorkflowId = workflow.Id,
                ModelType = workflow.ModelType,
                ModelName = workflow.ModelName,
                ItemName = workflow.ItemName
            };

            var result = await executor.ExecuteAsync(step.Parameter, context, cancellationToken);

            return new StepExecutionResultDto
            {
                StepIndex = stepIndex,
                StepName = step.StepName,
                Success = result.Success,
                Message = result.Message,
                Duration = result.Duration,
                OutputData = result.OutputData
            };
        }

        public void StopExecution()
        {
            _executionCts?.Cancel();
        }

        Task<WorkflowExecutionResult> IWorkflowAppService.ExecuteWorkflowAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
