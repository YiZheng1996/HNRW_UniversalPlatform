namespace MainUI.UniversalPlatform.Core.Domain.Workflows
{
    /// <summary>
    /// 工作流聚合根
    /// 代表一个完整的测试工作流配置
    /// </summary>
    public class Workflow
    {
        #region 私有字段

        private readonly List<WorkflowStep> _steps = new();
        private readonly List<DomainEvent> _domainEvents = new();

        #endregion

        #region 属性

        /// <summary>
        /// 工作流唯一标识
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// 产品类型
        /// </summary>
        public string ModelType { get; private set; }

        /// <summary>
        /// 产品型号
        /// </summary>
        public string ModelName { get; private set; }

        /// <summary>
        /// 测试项名称
        /// </summary>
        public string ItemName { get; private set; }

        /// <summary>
        /// 工作流描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime ModifiedAt { get; private set; }

        /// <summary>
        /// 步骤列表（只读）
        /// </summary>
        public IReadOnlyList<WorkflowStep> Steps => _steps.AsReadOnly();

        /// <summary>
        /// 步骤数量
        /// </summary>
        public int StepCount => _steps.Count;

        /// <summary>
        /// 领域事件（只读）
        /// </summary>
        public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        #endregion

        #region 构造函数

        /// <summary>
        /// 私有构造函数，用于EF/反序列化
        /// </summary>
        private Workflow() { }

        /// <summary>
        /// 创建新工作流
        /// </summary>
        public Workflow(string modelType, string modelName, string itemName)
        {
            if (string.IsNullOrWhiteSpace(modelType))
                throw new ArgumentException("产品类型不能为空", nameof(modelType));
            if (string.IsNullOrWhiteSpace(modelName))
                throw new ArgumentException("产品型号不能为空", nameof(modelName));
            if (string.IsNullOrWhiteSpace(itemName))
                throw new ArgumentException("测试项名称不能为空", nameof(itemName));

            Id = GenerateId(modelType, modelName, itemName);
            ModelType = modelType;
            ModelName = modelName;
            ItemName = itemName;
            CreatedAt = DateTime.Now;
            ModifiedAt = CreatedAt;
        }

        #endregion

        #region 步骤管理方法

        /// <summary>
        /// 添加步骤
        /// </summary>
        public WorkflowStep AddStep(string stepName, object parameter = null, string remark = null)
        {
            var step = new WorkflowStep(
                stepNumber: _steps.Count + 1,
                stepName: stepName,
                parameter: parameter,
                remark: remark
            );

            _steps.Add(step);
            Touch();

            AddDomainEvent(new StepAddedEvent(Id, step));

            return step;
        }

        /// <summary>
        /// 在指定位置插入步骤
        /// </summary>
        public WorkflowStep InsertStep(int index, string stepName, object parameter = null, string remark = null)
        {
            if (index < 0 || index > _steps.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var step = new WorkflowStep(
                stepNumber: index + 1,
                stepName: stepName,
                parameter: parameter,
                remark: remark
            );

            _steps.Insert(index, step);
            RenumberSteps();
            Touch();

            AddDomainEvent(new StepAddedEvent(Id, step));

            return step;
        }

        /// <summary>
        /// 移除步骤
        /// </summary>
        public bool RemoveStep(int index)
        {
            if (index < 0 || index >= _steps.Count)
                return false;

            var step = _steps[index];
            _steps.RemoveAt(index);
            RenumberSteps();
            Touch();

            AddDomainEvent(new StepRemovedEvent(Id, step));

            return true;
        }

        /// <summary>
        /// 移动步骤
        /// </summary>
        public bool MoveStep(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _steps.Count)
                return false;
            if (toIndex < 0 || toIndex >= _steps.Count)
                return false;
            if (fromIndex == toIndex)
                return true;

            var step = _steps[fromIndex];
            _steps.RemoveAt(fromIndex);
            _steps.Insert(toIndex, step);
            RenumberSteps();
            Touch();

            return true;
        }

        /// <summary>
        /// 更新步骤参数
        /// </summary>
        public bool UpdateStepParameter(int index, object parameter)
        {
            if (index < 0 || index >= _steps.Count)
                return false;

            _steps[index].UpdateParameter(parameter);
            Touch();

            return true;
        }

        /// <summary>
        /// 获取步骤
        /// </summary>
        public WorkflowStep GetStep(int index)
        {
            if (index < 0 || index >= _steps.Count)
                return null;

            return _steps[index];
        }

        /// <summary>
        /// 清空所有步骤
        /// </summary>
        public void ClearSteps()
        {
            _steps.Clear();
            Touch();

            AddDomainEvent(new StepsClearedEvent(Id));
        }

        /// <summary>
        /// 批量设置步骤
        /// </summary>
        public void SetSteps(IEnumerable<WorkflowStep> steps)
        {
            _steps.Clear();
            int number = 1;
            foreach (var step in steps)
            {
                step.SetStepNumber(number++);
                _steps.Add(step);
            }
            Touch();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 重新编号步骤
        /// </summary>
        private void RenumberSteps()
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                _steps[i].SetStepNumber(i + 1);
            }
        }

        /// <summary>
        /// 更新修改时间
        /// </summary>
        private void Touch()
        {
            ModifiedAt = DateTime.Now;
        }

        /// <summary>
        /// 生成工作流ID
        /// </summary>
        private static string GenerateId(string modelType, string modelName, string itemName)
        {
            return $"{modelType}/{modelName}/{itemName}";
        }

        /// <summary>
        /// 添加领域事件
        /// </summary>
        private void AddDomainEvent(DomainEvent @event)
        {
            _domainEvents.Add(@event);
        }

        /// <summary>
        /// 清除领域事件
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        #endregion

        #region 工厂方法

        /// <summary>
        /// 从现有数据重建工作流（用于从存储加载）
        /// </summary>
        public static Workflow Reconstitute(
            string modelType,
            string modelName,
            string itemName,
            IEnumerable<WorkflowStep> steps,
            DateTime createdAt,
            DateTime modifiedAt)
        {
            var workflow = new Workflow
            {
                Id = GenerateId(modelType, modelName, itemName),
                ModelType = modelType,
                ModelName = modelName,
                ItemName = itemName,
                CreatedAt = createdAt,
                ModifiedAt = modifiedAt
            };

            if (steps != null)
            {
                workflow._steps.AddRange(steps);
            }

            return workflow;
        }

        #endregion
    }


    #region 领域事件

    /// <summary>
    /// 领域事件基类
    /// </summary>
    public abstract class DomainEvent
    {
        public DateTime OccurredAt { get; } = DateTime.Now;
    }

    /// <summary>
    /// 步骤添加事件
    /// </summary>
    public class StepAddedEvent : DomainEvent
    {
        public string WorkflowId { get; }
        public WorkflowStep Step { get; }

        public StepAddedEvent(string workflowId, WorkflowStep step)
        {
            WorkflowId = workflowId;
            Step = step;
        }
    }

    /// <summary>
    /// 步骤移除事件
    /// </summary>
    public class StepRemovedEvent : DomainEvent
    {
        public string WorkflowId { get; }
        public WorkflowStep Step { get; }

        public StepRemovedEvent(string workflowId, WorkflowStep step)
        {
            WorkflowId = workflowId;
            Step = step;
        }
    }

    /// <summary>
    /// 步骤清空事件
    /// </summary>
    public class StepsClearedEvent : DomainEvent
    {
        public string WorkflowId { get; }

        public StepsClearedEvent(string workflowId)
        {
            WorkflowId = workflowId;
        }
    }

    #endregion
}
