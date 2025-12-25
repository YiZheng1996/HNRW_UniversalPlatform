using MainUI.LogicalConfiguration;
using MainUI.LogicalConfiguration.LogicalManager;
using MainUI.LogicalConfiguration.Services;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Core.Domain.Variables;
using MainUI.UniversalPlatform.Core.Domain.Workflows;

namespace MainUI.UniversalPlatform.Core.Compatibility
{
    #region 模型转换器

    /// <summary>
    /// 旧模型到新模型的转换器
    /// 用于渐进式迁移，新旧代码共存期间使用
    /// </summary>
    public static class LegacyModelAdapters
    {
        /// <summary>
        /// ChildModel → WorkflowStep
        /// </summary>
        public static WorkflowStep ToWorkflowStep(this ChildModel child, int index)
        {
            return WorkflowStep.Reconstitute(
                id: Guid.NewGuid(),
                stepNumber: child.StepNum > 0 ? child.StepNum : index + 1,
                stepName: child.StepName ?? "未知步骤",
                parameter: child.StepParameter,
                remark: child.Remark,
                status: (StepStatus)child.Status,
                errorMessage: child.ErrorMessage
            );
        }

        /// <summary>
        /// WorkflowStep → ChildModel
        /// </summary>
        public static ChildModel ToChildModel(this WorkflowStep step)
        {
            return new ChildModel
            {
                StepNum = step.StepNumber,
                StepName = step.StepName,
                StepParameter = step.Parameter,
                Remark = step.Remark,
                Status = (int)step.Status,
                ErrorMessage = step.ErrorMessage
            };
        }

        /// <summary>
        /// List<ChildModel> → Workflow
        /// </summary>
        public static Workflow ToWorkflow(
            this List<ChildModel> children,
            string modelType,
            string modelName,
            string itemName)
        {
            var steps = children.Select((c, i) => c.ToWorkflowStep(i)).ToList();
            return Workflow.Reconstitute(
                modelType: modelType,
                modelName: modelName,
                itemName: itemName,
                steps: steps,
                createdAt: DateTime.Now,
                modifiedAt: DateTime.Now
            );
        }

        /// <summary>
        /// Workflow → List<ChildModel>
        /// </summary>
        public static List<ChildModel> ToChildModels(this Workflow workflow)
        {
            return workflow.Steps.Select(s => s.ToChildModel()).ToList();
        }

        /// <summary>
        /// VarItem → Variable
        /// </summary>
        public static Variable ToVariable(this VarItem varItem)
        {
            var type = VariableTypeExtensions.ParseVariableType(varItem.VarType);
            return Variable.Reconstitute(
                name: varItem.VarName,
                type: type,
                value: varItem.VarValue,
                displayText: varItem.VarText,
                scope: VariableScope.Workflow,
                isSystem: false,
                lastUpdated: DateTime.Now
            );
        }

        /// <summary>
        /// Variable → VarItem
        /// </summary>
        public static VarItem ToVarItem(this Variable variable)
        {
            return new VarItem
            {
                VarName = variable.Name,
                VarType = variable.Type.ToTypeString(),
                VarValue = variable.Value,
                VarText = variable.DisplayText
            };
        }

        /// <summary>
        /// VarItem_Enhanced → Variable
        /// </summary>
        public static Variable ToVariable(this VarItem_Enhanced varItem)
        {
            var type = VariableTypeExtensions.ParseVariableType(varItem.VarType);
            return Variable.Reconstitute(
                name: varItem.VarName,
                type: type,
                value: varItem.VarValue,
                displayText: varItem.VarText,
                scope: varItem.IsSystemVariable ? VariableScope.Global : VariableScope.Workflow,
                isSystem: varItem.IsSystemVariable,
                lastUpdated: varItem.LastUpdated
            );
        }

        /// <summary>
        /// Variable → VarItem_Enhanced
        /// </summary>
        public static VarItem_Enhanced ToVarItemEnhanced(this Variable variable)
        {
            return new VarItem_Enhanced
            {
                VarName = variable.Name,
                VarType = variable.Type.ToTypeString(),
                VarValue = variable.Value,
                VarText = variable.DisplayText,
                IsSystemVariable = variable.IsSystem,
                LastUpdated = variable.LastUpdated
            };
        }
    }

    #endregion

    #region 服务适配器

    /// <summary>
    /// IWorkflowStateService 适配器
    /// 将旧的 IWorkflowStateService 接口适配到新的服务
    /// 
    /// 使用方式：在DI中注册此适配器替代原来的 WorkflowStateService
    /// services.AddSingleton<IWorkflowStateService, WorkflowStateServiceAdapter>();
    /// </summary>
    public class WorkflowStateServiceAdapter : IWorkflowStateService
    {
        private readonly IVariableService _variableService;
        private readonly List<ChildModel> _steps = new();
        private readonly object _lock = new();

        // 配置信息
        private string _modelType;
        private string _modelName;
        private string _itemName;
        private int _stepNum;
        private string _stepName;

        // 事件
        public event Action<ChildModel> StepAdded;
        public event Action<ChildModel> StepRemoved;
        public event Action StepsChanged;
        public event Action<object> VariableAdded;
        public event Action<object> VariableRemoved;
        public event Action<int> StepNumChanged;

        public WorkflowStateServiceAdapter(IVariableService variableService)
        {
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));

            // 订阅新服务的事件并转发
            _variableService.VariableAdded += v => VariableAdded?.Invoke(v.ToVarItemEnhanced());
            _variableService.VariableRemoved += name => VariableRemoved?.Invoke(name);
        }

        #region 配置属性

        public string ModelType
        {
            get => _modelType;
            set => _modelType = value;
        }

        public string ModelName
        {
            get => _modelName;
            set => _modelName = value;
        }

        public string ItemName
        {
            get => _itemName;
            set => _itemName = value;
        }

        public int StepNum
        {
            get => _stepNum;
            set => _stepNum = value;
        }

        public string StepName
        {
            get => _stepName;
            set => _stepName = value;
        }
        public string ModelTypeName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int T { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Status { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool ShouldBreakLoop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool ShouldContinueLoop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion

        #region 步骤管理

        public void AddStep(ChildModel step)
        {
            lock (_lock)
            {
                _steps.Add(step);
                StepAdded?.Invoke(step);
                StepsChanged?.Invoke();
            }
        }

        public void InsertStep(int index, ChildModel step)
        {
            lock (_lock)
            {
                if (index < 0) index = 0;
                if (index > _steps.Count) index = _steps.Count;

                _steps.Insert(index, step);
                RenumberSteps();
                StepAdded?.Invoke(step);
                StepsChanged?.Invoke();
            }
        }

        public void RemoveStep(ChildModel step)
        {
            lock (_lock)
            {
                if (_steps.Remove(step))
                {
                    RenumberSteps();
                    StepRemoved?.Invoke(step);
                    StepsChanged?.Invoke();
                }
            }
        }

        public void RemoveStepAt(int index)
        {
            lock (_lock)
            {
                if (index >= 0 && index < _steps.Count)
                {
                    var step = _steps[index];
                    _steps.RemoveAt(index);
                    RenumberSteps();
                    StepRemoved?.Invoke(step);
                    StepsChanged?.Invoke();
                }
            }
        }

        public void MoveStep(int fromIndex, int toIndex)
        {
            lock (_lock)
            {
                if (fromIndex < 0 || fromIndex >= _steps.Count) return;
                if (toIndex < 0 || toIndex >= _steps.Count) return;

                var step = _steps[fromIndex];
                _steps.RemoveAt(fromIndex);
                _steps.Insert(toIndex, step);
                RenumberSteps();
                StepsChanged?.Invoke();
            }
        }

        public List<ChildModel> GetSteps()
        {
            lock (_lock)
            {
                return _steps.ToList();
            }
        }

        public ChildModel GetStep(int index)
        {
            lock (_lock)
            {
                if (index >= 0 && index < _steps.Count)
                    return _steps[index];
                return null;
            }
        }

        public int GetStepCount()
        {
            lock (_lock)
            {
                return _steps.Count;
            }
        }

        public void ClearSteps()
        {
            lock (_lock)
            {
                _steps.Clear();
                StepsChanged?.Invoke();
            }
        }

        public void SetSteps(List<ChildModel> steps)
        {
            lock (_lock)
            {
                _steps.Clear();
                _steps.AddRange(steps);
                RenumberSteps();
                StepsChanged?.Invoke();
            }
        }

        private void RenumberSteps()
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                _steps[i].StepNum = i + 1;
            }
        }

        #endregion

        #region 变量管理（委托给新服务）

        public void AddVariable(object variable)
        {
            if (variable is VarItem_Enhanced enhanced)
            {
                _variableService.AddVariable(enhanced.ToVariable());
            }
            else if (variable is VarItem item)
            {
                _variableService.AddVariable(item.ToVariable());
            }
        }

        public void RemoveVariable(object variable)
        {
            string name = variable switch
            {
                VarItem_Enhanced e => e.VarName,
                VarItem v => v.VarName,
                string s => s,
                _ => null
            };

            if (!string.IsNullOrEmpty(name))
            {
                _variableService.RemoveVariable(name);
            }
        }

        public object GetVariable(string name)
        {
            var variable = _variableService.GetVariable(name);
            return variable?.ToVarItemEnhanced();
        }

        public List<object> GetAllVariables()
        {
            return _variableService.GetAllVariables()
                .Select(v => (object)v.ToVarItemEnhanced())
                .ToList();
        }

        public List<object> GetUserVariables()
        {
            return _variableService.GetUserVariables()
                .Select(v => (object)v.ToVarItemEnhanced())
                .ToList();
        }

        public void ClearUserVariables()
        {
            _variableService.ClearUserVariables();
        }

        public void SetVariableValue(string name, object value)
        {
            _variableService.SetVariable(name, value);
        }

        #endregion

        #region 配置管理

        public void UpdateConfiguration(string modelType, string modelName, string itemName)
        {
            _modelType = modelType;
            _modelName = modelName;
            _itemName = itemName;
        }

        bool IWorkflowStateService.RemoveStep(ChildModel step)
        {
            throw new NotImplementedException();
        }

        bool IWorkflowStateService.RemoveStepAt(int index)
        {
            throw new NotImplementedException();
        }

        public bool ValidateStepIndex(int stepIndex)
        {
            throw new NotImplementedException();
        }

        public void UpdateStepParameter(int stepIndex, object parameter)
        {
            throw new NotImplementedException();
        }

        bool IWorkflowStateService.RemoveVariable(object variable)
        {
            throw new NotImplementedException();
        }

        public void ClearAllVariables()
        {
            throw new NotImplementedException();
        }

        public List<T> GetVariables<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public T FindVariable<T>(Func<T, bool> predicate) where T : class
        {
            throw new NotImplementedException();
        }

        List<VarItem_Enhanced> IWorkflowStateService.GetAllVariables()
        {
            throw new NotImplementedException();
        }

        public VarItem_Enhanced FindVariableByName(string varName)
        {
            throw new NotImplementedException();
        }

        public void MarkVariableAssignedByStep(string varName, int stepIndex, string stepInfo, VariableAssignmentType assignmentType)
        {
            throw new NotImplementedException();
        }

        public void ClearVariableAssignmentMark(string varName, int stepIndex)
        {
            throw new NotImplementedException();
        }

        public GlobalVariableManager.VariableConflictInfo CheckVariableAssignmentConflict(string varName, int excludeStepIndex)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, GlobalVariableManager.VariableConflictInfo> CheckMultipleVariableConflicts(List<string> varNames, int excludeStepIndex)
        {
            throw new NotImplementedException();
        }

        public List<VarItem_Enhanced> GetVariablesAssignedByStep(int stepIndex)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public string GetDiagnosticInfo()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// GlobalVariableManager 适配器
    /// 将旧的 GlobalVariableManager 接口适配到新的 IVariableService
    /// </summary>
    public class GlobalVariableManagerAdapter
    {
        private readonly IVariableService _variableService;

        public GlobalVariableManagerAdapter(IVariableService variableService)
        {
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
        }

        /// <summary>
        /// 获取变量值
        /// </summary>
        public object GetVariableValue(string name)
        {
            return _variableService.GetVariable(name)?.Value;
        }

        /// <summary>
        /// 设置变量值
        /// </summary>
        public void SetVariableValue(string name, object value, string source = null)
        {
            _variableService.SetVariable(name, value, source);
        }

        /// <summary>
        /// 添加用户变量
        /// </summary>
        public void AddUserVariable(VarItem_Enhanced varItem)
        {
            _variableService.AddVariable(varItem.ToVariable());
        }

        /// <summary>
        /// 获取所有用户变量
        /// </summary>
        public List<VarItem_Enhanced> GetAllUserVariables()
        {
            return _variableService.GetUserVariables()
                .Select(v => v.ToVarItemEnhanced())
                .ToList();
        }

        /// <summary>
        /// 清除用户变量
        /// </summary>
        public void ClearUserVariables()
        {
            _variableService.ClearUserVariables();
        }

        /// <summary>
        /// 变量是否存在
        /// </summary>
        public bool VariableExists(string name)
        {
            return _variableService.Exists(name);
        }
    }

    #endregion

    #region 工厂类

    /// <summary>
    /// 兼容层工厂
    /// 提供创建适配器的便捷方法
    /// </summary>
    public static class CompatibilityFactory
    {
        /// <summary>
        /// 从新的 Workflow 创建旧的步骤列表
        /// </summary>
        public static List<ChildModel> CreateChildModels(Workflow workflow)
        {
            return workflow.ToChildModels();
        }

        /// <summary>
        /// 从旧的步骤列表创建新的 Workflow
        /// </summary>
        public static Workflow CreateWorkflow(
            List<ChildModel> steps,
            string modelType,
            string modelName,
            string itemName)
        {
            return steps.ToWorkflow(modelType, modelName, itemName);
        }

        /// <summary>
        /// 从新的 Variable 列表创建旧的 VarItem 列表
        /// </summary>
        public static List<VarItem> CreateVarItems(IEnumerable<Variable> variables)
        {
            return variables.Select(v => v.ToVarItem()).ToList();
        }

        /// <summary>
        /// 从旧的 VarItem 列表创建新的 Variable 列表
        /// </summary>
        public static List<Variable> CreateVariables(IEnumerable<VarItem> varItems)
        {
            return varItems.Select(v => v.ToVariable()).ToList();
        }
    }

    #endregion
}
