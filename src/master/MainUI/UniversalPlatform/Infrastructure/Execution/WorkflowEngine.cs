using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Domain.Workflows;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MainUI.UniversalPlatform.Infrastructure.Execution
{
    /// <summary>
    /// 工作流执行引擎
    /// 负责协调步骤执行器，管理执行流程
    /// </summary>
    public class WorkflowEngine : IDisposable
    {
        #region 私有字段

        private readonly IStepExecutorFactory _executorFactory;
        private readonly ILogger<WorkflowEngine> _logger;
        private CancellationTokenSource _executionCts;
        private bool _disposed;

        #endregion

        #region 属性

        /// <summary>
        /// 是否正在执行
        /// </summary>
        public bool IsExecuting { get; private set; }

        /// <summary>
        /// 当前执行的步骤索引
        /// </summary>
        public int CurrentStepIndex { get; private set; }

        #endregion

        #region 事件

        /// <summary>
        /// 步骤开始执行
        /// </summary>
        public event Action<int, WorkflowStep> StepStarting;

        /// <summary>
        /// 步骤执行完成
        /// </summary>
        public event Action<int, WorkflowStep, StepExecutionResult> StepCompleted;

        /// <summary>
        /// 执行进度更新
        /// </summary>
        public event Action<int, int, string> ProgressChanged;

        /// <summary>
        /// 执行日志
        /// </summary>
        public event Action<string, LogLevel> Log;

        #endregion

        #region 构造函数

        public WorkflowEngine(
            IStepExecutorFactory executorFactory,
            ILogger<WorkflowEngine> logger)
        {
            _executorFactory = executorFactory ?? throw new ArgumentNullException(nameof(executorFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 执行方法

        /// <summary>
        /// 执行整个工作流
        /// </summary>
        public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
            Workflow workflow,
            CancellationToken cancellationToken = default)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            if (IsExecuting)
                throw new InvalidOperationException("工作流正在执行中");

            IsExecuting = true;
            _executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var startTime = DateTime.Now;
            var stepResults = new List<StepExecutionResultInfo>();
            int completedSteps = 0;
            int failedStepIndex = -1;

            try
            {
                LogMessage($"开始执行工作流: {workflow.Id}, 共 {workflow.StepCount} 个步骤", LogLevel.Information);

                // 重置所有步骤状态
                for (int i = 0; i < workflow.StepCount; i++)
                {
                    workflow.GetStep(i)?.ResetStatus();
                }

                CurrentStepIndex = 0;

                while (CurrentStepIndex < workflow.StepCount)
                {
                    _executionCts.Token.ThrowIfCancellationRequested();

                    var step = workflow.GetStep(CurrentStepIndex);
                    if (step == null)
                    {
                        CurrentStepIndex++;
                        continue;
                    }

                    // 检查步骤是否启用
                    if (!step.IsEnabled)
                    {
                        step.MarkAsSkipped();
                        stepResults.Add(new StepExecutionResultInfo
                        {
                            StepIndex = CurrentStepIndex,
                            StepName = step.StepName,
                            Success = true,
                            Skipped = true,
                            Message = "步骤已禁用，跳过执行"
                        });
                        CurrentStepIndex++;
                        continue;
                    }

                    // 执行步骤
                    var result = await ExecuteStepInternalAsync(workflow, CurrentStepIndex, _executionCts.Token);

                    stepResults.Add(new StepExecutionResultInfo
                    {
                        StepIndex = CurrentStepIndex,
                        StepName = step.StepName,
                        Success = result.Success,
                        Message = result.Message,
                        Duration = result.Duration
                    });

                    if (result.Success)
                    {
                        completedSteps++;

                        // 检查跳转
                        if (result.NextStepIndex.HasValue)
                        {
                            LogMessage($"跳转到步骤 {result.NextStepIndex.Value + 1}", LogLevel.Information);
                            CurrentStepIndex = result.NextStepIndex.Value;
                        }
                        else
                        {
                            CurrentStepIndex++;
                        }
                    }
                    else
                    {
                        failedStepIndex = CurrentStepIndex;
                        LogMessage($"步骤 {CurrentStepIndex + 1} 执行失败: {result.Message}", LogLevel.Error);
                        break;
                    }
                }

                var duration = DateTime.Now - startTime;
                var success = failedStepIndex < 0;

                LogMessage(success
                    ? $"工作流执行完成，耗时 {duration.TotalSeconds:F1}s"
                    : $"工作流执行失败，在步骤 {failedStepIndex + 1} 停止",
                    success ? LogLevel.Information : LogLevel.Error);

                return new WorkflowExecutionResult
                {
                    Success = success,
                    Message = success ? "执行成功" : $"步骤 {failedStepIndex + 1} 执行失败",
                    TotalSteps = workflow.StepCount,
                    CompletedSteps = completedSteps,
                    FailedStepIndex = failedStepIndex,
                    Duration = duration,
                    StepResults = stepResults
                };
            }
            catch (OperationCanceledException)
            {
                LogMessage("工作流执行被取消", LogLevel.Warning);

                return new WorkflowExecutionResult
                {
                    Success = false,
                    Message = "执行被取消",
                    TotalSteps = workflow.StepCount,
                    CompletedSteps = completedSteps,
                    Duration = DateTime.Now - startTime,
                    StepResults = stepResults
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工作流执行异常");

                return new WorkflowExecutionResult
                {
                    Success = false,
                    Message = $"执行异常: {ex.Message}",
                    TotalSteps = workflow.StepCount,
                    CompletedSteps = completedSteps,
                    FailedStepIndex = CurrentStepIndex,
                    Duration = DateTime.Now - startTime,
                    StepResults = stepResults
                };
            }
            finally
            {
                IsExecuting = false;
                _executionCts?.Dispose();
                _executionCts = null;
            }
        }

        /// <summary>
        /// 执行单个步骤
        /// </summary>
        public async Task<StepExecutionResult> ExecuteStepAsync(
            Workflow workflow,
            int stepIndex,
            CancellationToken cancellationToken = default)
        {
            var step = workflow.GetStep(stepIndex);
            if (step == null)
            {
                return StepExecutionResult.Failed("步骤不存在");
            }

            return await ExecuteStepInternalAsync(workflow, stepIndex, cancellationToken);
        }

        /// <summary>
        /// 停止执行
        /// </summary>
        public void Stop()
        {
            if (IsExecuting && _executionCts != null)
            {
                LogMessage("请求停止工作流执行", LogLevel.Information);
                _executionCts.Cancel();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 内部执行步骤方法
        /// </summary>
        private async Task<StepExecutionResult> ExecuteStepInternalAsync(
            Workflow workflow,
            int stepIndex,
            CancellationToken cancellationToken)
        {
            var step = workflow.GetStep(stepIndex);
            var startTime = DateTime.Now;

            try
            {
                // 获取执行器
                var executor = _executorFactory.GetExecutor(step.StepName);
                if (executor == null)
                {
                    step.MarkAsFailed($"不支持的步骤类型: {step.StepName}");
                    return StepExecutionResult.Failed($"不支持的步骤类型: {step.StepName}");
                }

                // 标记为执行中
                step.MarkAsRunning();
                StepStarting?.Invoke(stepIndex, step);
                ProgressChanged?.Invoke(stepIndex + 1, workflow.StepCount, $"执行: {step.StepName}");
                LogMessage($"[{stepIndex + 1}/{workflow.StepCount}] 开始执行: {step.StepName}", LogLevel.Debug);

                // 创建执行上下文
                var context = new StepExecutionContext
                {
                    StepIndex = stepIndex,
                    TotalSteps = workflow.StepCount,
                    WorkflowId = workflow.Id,
                    ModelType = workflow.ModelType,
                    ModelName = workflow.ModelName,
                    ItemName = workflow.ItemName
                };

                // 执行步骤
                var result = await executor.ExecuteAsync(step.Parameter, context, cancellationToken);

                // 更新步骤状态
                if (result.Success)
                {
                    step.MarkAsSucceeded();
                    LogMessage($"[{stepIndex + 1}/{workflow.StepCount}] 执行成功: {step.StepName}, 耗时: {result.Duration.TotalMilliseconds:F0}ms",
                        LogLevel.Debug);
                }
                else
                {
                    step.MarkAsFailed(result.Message);
                    LogMessage($"[{stepIndex + 1}/{workflow.StepCount}] 执行失败: {step.StepName}, 原因: {result.Message}",
                        LogLevel.Warning);
                }

                StepCompleted?.Invoke(stepIndex, step, result);

                return result /*with { Duration = DateTime.Now - startTime }*/;
            }
            catch (OperationCanceledException)
            {
                step.MarkAsFailed("执行被取消");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "步骤执行异常: {StepName}", step.StepName);
                step.MarkAsFailed($"执行异常: {ex.Message}");

                var result = StepExecutionResult.Failed(ex.Message, ex);
                StepCompleted?.Invoke(stepIndex, step, result);

                return result /*with { Duration = DateTime.Now - startTime }*/;
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        private void LogMessage(string message, LogLevel level)
        {
            _logger.Log(level, message);
            Log?.Invoke(message, level);
        }

        #endregion

        #region 资源释放

        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            _executionCts?.Dispose();
            _disposed = true;
        }

        #endregion
    }

    /// <summary>
    /// 工作流执行结果
    /// </summary>
    public class WorkflowExecutionResult
    {
        public bool Success { get; init; }
        public string Message { get; init; }
        public int TotalSteps { get; init; }
        public int CompletedSteps { get; init; }
        public int FailedStepIndex { get; init; } = -1;
        public TimeSpan Duration { get; init; }
        public List<StepExecutionResultInfo> StepResults { get; init; } = new();
    }

    /// <summary>
    /// 步骤执行结果信息
    /// </summary>
    public class StepExecutionResultInfo
    {
        public int StepIndex { get; init; }
        public string StepName { get; init; }
        public bool Success { get; init; }
        public bool Skipped { get; init; }
        public string Message { get; init; }
        public TimeSpan Duration { get; init; }
    }

}
