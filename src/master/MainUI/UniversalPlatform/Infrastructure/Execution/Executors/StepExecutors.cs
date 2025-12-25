using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MainUI.UniversalPlatform.Infrastructure.Execution.Executors
{
    #region 延时等待执行器

    /// <summary>
    /// 延时等待步骤参数
    /// </summary>
    public class DelayParameter
    {
        /// <summary>
        /// 延时时间（毫秒）
        /// </summary>
        public int DelayMs { get; set; } = 1000;

        /// <summary>
        /// 延时时间（秒）- 优先使用
        /// </summary>
        public double? DelaySeconds { get; set; }

        /// <summary>
        /// 延时描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 获取实际延时毫秒数
        /// </summary>
        public int GetActualDelayMs()
        {
            if (DelaySeconds.HasValue)
                return (int)(DelaySeconds.Value * 1000);
            return DelayMs;
        }
    }

    /// <summary>
    /// 延时等待执行器
    /// </summary>
    public class DelayExecutor : BaseStepExecutor
    {
        public override string StepType => "延时等待";

        public DelayExecutor(ILogger<DelayExecutor> logger) : base(logger) { }

        public override ValidationResult ValidateParameter(object parameter)
        {
            var param = GetParameter<DelayParameter>(parameter);
            if (param.GetActualDelayMs() < 0)
                return ValidationResult.Invalid("延时时间不能为负数");
            return ValidationResult.Valid();
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<DelayParameter>(parameter);
            var delayMs = param.GetActualDelayMs();

            Logger?.LogInformation("开始延时等待: {DelayMs}ms", delayMs);

            await Task.Delay(delayMs, cancellationToken);

            return StepExecutionResult.Succeeded($"延时 {delayMs}ms 完成");
        }
    }

    #endregion

    #region 变量赋值执行器

    /// <summary>
    /// 变量赋值参数
    /// </summary>
    public class VariableAssignParameter
    {
        /// <summary>
        /// 目标变量名
        /// </summary>
        public string TargetVariable { get; set; }

        /// <summary>
        /// 赋值表达式
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// 赋值类型
        /// </summary>
        public AssignmentType AssignType { get; set; } = AssignmentType.Direct;

        /// <summary>
        /// 直接值（当AssignType为Direct时使用）
        /// </summary>
        public object DirectValue { get; set; }

        /// <summary>
        /// 源变量名（当AssignType为Variable时使用）
        /// </summary>
        public string SourceVariable { get; set; }

        /// <summary>
        /// PLC模块名（当AssignType为PLC时使用）
        /// </summary>
        public string PLCModule { get; set; }

        /// <summary>
        /// PLC点位名（当AssignType为PLC时使用）
        /// </summary>
        public string PLCTag { get; set; }
    }

    /// <summary>
    /// 赋值类型
    /// </summary>
    public enum AssignmentType
    {
        /// <summary>直接赋值</summary>
        Direct,
        /// <summary>表达式计算</summary>
        Expression,
        /// <summary>从变量复制</summary>
        Variable,
        /// <summary>从PLC读取</summary>
        PLC
    }

    /// <summary>
    /// 变量赋值执行器
    /// </summary>
    public class VariableAssignExecutor(
        IVariableService variableService,
        IExpressionEvaluator expressionEvaluator,
        IPLCAdapter plcAdapter,
        ILogger<VariableAssignExecutor> logger) : BaseStepExecutor(logger)
    {
        private readonly IVariableService _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));

        public override string StepType => "变量赋值";

        public override ValidationResult ValidateParameter(object parameter)
        {
            var param = GetParameter<VariableAssignParameter>(parameter);

            if (string.IsNullOrWhiteSpace(param.TargetVariable))
                return ValidationResult.Invalid("目标变量名不能为空");

            return ValidationResult.Valid();
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<VariableAssignParameter>(parameter);
            object value;

            // 根据赋值类型获取值
            switch (param.AssignType)
            {
                case AssignmentType.Direct:
                    value = param.DirectValue;
                    break;

                case AssignmentType.Expression:
                    if (expressionEvaluator == null)
                        return StepExecutionResult.Failed("表达式引擎不可用");
                    var evalResult = await expressionEvaluator.EvaluateAsync(param.Expression, cancellationToken);
                    if (!evalResult.Success)
                        return StepExecutionResult.Failed($"表达式计算失败: {evalResult.Error}");
                    value = evalResult.Result;
                    break;

                case AssignmentType.Variable:
                    var sourceVar = _variableService.GetVariable(param.SourceVariable);
                    if (sourceVar == null)
                        return StepExecutionResult.Failed($"源变量 '{param.SourceVariable}' 不存在");
                    value = sourceVar.Value;
                    break;

                case AssignmentType.PLC:
                    if (plcAdapter == null)
                        return StepExecutionResult.Failed("PLC适配器不可用");
                    var plcResult = await plcAdapter.ReadAsync(param.PLCModule, param.PLCTag, cancellationToken);
                    if (!plcResult.Success)
                        return StepExecutionResult.Failed($"PLC读取失败: {plcResult.Error}");
                    value = plcResult.Value;
                    break;

                default:
                    return StepExecutionResult.Failed($"不支持的赋值类型: {param.AssignType}");
            }

            // 设置变量值
            _variableService.SetVariable(param.TargetVariable, value, $"步骤{context.StepIndex + 1}", context.StepIndex);

            Logger?.LogInformation("变量赋值完成: {Variable} = {Value}", param.TargetVariable, value);

            return StepExecutionResult.Succeeded($"{param.TargetVariable} = {value}", value);
        }
    }

    #endregion

    #region 条件判断执行器

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
        /// 左值表达式
        /// </summary>
        public string LeftExpression { get; set; }

        /// <summary>
        /// 运算符
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// 右值表达式
        /// </summary>
        public string RightExpression { get; set; }

        /// <summary>
        /// 条件满足时执行的子步骤
        /// </summary>
        public List<object> TrueSteps { get; set; } = new();

        /// <summary>
        /// 条件不满足时执行的子步骤
        /// </summary>
        public List<object> FalseSteps { get; set; } = new();
    }

    /// <summary>
    /// 条件判断执行器
    /// </summary>
    public class ConditionExecutor : BaseStepExecutor
    {
        private readonly IExpressionEvaluator _expressionEvaluator;
        private readonly IChildStepExecutor _childStepExecutor;

        public override string StepType => "条件判断";

        public ConditionExecutor(
            IExpressionEvaluator expressionEvaluator,
            IChildStepExecutor childStepExecutor,
            ILogger<ConditionExecutor> logger) : base(logger)
        {
            _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
            _childStepExecutor = childStepExecutor;
        }

        public override ValidationResult ValidateParameter(object parameter)
        {
            var param = GetParameter<ConditionParameter>(parameter);

            bool hasFullExpression = !string.IsNullOrWhiteSpace(param.ConditionExpression);
            bool hasPartialExpression = !string.IsNullOrWhiteSpace(param.LeftExpression) &&
                                        !string.IsNullOrWhiteSpace(param.Operator);

            if (!hasFullExpression && !hasPartialExpression)
                return ValidationResult.Invalid("条件表达式不能为空");

            return ValidationResult.Valid();
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<ConditionParameter>(parameter);

            // 构建完整的条件表达式
            string expression = param.ConditionExpression;
            if (string.IsNullOrWhiteSpace(expression))
            {
                expression = $"{param.LeftExpression} {param.Operator} {param.RightExpression}";
            }

            // 计算条件
            var evalResult = await _expressionEvaluator.EvaluateAsync(expression, cancellationToken);
            if (!evalResult.Success)
                return StepExecutionResult.Failed($"条件计算失败: {evalResult.Error}");

            bool conditionMet = Convert.ToBoolean(evalResult.Result);

            Logger?.LogInformation("条件判断: {Expression} = {Result}", expression, conditionMet);

            // 执行对应的子步骤
            var stepsToExecute = conditionMet ? param.TrueSteps : param.FalseSteps;

            if (stepsToExecute?.Count > 0 && _childStepExecutor != null)
            {
                var childResult = await _childStepExecutor.ExecuteChildStepsAsync(
                    stepsToExecute, context, cancellationToken);

                if (!childResult.Success)
                    return childResult;
            }

            return StepExecutionResult.Succeeded(
                $"条件 {(conditionMet ? "满足" : "不满足")}: {expression}",
                conditionMet);
        }
    }

    #endregion

    #region 循环执行器

    /// <summary>
    /// 循环参数
    /// </summary>
    public class LoopParameter
    {
        /// <summary>
        /// 循环次数
        /// </summary>
        public int LoopCount { get; set; } = 1;

        /// <summary>
        /// 循环次数表达式
        /// </summary>
        public string LoopCountExpression { get; set; }

        /// <summary>
        /// 是否启用计数器变量
        /// </summary>
        public bool EnableCounter { get; set; }

        /// <summary>
        /// 计数器变量名
        /// </summary>
        public string CounterVariable { get; set; }

        /// <summary>
        /// 是否启用提前退出
        /// </summary>
        public bool EnableEarlyExit { get; set; }

        /// <summary>
        /// 退出条件表达式
        /// </summary>
        public string ExitCondition { get; set; }

        /// <summary>
        /// 循环体子步骤
        /// </summary>
        public List<object> ChildSteps { get; set; } = new();
    }

    /// <summary>
    /// 循环执行器
    /// </summary>
    public class LoopExecutor : BaseStepExecutor
    {
        private readonly IExpressionEvaluator _expressionEvaluator;
        private readonly IChildStepExecutor _childStepExecutor;
        private readonly IVariableService _variableService;

        public override string StepType => "循环工具";

        public LoopExecutor(
            IExpressionEvaluator expressionEvaluator,
            IChildStepExecutor childStepExecutor,
            IVariableService variableService,
            ILogger<LoopExecutor> logger) : base(logger)
        {
            _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
            _childStepExecutor = childStepExecutor ?? throw new ArgumentNullException(nameof(childStepExecutor));
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
        }

        public override ValidationResult ValidateParameter(object parameter)
        {
            var param = GetParameter<LoopParameter>(parameter);

            if (param.LoopCount <= 0 && string.IsNullOrWhiteSpace(param.LoopCountExpression))
                return ValidationResult.Invalid("循环次数必须大于0");

            return ValidationResult.Valid();
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<LoopParameter>(parameter);

            // 计算循环次数
            int loopCount = param.LoopCount;
            if (!string.IsNullOrWhiteSpace(param.LoopCountExpression))
            {
                var evalResult = await _expressionEvaluator.EvaluateAsync(param.LoopCountExpression, cancellationToken);
                if (evalResult.Success)
                    loopCount = Convert.ToInt32(evalResult.Result);
            }

            Logger?.LogInformation("开始循环执行，共 {Count} 次", loopCount);

            // 设置循环上下文
            context.IsInLoop = true;
            context.LoopTotal = loopCount;

            int completedCount = 0;

            for (int i = 1; i <= loopCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                context.LoopCounter = i;

                // 设置计数器变量
                if (param.EnableCounter && !string.IsNullOrWhiteSpace(param.CounterVariable))
                {
                    _variableService.SetVariable(param.CounterVariable, i, "循环计数器");
                }

                Logger?.LogDebug("执行第 {Current}/{Total} 次循环", i, loopCount);

                // 检查退出条件
                if (param.EnableEarlyExit && !string.IsNullOrWhiteSpace(param.ExitCondition))
                {
                    var exitResult = await _expressionEvaluator.EvaluateAsync(param.ExitCondition, cancellationToken);
                    if (exitResult.Success && Convert.ToBoolean(exitResult.Result))
                    {
                        Logger?.LogInformation("满足退出条件，在第 {Current} 次循环退出", i);
                        break;
                    }
                }

                // 执行子步骤
                if (param.ChildSteps?.Count > 0)
                {
                    var childResult = await _childStepExecutor.ExecuteChildStepsAsync(
                        param.ChildSteps, context, cancellationToken);

                    if (!childResult.Success)
                    {
                        return StepExecutionResult.Failed($"循环第 {i} 次执行失败: {childResult.Message}");
                    }

                    // 检查是否需要跳出循环
                    if (childResult.ShouldBreak)
                    {
                        Logger?.LogInformation("执行Break，在第 {Current} 次循环退出", i);
                        break;
                    }

                    // 检查是否需要继续下一次循环
                    if (childResult.ShouldContinue)
                    {
                        Logger?.LogDebug("执行Continue，跳过本次循环剩余步骤");
                        continue;
                    }
                }

                completedCount++;
            }

            context.IsInLoop = false;

            return StepExecutionResult.Succeeded($"循环执行完成，共执行 {completedCount} 次");
        }
    }

    #endregion

    #region 辅助接口

    /// <summary>
    /// 表达式计算器接口
    /// </summary>
    public interface IExpressionEvaluator
    {
        Task<ExpressionResult> EvaluateAsync(string expression, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 表达式计算结果
    /// </summary>
    public class ExpressionResult
    {
        public bool Success { get; init; }
        public object Result { get; init; }
        public string Error { get; init; }

        public static ExpressionResult Ok(object result) => new() { Success = true, Result = result };
        public static ExpressionResult Fail(string error) => new() { Success = false, Error = error };
    }

    /// <summary>
    /// 子步骤执行器接口
    /// </summary>
    public interface IChildStepExecutor
    {
        Task<StepExecutionResult> ExecuteChildStepsAsync(
            List<object> steps,
            StepExecutionContext context,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// PLC适配器接口
    /// </summary>
    public interface IPLCAdapter
    {
        Task<PLCResult> ReadAsync(string module, string tag, CancellationToken cancellationToken = default);
        Task<PLCResult> WriteAsync(string module, string tag, object value, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// PLC操作结果
    /// </summary>
    public class PLCResult
    {
        public bool Success { get; init; }
        public object Value { get; init; }
        public string Error { get; init; }

        public static PLCResult Ok(object value) => new() { Success = true, Value = value };
        public static PLCResult Fail(string error) => new() { Success = false, Error = error };
    }

    #endregion
}
