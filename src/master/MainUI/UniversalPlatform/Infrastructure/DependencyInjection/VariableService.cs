using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Core.Domain.Variables;

namespace MainUI.UniversalPlatform.Infrastructure.DependencyInjection
{
    /// <summary>
    /// 变量服务实现
    /// </summary>
    public class VariableService : IVariableService
    {
        private readonly Dictionary<string, Variable> _variables = new();
        private readonly object _lock = new();

        public event Action<Variable> VariableChanged;
        public event Action<Variable> VariableAdded;
        public event Action<string> VariableRemoved;

        public Variable GetVariable(string name)
        {
            lock (_lock)
            {
                return _variables.TryGetValue(name, out var v) ? v : null;
            }
        }

        public T GetValue<T>(string name)
        {
            var variable = GetVariable(name);
            return variable != null ? variable.GetValue<T>() : default;
        }

        public void SetVariable(string name, object value, string source = null, int? stepIndex = null)
        {
            lock (_lock)
            {
                if (_variables.TryGetValue(name, out var variable))
                {
                    variable.SetValue(value, source, stepIndex);
                    VariableChanged?.Invoke(variable);
                }
            }
        }

        public void AddVariable(Variable variable)
        {
            lock (_lock)
            {
                _variables[variable.Name] = variable;
                VariableAdded?.Invoke(variable);
            }
        }

        public bool RemoveVariable(string name)
        {
            lock (_lock)
            {
                if (_variables.Remove(name))
                {
                    VariableRemoved?.Invoke(name);
                    return true;
                }
                return false;
            }
        }

        public IEnumerable<Variable> GetAllVariables()
        {
            lock (_lock)
            {
                return _variables.Values.ToList();
            }
        }

        public IEnumerable<Variable> GetUserVariables()
        {
            return GetAllVariables().Where(v => !v.IsSystem);
        }

        public IEnumerable<Variable> GetSystemVariables()
        {
            return GetAllVariables().Where(v => v.IsSystem);
        }

        public void ClearUserVariables()
        {
            lock (_lock)
            {
                var toRemove = _variables.Where(kv => !kv.Value.IsSystem).Select(kv => kv.Key).ToList();
                foreach (var name in toRemove)
                {
                    _variables.Remove(name);
                    VariableRemoved?.Invoke(name);
                }
            }
        }

        public bool Exists(string name)
        {
            lock (_lock)
            {
                return _variables.ContainsKey(name);
            }
        }
    }
}
