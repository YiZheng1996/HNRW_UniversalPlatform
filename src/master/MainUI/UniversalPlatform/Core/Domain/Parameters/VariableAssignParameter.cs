namespace MainUI.UniversalPlatform.Core.Domain.Parameters
{
    /// <summary>
    /// 变量赋值参数
    /// </summary>
    public class VariableAssignParameter
    {
        /// <summary>
        /// 目标变量名
        /// </summary>
        public string TargetVariable { get; set; }

        /// <summary>
        /// 赋值表达式
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// 赋值类型
        /// </summary>
        public AssignmentType AssignType { get; set; } = AssignmentType.Direct;

        /// <summary>
        /// 直接值（当AssignType为Direct时使用）
        /// </summary>
        public object DirectValue { get; set; }

        /// <summary>
        /// 源变量名（当AssignType为Variable时使用）
        /// </summary>
        public string SourceVariable { get; set; }

        /// <summary>
        /// PLC模块名（当AssignType为PLC时使用）
        /// </summary>
        public string PLCModule { get; set; }

        /// <summary>
        /// PLC点位名（当AssignType为PLC时使用）
        /// </summary>
        public string PLCTag { get; set; }
    }

    /// <summary>
    /// 赋值类型
    /// </summary>
    public enum AssignmentType
    {
        /// <summary>直接赋值</summary>
        Direct,

        /// <summary>表达式计算</summary>
        Expression,

        /// <summary>从变量复制</summary>
        Variable,

        /// <summary>从PLC读取</summary>
        PLC
    }

}
