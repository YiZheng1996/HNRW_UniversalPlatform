using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MainUI.UniversalPlatform.Infrastructure.Execution.Executors
{
    #region 变量定义执行器

    /// <summary>
    /// 变量定义参数
    /// </summary>
    public class VariableDefineParameter
    {
        /// <summary>
        /// 变量定义列表
        /// </summary>
        public List<VariableDefinition> Variables { get; set; } = new();
    }

    /// <summary>
    /// 单个变量定义
    /// </summary>
    public class VariableDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; } = "string";
        public object DefaultValue { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 变量定义执行器
    /// </summary>
    public class VariableDefineExecutor : BaseStepExecutor
    {
        private readonly IVariableService _variableService;

        public override string StepType => "变量定义";

        public VariableDefineExecutor(
            IVariableService variableService,
            ILogger<VariableDefineExecutor> logger) : base(logger)
        {
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
        }

        protected override Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<VariableDefineParameter>(parameter);

            int count = 0;
            foreach (var varDef in param.Variables)
            {
                if (string.IsNullOrWhiteSpace(varDef.Name))
                    continue;

                var varType = Core.Domain.Variables.VariableTypeExtensions.ParseVariableType(varDef.Type);
                var variable = Core.Domain.Variables.Variable.CreateUser(varDef.Name, varType, varDef.Description);

                if (varDef.DefaultValue != null)
                {
                    variable.SetValue(varDef.DefaultValue, "变量定义");
                }

                _variableService.AddVariable(variable);
                count++;

                Logger?.LogDebug("定义变量: {Name} ({Type})", varDef.Name, varDef.Type);
            }

            return Task.FromResult(StepExecutionResult.Succeeded($"成功定义 {count} 个变量"));
        }
    }

    #endregion

    #region 实时监控执行器

    /// <summary>
    /// 实时监控参数
    /// </summary>
    public class MonitorParameter
    {
        /// <summary>
        /// 监控表达式
        /// </summary>
        public string MonitorExpression { get; set; }

        /// <summary>
        /// 上限值
        /// </summary>
        public double? UpperLimit { get; set; }

        /// <summary>
        /// 下限值
        /// </summary>
        public double? LowerLimit { get; set; }

        /// <summary>
        /// 监控时长（秒）
        /// </summary>
        public double Duration { get; set; } = 10;

        /// <summary>
        /// 采样间隔（毫秒）
        /// </summary>
        public int SampleInterval { get; set; } = 500;

        /// <summary>
        /// 超限时是否停止
        /// </summary>
        public bool StopOnLimit { get; set; } = true;

        /// <summary>
        /// 目标变量名（保存结果）
        /// </summary>
        public string ResultVariable { get; set; }
    }

    /// <summary>
    /// 实时监控执行器
    /// </summary>
    public class MonitorExecutor : BaseStepExecutor
    {
        private readonly IExpressionEvaluator _expressionEvaluator;
        private readonly IVariableService _variableService;

        public override string StepType => "实时监控";

        public MonitorExecutor(
            IExpressionEvaluator expressionEvaluator,
            IVariableService variableService,
            ILogger<MonitorExecutor> logger) : base(logger)
        {
            _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<MonitorParameter>(parameter);

            Logger?.LogInformation("开始实时监控: {Expression}, 时长: {Duration}s",
                param.MonitorExpression, param.Duration);

            var startTime = DateTime.Now;
            var samples = new List<double>();
            double? minValue = null;
            double? maxValue = null;
            bool limitExceeded = false;

            while ((DateTime.Now - startTime).TotalSeconds < param.Duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 获取当前值
                var evalResult = await _expressionEvaluator.EvaluateAsync(param.MonitorExpression, cancellationToken);
                if (!evalResult.Success)
                {
                    Logger?.LogWarning("监控表达式计算失败: {Error}", evalResult.Error);
                    await Task.Delay(param.SampleInterval, cancellationToken);
                    continue;
                }

                double currentValue = Convert.ToDouble(evalResult.Result);
                samples.Add(currentValue);

                // 更新最值
                minValue = minValue.HasValue ? Math.Min(minValue.Value, currentValue) : currentValue;
                maxValue = maxValue.HasValue ? Math.Max(maxValue.Value, currentValue) : currentValue;

                // 检查限值
                bool upperExceeded = param.UpperLimit.HasValue && currentValue > param.UpperLimit.Value;
                bool lowerExceeded = param.LowerLimit.HasValue && currentValue < param.LowerLimit.Value;

                if (upperExceeded || lowerExceeded)
                {
                    limitExceeded = true;
                    string limitType = upperExceeded ? "上限" : "下限";
                    Logger?.LogWarning("监控值超出{LimitType}: {Value}", limitType, currentValue);

                    if (param.StopOnLimit)
                    {
                        return StepExecutionResult.Failed(
                            $"监控值 {currentValue} 超出{limitType}限制");
                    }
                }

                await Task.Delay(param.SampleInterval, cancellationToken);
            }

            // 计算统计结果
            double average = samples.Count > 0 ? samples.Average() : 0;

            // 保存结果到变量
            if (!string.IsNullOrWhiteSpace(param.ResultVariable))
            {
                _variableService.SetVariable(param.ResultVariable, average, "实时监控结果");
            }

            var resultMessage = $"监控完成: 采样 {samples.Count} 次, 平均值 {average:F3}, " +
                               $"最小值 {minValue:F3}, 最大值 {maxValue:F3}";

            if (limitExceeded)
            {
                resultMessage += " (有超限记录)";
            }

            Logger?.LogInformation(resultMessage);

            return StepExecutionResult.Succeeded(resultMessage, new
            {
                SampleCount = samples.Count,
                Average = average,
                Min = minValue,
                Max = maxValue,
                LimitExceeded = limitExceeded
            });
        }
    }

    #endregion

    #region 检测判定执行器

    /// <summary>
    /// 检测判定参数
    /// </summary>
    public class DetectionParameter
    {
        /// <summary>
        /// 检测表达式
        /// </summary>
        public string DetectionExpression { get; set; }

        /// <summary>
        /// 标准值
        /// </summary>
        public double StandardValue { get; set; }

        /// <summary>
        /// 上限偏差
        /// </summary>
        public double UpperDeviation { get; set; }

        /// <summary>
        /// 下限偏差
        /// </summary>
        public double LowerDeviation { get; set; }

        /// <summary>
        /// 结果变量名
        /// </summary>
        public string ResultVariable { get; set; }

        /// <summary>
        /// 判定结果变量名
        /// </summary>
        public string JudgmentVariable { get; set; }

        /// <summary>
        /// 不合格时是否停止
        /// </summary>
        public bool StopOnFail { get; set; } = false;
    }

    /// <summary>
    /// 检测判定执行器
    /// </summary>
    public class DetectionExecutor : BaseStepExecutor
    {
        private readonly IExpressionEvaluator _expressionEvaluator;
        private readonly IVariableService _variableService;

        public override string StepType => "检测判定";

        public DetectionExecutor(
            IExpressionEvaluator expressionEvaluator,
            IVariableService variableService,
            ILogger<DetectionExecutor> logger) : base(logger)
        {
            _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<DetectionParameter>(parameter);

            // 获取检测值
            var evalResult = await _expressionEvaluator.EvaluateAsync(param.DetectionExpression, cancellationToken);
            if (!evalResult.Success)
            {
                return StepExecutionResult.Failed($"检测表达式计算失败: {evalResult.Error}");
            }

            double actualValue = Convert.ToDouble(evalResult.Result);

            // 计算上下限
            double upperLimit = param.StandardValue + param.UpperDeviation;
            double lowerLimit = param.StandardValue - param.LowerDeviation;

            // 判定
            bool isPass = actualValue >= lowerLimit && actualValue <= upperLimit;

            // 保存结果
            if (!string.IsNullOrWhiteSpace(param.ResultVariable))
            {
                _variableService.SetVariable(param.ResultVariable, actualValue, "检测结果");
            }

            if (!string.IsNullOrWhiteSpace(param.JudgmentVariable))
            {
                _variableService.SetVariable(param.JudgmentVariable, isPass ? "合格" : "不合格", "判定结果");
            }

            string message = $"检测值: {actualValue:F3}, 范围: [{lowerLimit:F3}, {upperLimit:F3}], " +
                           $"判定: {(isPass ? "合格" : "不合格")}";

            Logger?.LogInformation(message);

            if (!isPass && param.StopOnFail)
            {
                return StepExecutionResult.Failed($"检测不合格: {message}");
            }

            return StepExecutionResult.Succeeded(message, new
            {
                ActualValue = actualValue,
                UpperLimit = upperLimit,
                LowerLimit = lowerLimit,
                IsPass = isPass
            });
        }
    }

    #endregion

    #region Break/Continue 执行器

    /// <summary>
    /// Break 执行器 - 跳出循环
    /// </summary>
    public class BreakExecutor : BaseStepExecutor
    {
        public override string StepType => "跳出循环";

        public BreakExecutor(ILogger<BreakExecutor> logger) : base(logger) { }

        protected override Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            if (!context.IsInLoop)
            {
                Logger?.LogWarning("Break 指令不在循环中使用");
                return Task.FromResult(StepExecutionResult.Succeeded("不在循环中，跳过"));
            }

            Logger?.LogInformation("执行 Break，跳出当前循环");
            return Task.FromResult(StepExecutionResult.Break("跳出循环"));
        }
    }

    /// <summary>
    /// Continue 执行器 - 继续下一次循环
    /// </summary>
    public class ContinueExecutor : BaseStepExecutor
    {
        public override string StepType => "继续循环";

        public ContinueExecutor(ILogger<ContinueExecutor> logger) : base(logger) { }

        protected override Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            if (!context.IsInLoop)
            {
                Logger?.LogWarning("Continue 指令不在循环中使用");
                return Task.FromResult(StepExecutionResult.Succeeded("不在循环中，跳过"));
            }

            Logger?.LogInformation("执行 Continue，继续下一次循环");
            return Task.FromResult(StepExecutionResult.Continue("继续下一次循环"));
        }
    }

    #endregion

    #region 步骤类型信息提供者

    /// <summary>
    /// 步骤类型信息提供者
    /// </summary>
    public class StepTypeInfoProvider
    {
        private static readonly List<StepTypeInfo> _stepTypes = new()
        {
            // 逻辑控制
            new StepTypeInfo { Name = "延时等待", DisplayName = "延时等待", Category = "Logic", IconKey = "⏱", Description = "等待指定时间" },
            new StepTypeInfo { Name = "消息通知", DisplayName = "消息通知", Category = "Logic", IconKey = "💬", Description = "显示消息提示" },
            new StepTypeInfo { Name = "等待稳定", DisplayName = "等待稳定", Category = "Logic", IconKey = "⚖", Description = "等待数值稳定" },

            // 条件判断
            new StepTypeInfo { Name = "条件判断", DisplayName = "条件判断", Category = "Condition", IconKey = "❓", Description = "根据条件执行不同分支" },

            // 循环控制
            new StepTypeInfo { Name = "循环工具", DisplayName = "循环工具", Category = "Loop", IconKey = "🔄", Description = "循环执行子步骤" },
            new StepTypeInfo { Name = "跳出循环", DisplayName = "跳出循环", Category = "Loop", IconKey = "⏹", Description = "跳出当前循环" },
            new StepTypeInfo { Name = "继续循环", DisplayName = "继续循环", Category = "Loop", IconKey = "⏭", Description = "跳过本次继续下一次循环" },

            // 变量操作
            new StepTypeInfo { Name = "变量定义", DisplayName = "变量定义", Category = "Variable", IconKey = "📝", Description = "定义新变量" },
            new StepTypeInfo { Name = "变量赋值", DisplayName = "变量赋值", Category = "Variable", IconKey = "✏", Description = "给变量赋值" },

            // 通信操作
            new StepTypeInfo { Name = "读取PLC", DisplayName = "读取PLC", Category = "Communication", IconKey = "📥", Description = "从PLC读取数据" },
            new StepTypeInfo { Name = "写入PLC", DisplayName = "写入PLC", Category = "Communication", IconKey = "📤", Description = "向PLC写入数据" },

            // 报表操作
            new StepTypeInfo { Name = "读取单元格", DisplayName = "读取单元格", Category = "Report", IconKey = "📊", Description = "从Excel读取数据" },
            new StepTypeInfo { Name = "写入单元格", DisplayName = "写入单元格", Category = "Report", IconKey = "📋", Description = "向Excel写入数据" },

            // 监控操作
            new StepTypeInfo { Name = "实时监控", DisplayName = "实时监控", Category = "Monitor", IconKey = "👁", Description = "实时监控数据变化" },
            new StepTypeInfo { Name = "检测判定", DisplayName = "检测判定", Category = "Monitor", IconKey = "✅", Description = "检测数据并判定合格性" }
        };

        /// <summary>
        /// 获取所有步骤类型
        /// </summary>
        public static IEnumerable<StepTypeInfo> GetAllStepTypes() => _stepTypes;

        /// <summary>
        /// 获取指定名称的步骤类型信息
        /// </summary>
        public static StepTypeInfo GetStepType(string name)
            => _stepTypes.FirstOrDefault(s => s.Name == name);
    }

    #endregion
}
