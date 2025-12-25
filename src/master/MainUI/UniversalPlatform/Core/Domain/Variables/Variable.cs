using System.ComponentModel;

namespace MainUI.UniversalPlatform.Core.Domain.Variables
{
    /// <summary>
    /// 变量实体
    /// 代表工作流中的一个变量
    /// </summary>
    public class Variable
    {
        #region 属性

        /// <summary>
        /// 变量名称（唯一标识）
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 变量类型
        /// </summary>
        public VariableType Type { get; private set; }

        /// <summary>
        /// 变量值
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// 变量显示文本（描述）
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// 变量作用域
        /// </summary>
        public VariableScope Scope { get; private set; }

        /// <summary>
        /// 是否为系统变量
        /// </summary>
        public bool IsSystem { get; private set; }

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; private set; }

        /// <summary>
        /// 赋值来源信息
        /// </summary>
        public AssignmentInfo Assignment { get; private set; }

        /// <summary>
        /// 历史记录
        /// </summary>
        public IReadOnlyList<VariableHistoryEntry> History => _history.AsReadOnly();

        private readonly List<VariableHistoryEntry> _history = new();

        #endregion

        #region 构造函数

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private Variable() { }

        /// <summary>
        /// 创建用户变量
        /// </summary>
        public static Variable CreateUser(string name, VariableType type, string displayText = null)
        {
            return new Variable
            {
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Type = type,
                DisplayText = displayText ?? name,
                Scope = VariableScope.Workflow,
                IsSystem = false,
                IsReadOnly = false,
                LastUpdated = DateTime.Now,
                Value = GetDefaultValue(type)
            };
        }

        /// <summary>
        /// 创建系统变量
        /// </summary>
        public static Variable CreateSystem(string name, VariableType type, object initialValue, string displayText = null)
        {
            return new Variable
            {
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Type = type,
                DisplayText = displayText ?? name,
                Scope = VariableScope.Global,
                IsSystem = true,
                IsReadOnly = false,
                LastUpdated = DateTime.Now,
                Value = initialValue
            };
        }

        #endregion

        #region 方法

        /// <summary>
        /// 设置变量值
        /// </summary>
        public void SetValue(object newValue, string source = null, int? stepIndex = null)
        {
            if (IsReadOnly)
                throw new InvalidOperationException($"变量 '{Name}' 是只读的，不能修改");

            var oldValue = Value;
            Value = ConvertValue(newValue, Type);
            LastUpdated = DateTime.Now;

            // 记录赋值信息
            if (!string.IsNullOrEmpty(source) || stepIndex.HasValue)
            {
                Assignment = new AssignmentInfo
                {
                    Source = source,
                    StepIndex = stepIndex,
                    Timestamp = LastUpdated
                };
            }

            // 记录历史
            _history.Add(new VariableHistoryEntry
            {
                OldValue = oldValue?.ToString(),
                NewValue = Value?.ToString(),
                Timestamp = LastUpdated,
                Source = source ?? "Unknown"
            });

            // 只保留最近20条历史记录
            while (_history.Count > 20)
            {
                _history.RemoveAt(0);
            }
        }

        /// <summary>
        /// 获取类型化的值
        /// </summary>
        public T GetValue<T>()
        {
            if (Value == null)
                return default;

            if (Value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(Value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// 获取字符串值
        /// </summary>
        public string GetStringValue()
        {
            return Value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 获取数值
        /// </summary>
        public double GetNumericValue()
        {
            if (Value == null) return 0;

            if (double.TryParse(Value.ToString(), out double result))
                return result;

            return 0;
        }

        /// <summary>
        /// 获取布尔值
        /// </summary>
        public bool GetBoolValue()
        {
            if (Value == null) return false;

            if (Value is bool b) return b;

            var str = Value.ToString().ToLower();
            return str == "true" || str == "1" || str == "yes";
        }

        /// <summary>
        /// 重置值为默认
        /// </summary>
        public void Reset()
        {
            Value = GetDefaultValue(Type);
            LastUpdated = DateTime.Now;
            Assignment = null;
        }

        /// <summary>
        /// 清除赋值信息
        /// </summary>
        public void ClearAssignment()
        {
            Assignment = null;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取类型的默认值
        /// </summary>
        private static object GetDefaultValue(VariableType type)
        {
            return type switch
            {
                VariableType.String => string.Empty,
                VariableType.Integer => 0,
                VariableType.Double => 0.0,
                VariableType.Boolean => false,
                VariableType.DateTime => DateTime.MinValue,
                _ => null
            };
        }

        /// <summary>
        /// 转换值到指定类型
        /// </summary>
        private static object ConvertValue(object value, VariableType type)
        {
            if (value == null) return GetDefaultValue(type);

            try
            {
                return type switch
                {
                    VariableType.String => value.ToString(),
                    VariableType.Integer => Convert.ToInt32(value),
                    VariableType.Double => Convert.ToDouble(value),
                    VariableType.Boolean => Convert.ToBoolean(value),
                    VariableType.DateTime => Convert.ToDateTime(value),
                    _ => value
                };
            }
            catch
            {
                return GetDefaultValue(type);
            }
        }

        #endregion

        #region 工厂方法

        /// <summary>
        /// 从现有数据重建变量
        /// </summary>
        public static Variable Reconstitute(
            string name,
            VariableType type,
            object value,
            string displayText,
            VariableScope scope,
            bool isSystem,
            DateTime lastUpdated)
        {
            return new Variable
            {
                Name = name,
                Type = type,
                Value = value,
                DisplayText = displayText,
                Scope = scope,
                IsSystem = isSystem,
                LastUpdated = lastUpdated
            };
        }

        #endregion
    }

    /// <summary>
    /// 变量类型枚举
    /// </summary>
    public enum VariableType
    {
        [Description("字符串")]
        String,

        [Description("整数")]
        Integer,

        [Description("浮点数")]
        Double,

        [Description("布尔值")]
        Boolean,

        [Description("日期时间")]
        DateTime,

        [Description("对象")]
        Object
    }

    /// <summary>
    /// 变量作用域枚举
    /// </summary>
    public enum VariableScope
    {
        /// <summary>全局作用域（跨工作流）</summary>
        Global,

        /// <summary>工作流作用域（单个工作流内）</summary>
        Workflow,

        /// <summary>步骤作用域（单个步骤内）</summary>
        Step
    }

    /// <summary>
    /// 赋值信息
    /// </summary>
    public class AssignmentInfo
    {
        /// <summary>
        /// 赋值来源
        /// </summary>
        public string Source { get; init; }

        /// <summary>
        /// 赋值步骤索引
        /// </summary>
        public int? StepIndex { get; init; }

        /// <summary>
        /// 赋值时间
        /// </summary>
        public DateTime Timestamp { get; init; }
    }

    /// <summary>
    /// 变量历史记录项
    /// </summary>
    public class VariableHistoryEntry
    {
        public string OldValue { get; init; }
        public string NewValue { get; init; }
        public DateTime Timestamp { get; init; }
        public string Source { get; init; }
    }

    /// <summary>
    /// 变量类型扩展方法
    /// </summary>
    public static class VariableTypeExtensions
    {
        /// <summary>
        /// 从字符串解析变量类型
        /// </summary>
        public static VariableType ParseVariableType(string typeString)
        {
            return typeString?.ToLower() switch
            {
                "string" or "str" => VariableType.String,
                "int" or "integer" => VariableType.Integer,
                "double" or "float" or "decimal" => VariableType.Double,
                "bool" or "boolean" => VariableType.Boolean,
                "datetime" or "date" => VariableType.DateTime,
                _ => VariableType.String
            };
        }

        /// <summary>
        /// 获取变量类型的字符串表示
        /// </summary>
        public static string ToTypeString(this VariableType type)
        {
            return type switch
            {
                VariableType.String => "string",
                VariableType.Integer => "int",
                VariableType.Double => "double",
                VariableType.Boolean => "bool",
                VariableType.DateTime => "datetime",
                _ => "object"
            };
        }
    }
}
