using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainUI.UniversalPlatform.Core.Application.Interfaces
{
    /// <summary>
    /// 步骤配置服务接口
    /// </summary>
    public interface IStepConfigService
    {
        /// <summary>
        /// 获取步骤类型列表
        /// </summary>
        IEnumerable<StepTypeInfo> GetStepTypes();

        /// <summary>
        /// 获取步骤类型信息
        /// </summary>
        StepTypeInfo GetStepType(string stepName);

        /// <summary>
        /// 获取步骤参数默认值
        /// </summary>
        object GetDefaultParameter(string stepName);

        /// <summary>
        /// 验证步骤参数
        /// </summary>
        ValidationResultDto ValidateParameter(string stepName, object parameter);

        /// <summary>
        /// 获取步骤预览文本
        /// </summary>
        string GetPreviewText(string stepName, object parameter);
    }
}
