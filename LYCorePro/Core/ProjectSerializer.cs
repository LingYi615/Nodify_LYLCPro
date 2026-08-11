using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Nodify;
using PluginBase.Interfaces;

namespace LYCorePro.Core
{
    /// <summary>
    /// 项目序列化器
    /// 将编辑器中所有节点参数和连线序列化为二进制文件
    /// 支持保存（Save）和加载（Load）操作
    /// </summary>
    public static class ProjectSerializer
    {
        /// <summary>文件头魔数（用于校验文件格式）</summary>
        private const uint FileMagic = 0x4C595350; // "LYSP" = LYCorePro Serialized Project

        /// <summary>当前文件格式版本（v4: 新增 IsEnabled 属性序列化）</summary>
        private const int FileVersion = 4;

        /// <summary>
        /// 保存所有操作节点和连线到二进制文件
        /// </summary>
        /// <param name="filePath">目标文件路径</param>
        /// <param name="operations">操作节点列表（每个节点的 Instance 需实现 ISerializableOperation）</param>
        /// <param name="connections">连线列表</param>
        public static void Save(string filePath, IList<OperationNodeViewModel> operations,
            IList<ConnectionViewModel> connections)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);

            // 写入文件头
            writer.Write(FileMagic);
            writer.Write(FileVersion);

            // 统计可序列化的节点数量
            var serializableCount = 0;
            foreach (var op in operations)
            {
                if (op.Instance is ISerializableOperation)
                    serializableCount++;
            }

            writer.Write(serializableCount);

            // 写入每个节点的数据
            foreach (var op in operations)
            {
                if (op.Instance is not ISerializableOperation serializable)
                    continue;

                var type = op.Instance.GetType();

                // 写入类型全名（不含程序集版本，用于跨版本兼容的反序列化）
                writer.Write(type.FullName ?? "Unknown");
                // 写入程序集名称（不含版本号，用于辅助定位）
                writer.Write(type.Assembly.GetName().Name ?? "");

                // 写入节点位置
                writer.Write(op.Location.X);
                writer.Write(op.Location.Y);

                // 写入节点标题
                writer.Write(op.Title ?? "");

                // 写入节点启用状态
                writer.Write(op.IsEnabled);

                // 写入节点参数（由插件自行序列化）
                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);
                serializable.Serialize(bw);
                bw.Flush();
                var data = ms.ToArray();
                writer.Write(data.Length);
                writer.Write(data);

                Debug.WriteLine($"[ProjectSerializer] 已序列化节点: {type.FullName}, Title={op.Title}");
            }

            // ===== 写入连线数据 =====
            // 只保存两端节点都是可序列化节点的连线
            // 支持一个输出连接多个输入（一对多）
            var serializableConnections = new List<(int sourceIdx, int srcConnIdx, int targetIdx, int tgtConnIdx, int order)>();

            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                if (op.Instance is not ISerializableOperation) continue;

                for (int j = 0; j < op.Outputs.Count; j++)
                {
                    var output = op.Outputs[j];
                    // 使用 Where 获取该输出连接的所有连线（支持一对多）
                    var allConns = connections.Where(c => c.Output == output).ToList();
                    if (allConns.Count == 0) continue;

                    foreach (var conn in allConns)
                    {
                        if (conn?.Input == null) continue;

                        // 查找目标节点和连接器索引
                        for (int k = 0; k < operations.Count; k++)
                        {
                            var targetOp = operations[k];
                            if (targetOp.Instance is not ISerializableOperation) continue;

                            for (int m = 0; m < targetOp.Inputs.Count; m++)
                            {
                                if (targetOp.Inputs[m] == conn.Input)
                                {
                                    serializableConnections.Add((i, j, k, m, conn.Order));
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            writer.Write(serializableConnections.Count);
            foreach (var (srcIdx, srcConnIdx, tgtIdx, tgtConnIdx, order) in serializableConnections)
            {
                writer.Write(srcIdx);
                writer.Write(srcConnIdx);
                writer.Write(tgtIdx);
                writer.Write(tgtConnIdx);
                writer.Write(order);
                Debug.WriteLine($"[ProjectSerializer] 已序列化连线: Node[{srcIdx}].Output[{srcConnIdx}] -> Node[{tgtIdx}].Input[{tgtConnIdx}], Order={order}");
            }

            Debug.WriteLine($"[ProjectSerializer] 保存完成: {serializableCount} 节点, {serializableConnections.Count} 连线");
        }

        /// <summary>
        /// 从二进制文件加载所有操作节点和连线
        /// </summary>
        /// <param name="filePath">源文件路径</param>
        /// <param name="pluginManager">插件管理器（用于创建节点实例）</param>
        /// <returns>包含节点列表和连线列表的元组</returns>
        public static (List<OperationNodeViewModel> Nodes, List<ConnectionViewModel> Connections)
            Load(string filePath, PluginManager pluginManager)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            // 校验文件头
            var magic = reader.ReadUInt32();
            if (magic != FileMagic)
                throw new InvalidDataException("文件格式不正确，无法加载");

            var version = reader.ReadInt32();
            Debug.WriteLine($"[ProjectSerializer] 文件版本: {version}");

            var count = reader.ReadInt32();
            var nodes = new List<OperationNodeViewModel>(count);

            for (int i = 0; i < count; i++)
            {
                string typeFullName;
                string assemblyName;

                if (version >= 2)
                {
                    // v2+: 分别存储 FullName 和 AssemblyName
                    typeFullName = reader.ReadString();
                    assemblyName = reader.ReadString();
                }
                else
                {
                    // v1: 存储的是 AssemblyQualifiedName（含版本号），需解析出 FullName 和 AssemblyName
                    var assemblyQualifiedName = reader.ReadString();
                    var parts = assemblyQualifiedName.Split(',');
                    typeFullName = parts.Length > 0 ? parts[0].Trim() : assemblyQualifiedName;
                    assemblyName = parts.Length > 1 ? parts[1].Trim() : "";
                    Debug.WriteLine($"[ProjectSerializer] v1 兼容: {assemblyQualifiedName} -> {typeFullName}, {assemblyName}");
                }
                var x = reader.ReadDouble();
                var y = reader.ReadDouble();
                var title = reader.ReadString();
                var isEnabled = version >= 4 ? reader.ReadBoolean() : true;

                var dataLength = reader.ReadInt32();
                var data = reader.ReadBytes(dataLength);

                Debug.WriteLine($"[ProjectSerializer] 加载节点: {typeFullName}, Assembly={assemblyName}");

                // 通过类型名查找插件并创建节点实例
                var nodeVm = CreateNodeFromTypeName(typeFullName, assemblyName, title, x, y, pluginManager);
                if (nodeVm?.Instance is ISerializableOperation serializable)
                {
                    // 反序列化节点参数
                    using var ms = new MemoryStream(data);
                    using var br = new BinaryReader(ms);
                    serializable.Deserialize(br);
                    Debug.WriteLine($"[ProjectSerializer] 已反序列化节点参数: {typeFullName}");
                }

                if (nodeVm != null)
                {
                    nodeVm.IsEnabled = isEnabled;
                    nodes.Add(nodeVm);
                }
                else
                    Debug.WriteLine($"[ProjectSerializer] 警告: 无法创建节点 {typeFullName}");
            }

            // ===== 读取连线数据（v2+） =====
            var connections = new List<ConnectionViewModel>();
            if (version >= 2)
            {
                var connectionCount = reader.ReadInt32();
                Debug.WriteLine($"[ProjectSerializer] 读取连线: {connectionCount} 条");

                // v2 旧文件没有 Order 数据，按读取顺序自动分配递增序号
                var orderMap = new Dictionary<object, int>(); // key: output connector, value: next order

                for (int i = 0; i < connectionCount; i++)
                {
                    var srcIdx = reader.ReadInt32();
                    var srcConnIdx = reader.ReadInt32();
                    var tgtIdx = reader.ReadInt32();
                    var tgtConnIdx = reader.ReadInt32();
                    var order = version >= 3 ? reader.ReadInt32() : 0;

                    if (srcIdx >= 0 && srcIdx < nodes.Count &&
                        tgtIdx >= 0 && tgtIdx < nodes.Count)
                    {
                        var srcNode = nodes[srcIdx];
                        var tgtNode = nodes[tgtIdx];

                        if (srcConnIdx < srcNode.Outputs.Count &&
                            tgtConnIdx < tgtNode.Inputs.Count)
                        {
                            var source = srcNode.Outputs[srcConnIdx];
                            var target = tgtNode.Inputs[tgtConnIdx];

                            // 验证数据类型兼容性
                            if (DataTypeHelper.IsCompatible(source.DataType, target.DataType))
                            {
                                var connection = new ConnectionViewModel(target, source);

                                if (version >= 3)
                                {
                                    connection.Order = order;
                                }
                                else
                                {
                                    // v2: 按同一输出源自动递增分配 Order
                                    if (!orderMap.TryGetValue(source, out var nextOrder))
                                        nextOrder = 1;
                                    connection.Order = nextOrder;
                                    orderMap[source] = nextOrder + 1;
                                }

                                source.IsConnected = true;
                                target.IsConnected = true;
                                connections.Add(connection);
                                Debug.WriteLine($"[ProjectSerializer] 已恢复连线: '{source.Title}' -> '{target.Title}', Order={connection.Order}");
                            }
                            else
                            {
                                Debug.WriteLine($"[ProjectSerializer] 跳过连线: 类型不兼容 {source.DataType} != {target.DataType}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[ProjectSerializer] 跳过连线: 连接器索引越界");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[ProjectSerializer] 跳过连线: 节点索引越界");
                    }
                }
            }

            Debug.WriteLine($"[ProjectSerializer] 加载完成: {nodes.Count} 节点, {connections.Count} 连线");
            return (nodes, connections);
        }

        /// <summary>
        /// 根据类型名称创建操作节点（用于项目加载时反序列化）
        /// 使用 FullName + AssemblyName 进行跨版本兼容的类型查找
        /// </summary>
        private static OperationNodeViewModel? CreateNodeFromTypeName(
            string typeFullName, string assemblyName,
            string title, double x, double y, PluginManager pluginManager)
        {
            try
            {
                Type? type = null;

                // 遍历所有已加载的程序集，按 FullName + AssemblyName 匹配
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!string.IsNullOrEmpty(assemblyName) &&
                        asm.GetName().Name != assemblyName)
                        continue;

                    type = asm.GetType(typeFullName);
                    if (type != null) break;
                }

                // 如果按程序集名没找到，忽略程序集名再试一次
                if (type == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = asm.GetType(typeFullName);
                        if (type != null) break;
                    }
                }

                if (type == null)
                {
                    pluginManager.Log($"无法找到类型: {typeFullName} (Assembly: {assemblyName})",
                        LogLevel.Warning);
                    return null;
                }

                var instance = Activator.CreateInstance(type);
                if (instance == null) return null;

                var node = new OperationNodeViewModel(instance);
                node.Title = title;
                node.Location = new System.Windows.Point(x, y);
                return node;
            }
            catch (Exception ex)
            {
                pluginManager.Log($"创建节点失败: {typeFullName} - {ex.Message}", LogLevel.Error);
                return null;
            }
        }
    }
}