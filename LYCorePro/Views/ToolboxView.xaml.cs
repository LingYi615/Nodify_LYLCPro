using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LYCorePro.Core;

namespace LYCorePro.Views
{
    public partial class ToolboxView : UserControl, INotifyPropertyChanged
    {
        private ToolboxService? _toolboxService;
        private string _searchText = string.Empty;
        private ObservableCollection<ToolboxNode> _filteredCategories = new();
        private ToolboxOperation? _draggingOperation;
        private Point _dragStartPoint;
        private bool _isDragging;

        public event EventHandler<ToolboxOperation>? OperationDoubleClicked;
        public event EventHandler<ToolboxDragEventArgs>? DragStarted;
        public event EventHandler<ToolboxDragEventArgs>? DragCompleted;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    FilterCategories();
                }
            }
        }

        public ObservableCollection<ToolboxNode> FilteredCategories
        {
            get => _filteredCategories;
            set
            {
                _filteredCategories = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasItems));
            }
        }

        public bool HasItems => FilteredCategories?.Count > 0;

        public ICommand ClearSearchCommand { get; }

        public ToolboxView()
        {
            InitializeComponent();
            DataContext = this;
            ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);

            // 订阅TreeView的鼠标事件用于拖拽
            ToolboxTree.PreviewMouseLeftButtonDown += OnTreeViewPreviewMouseLeftButtonDown;
            ToolboxTree.PreviewMouseMove += OnTreeViewPreviewMouseMove;
            ToolboxTree.PreviewMouseLeftButtonUp += OnTreeViewPreviewMouseLeftButtonUp;

            // 允许拖放
            AllowDrop = true;
        }

        public void Initialize(ToolboxService toolboxService)
        {
            _toolboxService = toolboxService;
            FilteredCategories = new ObservableCollection<ToolboxNode>(_toolboxService.RootCategories);
        }

        private void FilterCategories()
        {
            if (_toolboxService == null) return;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredCategories = new ObservableCollection<ToolboxNode>(_toolboxService.RootCategories);
                return;
            }

            var matches = _toolboxService.SearchOperations(SearchText);
            var matchNames = new HashSet<string>(matches.Select(m => m.Name));

            var filtered = new ObservableCollection<ToolboxNode>();
            foreach (var node in _toolboxService.RootCategories)
            {
                if (node is ToolboxCategory category)
                {
                    var filteredCategory = FilterCategory(category, matchNames);
                    if (filteredCategory != null)
                    {
                        filtered.Add(filteredCategory);
                    }
                }
                else if (node is ToolboxOperation operation && matchNames.Contains(operation.Name))
                {
                    filtered.Add(operation);
                }
            }

            FilteredCategories = filtered;
        }

        private ToolboxCategory? FilterCategory(ToolboxCategory category, HashSet<string> matchNames)
        {
            var matchingOps = category.Children.OfType<ToolboxOperation>()
                .Where(op => matchNames.Contains(op.Name)).ToList();

            var filteredChildren = new ObservableCollection<ToolboxNode>();
            foreach (var child in category.Children.OfType<ToolboxCategory>())
            {
                var filteredChild = FilterCategory(child, matchNames);
                if (filteredChild != null)
                {
                    filteredChildren.Add(filteredChild);
                }
            }

            if (matchingOps.Any() || filteredChildren.Any())
            {
                var newCategory = new ToolboxCategory(category.Name, category.Icon);
                newCategory.IsExpanded = true;

                foreach (var op in matchingOps.OrderBy(o => o.SortOrder))
                {
                    newCategory.Children.Add(op);
                }

                foreach (var child in filteredChildren)
                {
                    newCategory.Children.Add(child);
                }

                return newCategory;
            }

            return null;
        }

        #region 拖拽逻辑

        private void OnTreeViewPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = FindParent<TreeViewItem>(e.OriginalSource as DependencyObject);
            if (treeViewItem?.Header is ToolboxOperation operation)
            {
                _dragStartPoint = e.GetPosition(null);
                _draggingOperation = operation;
                _isDragging = false;

                if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 500)
                {
                    OnOperationDoubleClicked(operation);
                    _draggingOperation = null;
                }
                _lastClickTime = DateTime.Now;
            }
        }

        private void OnTreeViewPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingOperation == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            var currentPoint = e.GetPosition(null);
            var dragDistance = (currentPoint - _dragStartPoint).Length;

            if (!_isDragging && dragDistance > SystemParameters.MinimumHorizontalDragDistance)
            {
                _isDragging = true;

                OnDragStarted(new ToolboxDragEventArgs(_draggingOperation, DragDropEffects.None));

                // 创建拖拽数据 - 确保数据格式正确
                var dataObject = new DataObject();
                dataObject.SetData("ToolboxOperation", _draggingOperation);
                dataObject.SetData("OperationType", _draggingOperation.OperationType);
                dataObject.SetData(typeof(ToolboxOperation).FullName ?? "ToolboxOperation", _draggingOperation);

                // 执行拖拽
                var result = DragDrop.DoDragDrop(this, dataObject, DragDropEffects.Copy);

                OnDragCompleted(new ToolboxDragEventArgs(_draggingOperation, result));

                _isDragging = false;
                _draggingOperation = null;
            }
        }

        private void OnTreeViewPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _draggingOperation = null;
        }

        private DateTime _lastClickTime = DateTime.Now;

        #endregion

        #region 事件触发

        protected virtual void OnDragStarted(ToolboxDragEventArgs e)
        {
            DragStarted?.Invoke(this, e);
        }

        protected virtual void OnDragCompleted(ToolboxDragEventArgs e)
        {
            DragCompleted?.Invoke(this, e);
        }

        protected virtual void OnOperationDoubleClicked(ToolboxOperation operation)
        {
            OperationDoubleClicked?.Invoke(this, operation);
        }

        #endregion

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ToolboxDragEventArgs : EventArgs
    {
        public ToolboxOperation Operation { get; }
        public DragDropEffects DragEffect { get; }

        public ToolboxDragEventArgs(ToolboxOperation operation, DragDropEffects dragEffect)
        {
            Operation = operation;
            DragEffect = dragEffect;
        }
    }
}