using System;
using System.Diagnostics;
using System.Windows;
using LYCorePro.Core;
using LYCorePro.Views;

namespace LYCorePro
{
    public partial class MainWindow
    {
        private PluginManager _pluginManager = null!;
        private ToolboxService _toolboxService = null!;
        private EditorViewModel _viewModel = null!;

        public MainWindow()
        {
            InitializeComponent();
            // 创建 EditorViewModel 并设置为 NodifyEditorView 的 DataContext
            _viewModel = new EditorViewModel();
            Editor.DataContext = _viewModel;
            _pluginManager = new PluginManager(Editor, "Plugins");
            _pluginManager.PluginLoaded += (s, e) => UpdateToolbox();
            _pluginManager.PluginUnloaded += (s, e) => UpdateToolbox();

            _toolboxService = new ToolboxService();

            LoadPlugins();
        }

        private void LoadPlugins()
        {
            try
            {
                _pluginManager.LoadAllPlugins();
                UpdateToolbox();

                Title = $"插件系统 - 已加载 {_pluginManager.LoadedPlugins.Count} 个插件";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载插件失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateToolbox()
        {
            _toolboxService.BuildToolbox(_pluginManager.LoadedPlugins);
            ToolboxView.Initialize(_toolboxService);
        }

        /// <summary>
        /// 保存项目：将当前编辑器中所有实现了 ISerializableOperation 的节点参数序列化为二进制文件
        /// </summary>
        private void OnSaveProject(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "保存项目",
                Filter = "LYCorePro 项目文件 (*.lysp)|*.lysp|所有文件 (*.*)|*.*",
                DefaultExt = ".lysp",
                FileName = "project.lysp"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                ProjectSerializer.Save(dialog.FileName, _viewModel.Operations, _viewModel.Connections);
                Debug.WriteLine($"[MainWindow] 项目已保存: {dialog.FileName}");
                Title = $"插件系统 - 已保存 {dialog.FileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存项目失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载项目：从二进制文件反序列化所有节点参数并恢复到编辑器
        /// </summary>
        private void OnLoadProject(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "加载项目",
                Filter = "LYCorePro 项目文件 (*.lysp)|*.lysp|所有文件 (*.*)|*.*",
                DefaultExt = ".lysp"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var (nodes, connections) = ProjectSerializer.Load(dialog.FileName, _pluginManager);
                // 如果当前在子画布中，先返回根画布，避免新旧数据冲突
                _viewModel.NavigateToRoot();

                // 清空现有节点和连线
                _viewModel.SelectedOperations.Clear();
                _viewModel.Connections.Clear();
                _viewModel.Operations.Clear();

                // 添加加载的节点（注入 ExecuteCommand、Connections 和 ParentEditor，与 OnDrop/AddOperation/OnPaste 保持一致）
                foreach (var node in nodes)
                {
                    node.ExecuteCommand = _viewModel.ExecuteCommand;
                    if (node.Instance is IConnectionAware connectionAware)
                    {
                        connectionAware.Connections = _viewModel.Connections;
                    }
                    // 注入 ParentEditor（用于 FunctionBlock 等需要导航的节点）
                    var fbProp = node.Instance.GetType().GetProperty("ParentEditor");
                    if (fbProp != null && fbProp.CanWrite)
                    {
                        fbProp.SetValue(node.Instance, _viewModel);
                    }
                    _viewModel.Operations.Add(node);
                }

                // 恢复连线
                foreach (var conn in connections)
                {
                    _viewModel.Connections.Add(conn);
                }

                Debug.WriteLine($"[MainWindow] 项目已加载: {dialog.FileName}, 节点数: {nodes.Count}, 连线数: {connections.Count}");
                Title = $"插件系统 - 已加载 {dialog.FileName}";
                // 加载完成后自适应视图（延迟到 UI 布局完成后再执行）
                Dispatcher.BeginInvoke(new Action(() => Editor.CenterView()),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载项目失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 执行一次：按拓扑顺序执行编辑器中所有节点
        /// </summary>
        private void OnExecuteAll(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ExecuteAllCommand.CanExecute(null))
                _viewModel.ExecuteAllCommand.Execute(null);
        }

        protected override void OnClosed(EventArgs e)
        {
            // 关闭前断开所有插件的通讯连接
            foreach (var node in _viewModel.Operations)
            {
                try
                {
                    var disconnectMethod = node.Instance.GetType().GetMethod("Disconnect");
                    if (disconnectMethod != null)
                    {
                        disconnectMethod.Invoke(node.Instance, null);
                        Debug.WriteLine($"[MainWindow] 已断开: {node.Title}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MainWindow] 断开失败 {node.Title}: {ex.Message}");
                }
            }

            _pluginManager.UnloadAllPlugins();
            base.OnClosed(e);
        }
    }
}