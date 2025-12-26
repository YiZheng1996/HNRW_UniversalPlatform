using System;
using System.Windows.Forms;

namespace MainUI.UniversalPlatform.UI.WorkflowDesigner.Forms.Base
{
    /// <summary>
    /// 参数表单接口 - 定义所有步骤配置表单的统一契约
    /// </summary>
    public interface IParameterForm : IDisposable
    {
        #region 属性

        /// <summary>
        /// 步骤类型名称
        /// </summary>
        string StepType { get; }

        /// <summary>
        /// 返回的参数对象
        /// </summary>
        object ResultParameter { get; }

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        bool HasUnsavedChanges { get; }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        bool IsLoading { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 设置参数（加载到界面）
        /// </summary>
        /// <param name="parameter">参数对象</param>
        void SetParameter(object parameter);

        /// <summary>
        /// 获取参数（从界面收集）
        /// </summary>
        /// <returns>参数对象</returns>
        object GetParameter();

        /// <summary>
        /// 验证输入
        /// </summary>
        /// <returns>验证结果</returns>
        ParameterValidationResult Validate();

        /// <summary>
        /// 重置为默认值
        /// </summary>
        void ResetToDefault();

        /// <summary>
        /// 显示表单
        /// </summary>
        /// <returns>对话框结果</returns>
        DialogResult ShowDialog();

        /// <summary>
        /// 显示表单（指定父窗体）
        /// </summary>
        /// <param name="owner">父窗体</param>
        /// <returns>对话框结果</returns>
        DialogResult ShowDialog(IWin32Window owner);

        #endregion

        #region 事件

        /// <summary>
        /// 参数变更事件
        /// </summary>
        event EventHandler<ParameterChangedEventArgs> ParameterChanged;

        /// <summary>
        /// 验证完成事件
        /// </summary>
        event EventHandler<ValidationCompletedEventArgs> ValidationCompleted;

        #endregion
    }

    /// <summary>
    /// 泛型参数表单接口
    /// </summary>
    /// <typeparam name="TParameter">参数类型</typeparam>
    public interface IParameterForm<TParameter> : IParameterForm
        where TParameter : class, new()
    {
        /// <summary>
        /// 强类型参数
        /// </summary>
        new TParameter ResultParameter { get; }

        /// <summary>
        /// 设置强类型参数
        /// </summary>
        void SetParameter(TParameter parameter);

        /// <summary>
        /// 获取强类型参数
        /// </summary>
        new TParameter GetParameter();
    }

    #region 事件参数类

    /// <summary>
    /// 参数变更事件参数
    /// </summary>
    public class ParameterChangedEventArgs(string propertyName, object oldValue, object newValue) : EventArgs
    {
        /// <summary>
        /// 变更的属性名
        /// </summary>
        public string PropertyName { get; } = propertyName;

        /// <summary>
        /// 旧值
        /// </summary>
        public object OldValue { get; } = oldValue;

        /// <summary>
        /// 新值
        /// </summary>
        public object NewValue { get; } = newValue;
    }

    /// <summary>
    /// 验证完成事件参数
    /// </summary>
    public class ValidationCompletedEventArgs(ParameterValidationResult result) : EventArgs
    {
        /// <summary>
        /// 验证结果
        /// </summary>
        public ParameterValidationResult Result { get; } = result;
    }

    /// <summary>
    /// 参数验证结果
    /// </summary>
    public class ParameterValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// 错误消息列表
        /// </summary>
        public List<string> Errors { get; init; } = new();

        /// <summary>
        /// 警告消息列表
        /// </summary>
        public List<string> Warnings { get; init; } = new();

        /// <summary>
        /// 汇总消息
        /// </summary>
        public string Message => string.Join("; ", Errors);

        /// <summary>
        /// 创建有效结果
        /// </summary>
        public static ParameterValidationResult Valid() => new() { IsValid = true };

        /// <summary>
        /// 创建无效结果
        /// </summary>
        public static ParameterValidationResult Invalid(params string[] errors)
            => new() { IsValid = false, Errors = [.. errors] };

        /// <summary>
        /// 创建带警告的有效结果
        /// </summary>
        public static ParameterValidationResult ValidWithWarnings(params string[] warnings)
            => new() { IsValid = true, Warnings = warnings.ToList() };
    }

    #endregion
}
