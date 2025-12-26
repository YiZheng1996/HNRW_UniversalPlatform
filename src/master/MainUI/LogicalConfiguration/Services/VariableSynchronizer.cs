using Microsoft.Extensions.Logging;

namespace MainUI.LogicalConfiguration.Services
{
    /// <summary>
    /// 变量同步协调器实现
    /// 
    /// 核心职责：
    /// 1. 协调运行时状态（WorkflowStateService）和持久化存储（JSON）的同步
    /// 2. 提供统一的变量操作API
    /// 3. 触发变量变更事件
    /// 
    /// 生命周期：Scoped（每个窗体实例一个）
    /// </summary>
    public class VariableSynchronizer : IVariableSynchronizer
    {
        #region 私有字段

        private readonly IWorkflowStateService _workflowState;
        private readonly IWorkflowConfigurationService _configService;
        private readonly ILogger<VariableSynchronizer> _logger;

        private bool _isInitialized;
        private bool _hasUnsavedChanges;

        #endregion

        #region 属性

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        public bool HasUnsavedChanges => _hasUnsavedChanges;

        #endregion

        #region 事件

        /// <summary>
        /// 变量变更事件
        /// </summary>
        public event EventHandler<VariableChangedEventArgs> VariableChanged;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public VariableSynchronizer(
            IWorkflowStateService workflowState,
            IWorkflowConfigurationService configService,
            ILogger<VariableSynchronizer> logger)
        {
            _workflowState = workflowState ?? throw new ArgumentNullException(nameof(workflowState));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 加载/持久化

        /// <summary>
        /// 从持久化存储加载变量到运行时状态
        /// </summary>
        public async Task LoadVariablesAsync()
        {
            try
            {
                _logger.LogInformation("开始从持久化存储加载变量");

                // 1. 从JSON加载
                var persistedVariables = await _configService.LoadVariablesAsync();

                // 2. 清空运行时用户变量（保留系统变量）
                _workflowState.ClearUserVariables();

                // 3. 转换并添加到运行时状态
                var loadedVariables = new List<VarItem_Enhanced>();
                foreach (var varItem in persistedVariables)
                {
                    var enhanced = ConvertToEnhanced(varItem);
                    _workflowState.AddVariable(enhanced);
                    loadedVariables.Add(enhanced);
                }

                _isInitialized = true;
                _hasUnsavedChanges = false;

                _logger.LogInformation("成功加载 {Count} 个变量", persistedVariables.Count);

                // 4. 触发批量加载事件
                OnVariableChanged(new VariableChangedEventArgs
                {
                    ChangeType = VariableChangeType.BatchLoaded,
                    Variables = loadedVariables.AsReadOnly()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载变量失败");
                throw;
            }
        }

        /// <summary>
        /// 将运行时变量持久化到存储
        /// </summary>
        public async Task PersistVariablesAsync()
        {
            try
            {
                _logger.LogInformation("开始持久化变量");

                // 1. 从运行时获取用户变量
                var userVariables = GetUserVariables();

                // 2. 转换为持久化格式
                var varItems = userVariables.Select(ConvertToVarItem).ToList();

                // 3. 保存到JSON
                await _configService.SaveVariablesAsync(varItems);

                _hasUnsavedChanges = false;

                _logger.LogInformation("成功持久化 {Count} 个变量", varItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "持久化变量失败");
                throw;
            }
        }

        #endregion

        #region 变量操作

        /// <summary>
        /// 添加或更新变量
        /// </summary>
        public async Task AddOrUpdateVariableAsync(VarItem_Enhanced variable, bool persistImmediately = false)
        {
            ArgumentNullException.ThrowIfNull(variable);

            var existing = _workflowState.FindVariableByName(variable.VarName);
            var isNew = existing == null;
            var oldValue = existing?.VarValue?.ToString();

            if (existing != null)
            {
                // 更新现有变量
                existing.VarType = variable.VarType;
                existing.VarValue = variable.VarValue;
                existing.VarText = variable.VarText;
                existing.LastUpdated = DateTime.Now;

                _logger.LogDebug("更新变量: {VarName}", variable.VarName);
            }
            else
            {
                // 添加新变量
                variable.LastUpdated = DateTime.Now;
                _workflowState.AddVariable(variable);

                _logger.LogDebug("添加变量: {VarName}", variable.VarName);
            }

            _hasUnsavedChanges = true;

            // 可选立即持久化
            if (persistImmediately)
            {
                await PersistVariablesAsync();
            }

            // 触发事件
            OnVariableChanged(new VariableChangedEventArgs
            {
                ChangeType = isNew ? VariableChangeType.Added : VariableChangeType.Updated,
                Variable = variable,
                OldValue = oldValue,
                NewValue = variable.VarValue?.ToString()
            });
        }

        /// <summary>
        /// 批量添加或更新变量
        /// </summary>
        public async Task AddOrUpdateVariablesAsync(IEnumerable<VarItem_Enhanced> variables, bool persistImmediately = false)
        {
            ArgumentNullException.ThrowIfNull(variables);

            var variableList = variables.ToList();
            if (variableList.Count == 0) return;

            foreach (var variable in variableList)
            {
                var existing = _workflowState.FindVariableByName(variable.VarName);

                if (existing != null)
                {
                    existing.VarType = variable.VarType;
                    existing.VarValue = variable.VarValue;
                    existing.VarText = variable.VarText;
                    existing.LastUpdated = DateTime.Now;
                }
                else
                {
                    variable.LastUpdated = DateTime.Now;
                    _workflowState.AddVariable(variable);
                }
            }

            _hasUnsavedChanges = true;

            _logger.LogDebug("批量更新 {Count} 个变量", variableList.Count);

            if (persistImmediately)
            {
                await PersistVariablesAsync();
            }

            // 触发批量更新事件
            OnVariableChanged(new VariableChangedEventArgs
            {
                ChangeType = VariableChangeType.BatchUpdated,
                Variables = variableList.AsReadOnly()
            });
        }

        /// <summary>
        /// 删除变量
        /// </summary>
        public async Task RemoveVariableAsync(string varName, bool persistImmediately = false)
        {
            var variable = _workflowState.FindVariableByName(varName);
            if (variable == null)
            {
                _logger.LogWarning("尝试删除不存在的变量: {VarName}", varName);
                return;
            }

            _workflowState.RemoveVariable(variable);
            _hasUnsavedChanges = true;

            _logger.LogDebug("删除变量: {VarName}", varName);

            if (persistImmediately)
            {
                await PersistVariablesAsync();
            }

            OnVariableChanged(new VariableChangedEventArgs
            {
                ChangeType = VariableChangeType.Removed,
                Variable = variable
            });
        }

        /// <summary>
        /// 更新变量值（仅更新值，不更新其他属性）
        /// </summary>
        public void UpdateVariableValue(string varName, object value, string source = null)
        {
            var variable = _workflowState.FindVariableByName(varName);
            if (variable == null)
            {
                _logger.LogWarning("尝试更新不存在的变量值: {VarName}", varName);
                return;
            }

            var oldValue = variable.VarValue?.ToString();
            variable.VarValue = value;
            variable.LastUpdated = DateTime.Now;

            // 注意：值变更不标记为需要持久化（运行时值通常不需要持久化）
            // 如果需要持久化，调用方应调用 PersistVariablesAsync

            _logger.LogDebug("变量值更新: {VarName} = {Value}", varName, value);

            OnVariableChanged(new VariableChangedEventArgs
            {
                ChangeType = VariableChangeType.ValueChanged,
                Variable = variable,
                OldValue = oldValue,
                NewValue = value?.ToString(),
                Source = source
            });
        }

        /// <summary>
        /// 清空所有用户变量
        /// </summary>
        public async Task ClearUserVariablesAsync(bool persistImmediately = false)
        {
            _workflowState.ClearUserVariables();
            _hasUnsavedChanges = true;

            _logger.LogInformation("清空所有用户变量");

            if (persistImmediately)
            {
                await PersistVariablesAsync();
            }

            OnVariableChanged(new VariableChangedEventArgs
            {
                ChangeType = VariableChangeType.Cleared
            });
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取所有用户变量
        /// </summary>
        public IReadOnlyList<VarItem_Enhanced> GetUserVariables()
        {
            return _workflowState.GetAllVariables()
                .Where(v => !v.IsSystemVariable)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// 获取所有变量
        /// </summary>
        public IReadOnlyList<VarItem_Enhanced> GetAllVariables()
        {
            return _workflowState.GetAllVariables().ToList().AsReadOnly();
        }

        /// <summary>
        /// 按名称查找变量
        /// </summary>
        public VarItem_Enhanced FindVariable(string varName)
        {
            if (string.IsNullOrEmpty(varName)) return null;
            return _workflowState.FindVariableByName(varName);
        }

        /// <summary>
        /// 检查变量是否存在
        /// </summary>
        public bool VariableExists(string varName)
        {
            return FindVariable(varName) != null;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将 VarItem 转换为 VarItem_Enhanced
        /// </summary>
        private static VarItem_Enhanced ConvertToEnhanced(VarItem varItem)
        {
            return new VarItem_Enhanced
            {
                VarName = varItem.VarName,
                VarType = varItem.VarType,
                VarValue = varItem.VarValue,
                VarText = varItem.VarText,
                LastUpdated = DateTime.Now,
                IsAssignedByStep = false,
                AssignedByStepIndex = -1,
                AssignmentType = VariableAssignmentType.None
            };
        }

        /// <summary>
        /// 将 VarItem_Enhanced 转换为 VarItem
        /// </summary>
        private static VarItem ConvertToVarItem(VarItem_Enhanced enhanced)
        {
            return new VarItem
            {
                VarName = enhanced.VarName,
                VarType = enhanced.VarType,
                VarValue = enhanced.VarValue?.ToString(),
                VarText = enhanced.VarText
            };
        }

        /// <summary>
        /// 触发变量变更事件
        /// </summary>
        private void OnVariableChanged(VariableChangedEventArgs args)
        {
            args.ChangedAt = DateTime.Now;
            VariableChanged?.Invoke(this, args);
        }

        #endregion
    }
}
