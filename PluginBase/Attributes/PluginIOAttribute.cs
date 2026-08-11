using System;

namespace PluginBase.Attributes
{
    /// <summary>
    /// 插件输入输出特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PluginIOAttribute : Attribute
    {
        public int InputCount { get; set; } = 1;
        public int OutputCount { get; set; } = 1;
        public bool AllowMultipleInputs { get; set; } = false;
        public bool AllowMultipleOutputs { get; set; } = false;
        public string InputType { get; set; } = "any";
        public string OutputType { get; set; } = "any";

        public PluginIOAttribute() { }

        public PluginIOAttribute(int inputs, int outputs)
        {
            InputCount = inputs;
            OutputCount = outputs;
        }
    }
}