using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MainUI.UniversalPlatform.Infrastructure.Execution.Executors
{
    #region PLC读取执行器

    /// <summary>
    /// PLC读取参数
    /// </summary>
    public class PLCReadParameter
    {
        /// <summary>
        /// 读取项列表
        /// </summary>
        public List<PLCReadItem> Items { get; set; } = new();
    }

    /// <summary>
    /// PLC读取项
    /// </summary>
    public class PLCReadItem
    {
        /// <summary>
        /// PLC模块名
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// PLC点位名
        /// </summary>
        public string TagName { get; set; }

        /// <summary>
        /// 目标变量名
        /// </summary>
        public string TargetVariable { get; set; }
    }

    /// <summary>
    /// PLC读取执行器
    /// </summary>
    public class PLCReadExecutor : BaseStepExecutor
    {
        private readonly IPLCAdapter _plcAdapter;
        private readonly IVariableService _variableService;

        public override string StepType => "读取PLC";

        public PLCReadExecutor(
            IPLCAdapter plcAdapter,
            IVariableService variableService,
            ILogger<PLCReadExecutor> logger) : base(logger)
        {
            _plcAdapter = plcAdapter ?? throw new ArgumentNullException(nameof(plcAdapter));
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
        }

        public override ValidationResult ValidateParameter(object parameter)
        {
            var param = GetParameter<PLCReadParameter>(parameter);

            if (param.Items == null || param.Items.Count == 0)
                return ValidationResult.Invalid("PLC读取项不能为空");

            foreach (var item in param.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ModuleName))
                    return ValidationResult.Invalid("PLC模块名不能为空");
                if (string.IsNullOrWhiteSpace(item.TagName))
                    return ValidationResult.Invalid("PLC点位名不能为空");
                if (string.IsNullOrWhiteSpace(item.TargetVariable))
                    return ValidationResult.Invalid("目标变量名不能为空");
            }

            return ValidationResult.Valid();
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<PLCReadParameter>(parameter);
            var results = new Dictionary<string, object>();
            var errors = new List<string>();

            foreach (var item in param.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    Logger?.LogDebug("读取PLC: {Module}.{Tag} → {Variable}",
                        item.ModuleName, item.TagName, item.TargetVariable);

                    var result = await _plcAdapter.ReadAsync(item.ModuleName, item.TagName, cancellationToken);

                    if (result.Success)
                    {
                        _variableService.SetVariable(item.TargetVariable, result.Value,
                            $"PLC读取[{item.ModuleName}.{item.TagName}]", context.StepIndex);
                        results[item.TargetVariable] = result.Value;

                        Logger?.LogDebug("PLC读取成功: {Variable} = {Value}", item.TargetVariable, result.Value);
                    }
                    else
                    {
                        errors.Add($"{item.ModuleName}.{item.TagName}: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{item.ModuleName}.{item.TagName}: {ex.Message}");
                    Logger?.LogError(ex, "PLC读取异常: {Module}.{Tag}", item.ModuleName, item.TagName);
                }
            }

            if (errors.Count > 0)
            {
                return StepExecutionResult.Failed($"部分PLC读取失败: {string.Join("; ", errors)}");
            }

            return StepExecutionResult.Succeeded($"成功读取 {results.Count} 个PLC点位", results);
        }
    }

    #endregion

    #region PLC写入执行器

    /// <summary>
    /// PLC写入参数
    /// </summary>
    public class PLCWriteParameter
    {
        /// <summary>
        /// 写入项列表
        /// </summary>
        public List<PLCWriteItem> Items { get; set; } = new();
    }

    /// <summary>
    /// PLC写入项
    /// </summary>
    public class PLCWriteItem
    {
        /// <summary>
        /// PLC模块名
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// PLC点位名
        /// </summary>
        public string TagName { get; set; }

        /// <summary>
        /// 写入值表达式
        /// </summary>
        public string ValueExpression { get; set; }

        /// <summary>
        /// 直接写入值
        /// </summary>
        public object DirectValue { get; set; }
    }

    /// <summary>
    /// PLC写入执行器
    /// </summary>
    public class PLCWriteExecutor : BaseStepExecutor
    {
        private readonly IPLCAdapter _plcAdapter;
        private readonly IExpressionEvaluator _expressionEvaluator;

        public override string StepType => "写入PLC";

        public PLCWriteExecutor(
            IPLCAdapter plcAdapter,
            IExpressionEvaluator expressionEvaluator,
            ILogger<PLCWriteExecutor> logger) : base(logger)
        {
            _plcAdapter = plcAdapter ?? throw new ArgumentNullException(nameof(plcAdapter));
            _expressionEvaluator = expressionEvaluator;
        }

        public override ValidationResult ValidateParameter(object parameter)
        {
            var param = GetParameter<PLCWriteParameter>(parameter);

            if (param.Items == null || param.Items.Count == 0)
                return ValidationResult.Invalid("PLC写入项不能为空");

            return ValidationResult.Valid();
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<PLCWriteParameter>(parameter);
            var errors = new List<string>();
            int successCount = 0;

            foreach (var item in param.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // 获取要写入的值
                    object value = item.DirectValue;
                    if (!string.IsNullOrWhiteSpace(item.ValueExpression) && _expressionEvaluator != null)
                    {
                        var evalResult = await _expressionEvaluator.EvaluateAsync(item.ValueExpression, cancellationToken);
                        if (evalResult.Success)
                            value = evalResult.Result;
                    }

                    Logger?.LogDebug("写入PLC: {Module}.{Tag} = {Value}",
                        item.ModuleName, item.TagName, value);

                    var result = await _plcAdapter.WriteAsync(item.ModuleName, item.TagName, value, cancellationToken);

                    if (result.Success)
                    {
                        successCount++;
                        Logger?.LogDebug("PLC写入成功");
                    }
                    else
                    {
                        errors.Add($"{item.ModuleName}.{item.TagName}: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{item.ModuleName}.{item.TagName}: {ex.Message}");
                    Logger?.LogError(ex, "PLC写入异常");
                }
            }

            if (errors.Count > 0)
            {
                return StepExecutionResult.Failed($"部分PLC写入失败: {string.Join("; ", errors)}");
            }

            return StepExecutionResult.Succeeded($"成功写入 {successCount} 个PLC点位");
        }
    }

    #endregion

    #region 读取单元格执行器

    /// <summary>
    /// 读取单元格参数
    /// </summary>
    public class ReadCellParameter
    {
        /// <summary>
        /// 读取项列表
        /// </summary>
        public List<CellReadItem> Items { get; set; } = new();
    }

    /// <summary>
    /// 单元格读取项
    /// </summary>
    public class CellReadItem
    {
        /// <summary>
        /// 工作表名
        /// </summary>
        public string SheetName { get; set; }

        /// <summary>
        /// 单元格地址（如 A1, B2）
        /// </summary>
        public string CellAddress { get; set; }

        /// <summary>
        /// 目标变量名
        /// </summary>
        public string TargetVariable { get; set; }
    }

    /// <summary>
    /// 读取单元格执行器
    /// </summary>
    public class ReadCellExecutor : BaseStepExecutor
    {
        private readonly IReportService _reportService;
        private readonly IVariableService _variableService;

        public override string StepType => "读取单元格";

        public ReadCellExecutor(
            IReportService reportService,
            IVariableService variableService,
            ILogger<ReadCellExecutor> logger) : base(logger)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<ReadCellParameter>(parameter);
            var results = new Dictionary<string, object>();

            foreach (var item in param.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var value = await _reportService.ReadCellAsync(item.SheetName, item.CellAddress, cancellationToken);
                _variableService.SetVariable(item.TargetVariable, value, $"单元格[{item.SheetName}!{item.CellAddress}]");
                results[item.TargetVariable] = value;

                Logger?.LogDebug("读取单元格: {Sheet}!{Cell} = {Value} → {Variable}",
                    item.SheetName, item.CellAddress, value, item.TargetVariable);
            }

            return StepExecutionResult.Succeeded($"成功读取 {results.Count} 个单元格", results);
        }
    }

    #endregion

    #region 写入单元格执行器

    /// <summary>
    /// 写入单元格参数
    /// </summary>
    public class WriteCellParameter
    {
        /// <summary>
        /// 写入项列表
        /// </summary>
        public List<CellWriteItem> Items { get; set; } = new();
    }

    /// <summary>
    /// 单元格写入项
    /// </summary>
    public class CellWriteItem
    {
        /// <summary>
        /// 工作表名
        /// </summary>
        public string SheetName { get; set; }

        /// <summary>
        /// 单元格地址
        /// </summary>
        public string CellAddress { get; set; }

        /// <summary>
        /// 值表达式
        /// </summary>
        public string ValueExpression { get; set; }

        /// <summary>
        /// 直接值
        /// </summary>
        public object DirectValue { get; set; }
    }

    /// <summary>
    /// 写入单元格执行器
    /// </summary>
    public class WriteCellExecutor : BaseStepExecutor
    {
        private readonly IReportService _reportService;
        private readonly IExpressionEvaluator _expressionEvaluator;

        public override string StepType => "写入单元格";

        public WriteCellExecutor(
            IReportService reportService,
            IExpressionEvaluator expressionEvaluator,
            ILogger<WriteCellExecutor> logger) : base(logger)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _expressionEvaluator = expressionEvaluator;
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<WriteCellParameter>(parameter);
            int successCount = 0;

            foreach (var item in param.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 获取要写入的值
                object value = item.DirectValue;
                if (!string.IsNullOrWhiteSpace(item.ValueExpression) && _expressionEvaluator != null)
                {
                    var evalResult = await _expressionEvaluator.EvaluateAsync(item.ValueExpression, cancellationToken);
                    if (evalResult.Success)
                        value = evalResult.Result;
                }

                await _reportService.WriteCellAsync(item.SheetName, item.CellAddress, value, cancellationToken);
                successCount++;

                Logger?.LogDebug("写入单元格: {Sheet}!{Cell} = {Value}",
                    item.SheetName, item.CellAddress, value);
            }

            return StepExecutionResult.Succeeded($"成功写入 {successCount} 个单元格");
        }
    }

    #endregion

    #region 等待稳定执行器

    /// <summary>
    /// 等待稳定参数
    /// </summary>
    public class WaitStableParameter
    {
        /// <summary>
        /// 监控表达式
        /// </summary>
        public string MonitorExpression { get; set; }

        /// <summary>
        /// 稳定阈值
        /// </summary>
        public double Threshold { get; set; } = 0.01;

        /// <summary>
        /// 稳定持续时间（秒）
        /// </summary>
        public double StableDuration { get; set; } = 3;

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public double Timeout { get; set; } = 60;

        /// <summary>
        /// 采样间隔（毫秒）
        /// </summary>
        public int SampleInterval { get; set; } = 500;
    }

    /// <summary>
    /// 等待稳定执行器
    /// </summary>
    public class WaitStableExecutor : BaseStepExecutor
    {
        private readonly IExpressionEvaluator _expressionEvaluator;

        public override string StepType => "等待稳定";

        public WaitStableExecutor(
            IExpressionEvaluator expressionEvaluator,
            ILogger<WaitStableExecutor> logger) : base(logger)
        {
            _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<WaitStableParameter>(parameter);

            Logger?.LogInformation("开始等待稳定: 阈值={Threshold}, 持续={Duration}s, 超时={Timeout}s",
                param.Threshold, param.StableDuration, param.Timeout);

            var startTime = DateTime.Now;
            var stableStartTime = DateTime.MinValue;
            double? lastValue = null;

            while ((DateTime.Now - startTime).TotalSeconds < param.Timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 获取当前值
                var evalResult = await _expressionEvaluator.EvaluateAsync(param.MonitorExpression, cancellationToken);
                if (!evalResult.Success)
                {
                    return StepExecutionResult.Failed($"表达式计算失败: {evalResult.Error}");
                }

                double currentValue = Convert.ToDouble(evalResult.Result);

                // 检查是否稳定
                if (lastValue.HasValue)
                {
                    double change = Math.Abs(currentValue - lastValue.Value);

                    if (change <= param.Threshold)
                    {
                        if (stableStartTime == DateTime.MinValue)
                            stableStartTime = DateTime.Now;

                        var stableDuration = (DateTime.Now - stableStartTime).TotalSeconds;
                        if (stableDuration >= param.StableDuration)
                        {
                            Logger?.LogInformation("已达到稳定状态，当前值: {Value}", currentValue);
                            return StepExecutionResult.Succeeded($"稳定值: {currentValue}", currentValue);
                        }
                    }
                    else
                    {
                        stableStartTime = DateTime.MinValue;
                    }
                }

                lastValue = currentValue;
                await Task.Delay(param.SampleInterval, cancellationToken);
            }

            return StepExecutionResult.Failed($"等待稳定超时（{param.Timeout}秒）");
        }
    }

    #endregion

    #region 消息通知执行器

    /// <summary>
    /// 消息通知参数
    /// </summary>
    public class MessageParameter
    {
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public MessageType Type { get; set; } = MessageType.Info;

        /// <summary>
        /// 是否需要确认
        /// </summary>
        public bool RequireConfirm { get; set; }
    }

    /// <summary>
    /// 消息类型
    /// </summary>
    public enum MessageType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// 消息通知执行器
    /// </summary>
    public class MessageExecutor : BaseStepExecutor
    {
        private readonly IMessageService _messageService;

        public override string StepType => "消息通知";

        public MessageExecutor(
            IMessageService messageService,
            ILogger<MessageExecutor> logger) : base(logger)
        {
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<MessageParameter>(parameter);

            if (param.RequireConfirm)
            {
                var confirmed = await _messageService.ShowConfirmAsync(param.Message, param.Type);
                if (!confirmed)
                {
                    return StepExecutionResult.Failed("用户取消操作");
                }
            }
            else
            {
                _messageService.Show(param.Message, param.Type);
            }

            return StepExecutionResult.Succeeded("消息已显示");
        }
    }

    #endregion

    #region 服务接口

    /// <summary>
    /// 报表服务接口
    /// </summary>
    public interface IReportService
    {
        Task<object> ReadCellAsync(string sheetName, string cellAddress, CancellationToken cancellationToken = default);
        Task WriteCellAsync(string sheetName, string cellAddress, object value, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 消息服务接口
    /// </summary>
    public interface IMessageService
    {
        void Show(string message, MessageType type = MessageType.Info);
        Task<bool> ShowConfirmAsync(string message, MessageType type = MessageType.Info);
    }

    #endregion
}
