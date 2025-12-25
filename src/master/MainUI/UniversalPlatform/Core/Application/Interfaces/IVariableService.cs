using MainUI.UniversalPlatform.Core.Domain.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainUI.UniversalPlatform.Core.Application.Interfaces
{
    /// <summary>
    /// 变量服务接口
    /// </summary>
    public interface IVariableService
    {
        /// <summary>
        /// 获取变量
        /// </summary>
        Variable GetVariable(string name);

        /// <summary>
        /// 获取变量值
        /// </summary>
        T GetValue<T>(string name);

        /// <summary>
        /// 设置变量值
        /// </summary>
        void SetVariable(string name, object value, string source = null, int? stepIndex = null);

        /// <summary>
        /// 添加变量
        /// </summary>
        void AddVariable(Variable variable);

        /// <summary>
        /// 删除变量
        /// </summary>
        bool RemoveVariable(string name);

        /// <summary>
        /// 获取所有变量
        /// </summary>
        IEnumerable<Variable> GetAllVariables();

        /// <summary>
        /// 获取用户变量
        /// </summary>
        IEnumerable<Variable> GetUserVariables();

        /// <summary>
        /// 获取系统变量
        /// </summary>
        IEnumerable<Variable> GetSystemVariables();

        /// <summary>
        /// 清除所有用户变量
        /// </summary>
        void ClearUserVariables();

        /// <summary>
        /// 变量是否存在
        /// </summary>
        bool Exists(string name);

        /// <summary>
        /// 变量变更事件
        /// </summary>
        event Action<Variable> VariableChanged;

        /// <summary>
        /// 变量添加事件
        /// </summary>
        event Action<Variable> VariableAdded;

        /// <summary>
        /// 变量删除事件
        /// </summary>
        event Action<string> VariableRemoved;
    }
}
