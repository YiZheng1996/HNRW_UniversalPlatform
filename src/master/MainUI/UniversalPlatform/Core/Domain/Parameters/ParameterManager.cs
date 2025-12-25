using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 参数管理器 - 提供参数类型转换和验证功能
    /// </summary>
    public static class ParameterManager
    {
        /// <summary>
        /// 尝试将参数转换为指定类型
        /// </summary>
        public static bool TryGetParameter<T>(object stepParameter, out T parameter) where T : class
        {
            parameter = null;

            if (stepParameter == null)
                return false;

            try
            {
                // 1. 直接是目标类型
                if (stepParameter is T directParam)
                {
                    parameter = directParam;
                    return true;
                }

                // 2. JSON字符串
                if (stepParameter is string jsonStr && !string.IsNullOrEmpty(jsonStr))
                {
                    parameter = JsonConvert.DeserializeObject<T>(jsonStr);
                    return parameter != null;
                }

                // 3. 其他对象，先序列化再反序列化
                var jsonString = JsonConvert.SerializeObject(stepParameter);
                parameter = JsonConvert.DeserializeObject<T>(jsonString);
                return parameter != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取参数，如果转换失败则返回默认实例
        /// </summary>
        public static T GetParameter<T>(object stepParameter) where T : class, new()
        {
            if (TryGetParameter<T>(stepParameter, out var param))
                return param;
            return new T();
        }

        /// <summary>
        /// 根据步骤类型获取参数类型
        /// </summary>
        public static Type GetParameterType(string stepName)
        {
            return stepName switch
            {
                "延时等待" => typeof(DelayParameter),
                "消息通知" => typeof(MessageParameter),
                "等待稳定" => typeof(WaitStableParameter),
                "变量定义" => typeof(DefineVariableParameter),
                "变量赋值" => typeof(AssignVariableParameter),
                "条件判断" => typeof(ConditionParameter),
                "循环工具" => typeof(LoopParameter),
                //"跳出循环" or "继续循环" => typeof(LoopControlParameter),
                "读取PLC" => typeof(PLCReadParameter),
                "写入PLC" => typeof(PLCWriteParameter),
                "检测判定" => typeof(DetectionParameter),
                "读取单元格" => typeof(ReadCellParameter),
                "写入单元格" => typeof(WriteCellParameter),
                "实时监控" => typeof(RealtimeMonitorParameter),
                _ => null
            };
        }

        /// <summary>
        /// 创建默认参数实例
        /// </summary>
        public static object CreateDefaultParameter(string stepName)
        {
            var type = GetParameterType(stepName);
            return type != null ? Activator.CreateInstance(type) : null;
        }
    }

}
