using System.IO;

namespace PluginBase.Interfaces
{
    /// <summary>
    /// 可序列化操作接口
    /// 插件的 Operation 类实现此接口后，支持将参数序列化为二进制（保存）和反序列化（加载）
    /// 序列化格式由各插件自行定义，建议在开头写入版本号以支持未来兼容
    /// </summary>
    public interface ISerializableOperation
    {
        /// <summary>
        /// 将插件参数序列化到二进制流（保存时调用）
        /// </summary>
        /// <param name="writer">二进制写入器</param>
        void Serialize(BinaryWriter writer);

        /// <summary>
        /// 从二进制流反序列化插件参数（加载时调用）
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        void Deserialize(BinaryReader reader);
    }
}