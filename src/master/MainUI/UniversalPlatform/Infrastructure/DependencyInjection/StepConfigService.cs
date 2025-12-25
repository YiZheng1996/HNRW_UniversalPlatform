using MainUI.UniversalPlatform.Core.Application.Interfaces;

namespace MainUI.UniversalPlatform.Infrastructure.DependencyInjection
{
    /// <summary>
    /// 步骤配置服务实现（占位符）
    /// </summary>
    public class StepConfigService : IStepConfigService
    {
        public IEnumerable<StepTypeInfo> GetStepTypes() => throw new NotImplementedException();
        public StepTypeInfo GetStepType(string stepName) => throw new NotImplementedException();
        public object GetDefaultParameter(string stepName) => throw new NotImplementedException();
        public ValidationResultDto ValidateParameter(string stepName, object parameter) => ValidationResultDto.Valid();
        public string GetPreviewText(string stepName, object parameter) => stepName;
    }
}
