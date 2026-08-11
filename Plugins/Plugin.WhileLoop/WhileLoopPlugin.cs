using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using PluginBase.Attributes;
using PluginBase.Base;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace WhileLoopPlugin
{
    /// <summary>
    /// 功能块插件
    /// - 拖入编辑器时不带任何输入输出
    /// - 输入输出可新增/删除，数据类型可自定义
    /// - 右键 → 展开 → 打开子画布窗口
    /// - 子画布中可拖入其他插件、连线、删除等
    /// - 子画布右键 → 返回上一级
    /// - 不包含任何逻辑运算
    /// </summary>
    [Plugin("WhileLoop", Description = "While循环",
        Version = "1.0.0",
        Author = "LYCorePro",
        Icon = "M489.448792 1024c-66.011546 0-130.052598-12.917682-190.371606-38.424631-58.239042-24.631174-110.566603-59.771648-155.450075-104.655121l49.481292-49.590763c79.148172 79.0387 184.460124 122.60851 296.340389 122.608509 58.020098 0 113.193928-10.947188 163.988882-32.513149 39.628822-16.85867 76.192431-39.957238 108.924524-68.857815 13.683985-12.151379 34.483643-11.60402 47.620269 1.094719 14.669232 14.231345 14.012401 37.877272-1.313662 51.451785-38.315159 33.826812-81.228138 60.975839-127.753688 80.79025-59.552705 25.288005-123.922172 38.096216-191.466325 38.096216zM973.205046 609.758392l-69.295702-10.728244c3.284157-21.237545 4.926235-42.912978 4.926234-64.47894 0-91.299551-28.791105-178.001283-83.308103-250.909557-26.273252-35.031003-57.582211-65.792602-93.379517-91.080608-27.258499-19.376523-56.487492-35.359418-87.358563-47.620269-17.077614-6.787257-26.054308-25.506949-20.690186-43.02245 6.020954-19.376523 27.367971-29.776352 46.306607-22.222793 36.125722 14.340817 70.499893 33.060509 102.356211 55.721189 41.599316 29.557409 78.272397 65.354715 108.815052 106.297199 30.980543 41.380372 55.064357 87.139619 71.48514 135.854608 17.077614 50.466538 25.725893 103.231986 25.725893 156.982681 0.109472 25.178533-1.861022 50.57601-5.583066 75.207184zM41.706788 603.737439c-20.252298 3.065213-38.753047-11.713492-40.285654-32.184734C0.547359 559.291854 0 546.921531 0 534.551208c0-104.983536 32.732093-205.15031 94.802651-289.553132 29.885824-40.614069 65.573658-76.192431 106.297199-105.968784 41.161428-30.104768 86.59226-53.422279 134.869361-69.295702l22.003849 66.558905c-41.2709 13.683985-80.133419 33.607868-115.492838 59.443233-34.921531 25.506949-65.573658 56.049604-91.190079 90.861663C98.19628 358.848835 70.062006 444.674792 70.062006 534.66068c0 10.728245 0.437888 21.456489 1.20419 32.075262 1.423134 18.062861-11.60402 34.2647-29.557408 37.001497zM546.483643 67.43468L358.082532 206.026085 319.329485 0l227.154158 67.43468zM852.457558 785.460765l-0.109472-233.941415 188.948472 90.752192-188.839 143.189223zM52.984392 687.811845L258.353645 799.69211 88.124866 922.081676l-35.140474-234.269831zM705.874706 326.883045h65.135771l-165.302545 414.351079h-88.124866l-18.062861-308.163352-136.839854 308.163352h-88.124867l-4.926234-414.351079h73.346162l-1.094719 324.584135 142.313449-324.584135h79.914475l16.420782 324.584135z")]
    [PluginCategory("流程控制")]
    [PluginIO(InputCount = 1, OutputCount = 0)] // OutputCount=0 表示输出数量动态可变
    [PluginTag("WhileLoop")]
    public class WhileLoopPlugin : PluginBase.Base.PluginBase
    {
        public static readonly IValueConverter NullToVisibilityConverter = new NullToVisibilityConverterImpl();
        public static readonly IValueConverter BoolToVisibilityConverter = new BooleanToVisibilityConverter();

        public override string Name => "WhileLoop";
        public override Version Version => new(1, 0, 0);
        public override string Description => "WhileLoop（子图容器），支持动态输入输出、数据类型自定义、子画布嵌套。右键可展开子画布，在其中拖入其他插件进行连线操作。";
        public override string Author => "LYCorePro";

        public override Type[] OperationTypes => new[] { typeof(WhileLoopOperationViewModel) };

        public override void Initialize(IPluginHost host)
        {
            base.Initialize(host);

            var nodeTemplate = CreateNodeContentTemplate();
            if (!System.Windows.Application.Current.Resources.Contains(typeof(WhileLoopOperationViewModel)))
            {
                System.Windows.Application.Current.Resources.Add(typeof(WhileLoopOperationViewModel), nodeTemplate);
            }

            // 注册子画布入口/出口节点的显示模板
            var entryTemplate = CreateEntryNodeTemplate();
            if (!System.Windows.Application.Current.Resources.Contains(typeof(SubEditorEntryNode)))
            {
                System.Windows.Application.Current.Resources.Add(typeof(SubEditorEntryNode), entryTemplate);
            }

            var exitTemplate = CreateExitNodeTemplate();
            if (!System.Windows.Application.Current.Resources.Contains(typeof(SubEditorExitNode)))
            {
                System.Windows.Application.Current.Resources.Add(typeof(SubEditorExitNode), exitTemplate);
            }

            // 注册子画布入口/出口节点的参数面板模板（右侧设置面板）
            var entrySettingsKey = typeof(SubEditorEntryNode).FullName + ".Settings";
            var entrySettingsTemplate = CreateEntrySettingsTemplate();
            System.Windows.Application.Current.Resources[entrySettingsKey] = entrySettingsTemplate;

            var exitSettingsKey = typeof(SubEditorExitNode).FullName + ".Settings";
            var exitSettingsTemplate = CreateExitSettingsTemplate();
            System.Windows.Application.Current.Resources[exitSettingsKey] = exitSettingsTemplate;
        }

        public override DataTemplate? GetTemplate(Type operationType)
        {
            if (operationType == typeof(WhileLoopOperationViewModel))
            {
                return CreateSettingsPanelTemplate();
            }
            return base.GetTemplate(operationType);
        }

        /// <summary>
        /// 节点内容模板（显示在 Nodify 节点内部）
        /// 右键 ContextMenu 提供"展开子画布"入口
        /// 输入输出管理（新增/删除/选择类型）在右侧参数面板中操作
        /// </summary>
        private DataTemplate CreateNodeContentTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:WhileLoopPlugin;assembly=Plugin.WhileLoop""
              DataType=""{x:Type local:WhileLoopOperationViewModel}"">
    <Border Background=""#1E1E2E"" CornerRadius=""4"" Padding=""8,6"" Margin=""4,2""
            BorderBrush=""#45475A"" BorderThickness=""1"">
        <TextBlock Text=""{Binding InstanceInfo}""
                   FontSize=""11"" Foreground=""#A6ADC8""
                   TextWrapping=""Wrap""
                   TextTrimming=""CharacterEllipsis""
                   MaxWidth=""180""
                   FontFamily=""Consolas""/>
    </Border>
</DataTemplate>";

            using var stringReader = new System.IO.StringReader(xaml);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (DataTemplate)System.Windows.Markup.XamlReader.Load(xmlReader);
        }

        /// <summary>
        /// 子画布入口节点模板（显示在 Nodify 节点内部）
        /// </summary>
        private DataTemplate CreateEntryNodeTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:WhileLoopPlugin;assembly=Plugin.WhileLoop""
              DataType=""{x:Type local:SubEditorEntryNode}"">
    <Border Background=""#1E3A2E"" CornerRadius=""4"" Padding=""8,6"" Margin=""4,2""
            BorderBrush=""#3A6B4A"" BorderThickness=""1"">
        <TextBlock Text=""{Binding DisplayInfo}""
                   FontSize=""11"" Foreground=""#A6E3A1""
                   TextWrapping=""Wrap""
                   TextTrimming=""CharacterEllipsis""
                   MaxWidth=""180""
                   FontFamily=""Consolas""/>
    </Border>
</DataTemplate>";

            using var stringReader = new System.IO.StringReader(xaml);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (DataTemplate)System.Windows.Markup.XamlReader.Load(xmlReader);
        }

        /// <summary>
        /// 子画布出口节点模板（显示在 Nodify 节点内部）
        /// </summary>
        private DataTemplate CreateExitNodeTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:WhileLoopPlugin;assembly=Plugin.WhileLoop""
              DataType=""{x:Type local:SubEditorExitNode}"">
    <Border Background=""#2E1E3A"" CornerRadius=""4"" Padding=""8,6"" Margin=""4,2""
            BorderBrush=""#6B3A8A"" BorderThickness=""1"">
        <TextBlock Text=""{Binding DisplayInfo}""
                   FontSize=""11"" Foreground=""#CBA6F7""
                   TextWrapping=""Wrap""
                   TextTrimming=""CharacterEllipsis""
                   MaxWidth=""180""
                   FontFamily=""Consolas""/>
    </Border>
</DataTemplate>";

            using var stringReader = new System.IO.StringReader(xaml);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (DataTemplate)System.Windows.Markup.XamlReader.Load(xmlReader);
        }

        /// <summary>
        /// 子画布入口节点参数面板模板（右侧设置面板，显示当前数据）
        /// </summary>
        private DataTemplate CreateEntrySettingsTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:WhileLoopPlugin;assembly=Plugin.WhileLoop""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:SubEditorEntryNode}"">
    <ScrollViewer VerticalScrollBarVisibility=""Auto"">
        <StackPanel Margin=""0,8,0,0"">
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""入口节点"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#A6E3A1"" Margin=""0,0,0,8""/>
                    <TextBlock FontSize=""11"" Foreground=""#CDD6F4""
                               Margin=""0,0,0,4"">
                        <TextBlock.Text>
                            <Binding Path=""NodeName"" StringFormat=""名称: {0}""/>
                        </TextBlock.Text>
                    </TextBlock>
                    <TextBlock FontSize=""11"" Foreground=""#6C7086""
                               Margin=""0,0,0,8"">
                        <TextBlock.Text>
                            <Binding Path=""DataType"" StringFormat=""数据类型: {0}""/>
                        </TextBlock.Text>
                    </TextBlock>
                    <Separator Background=""#313244"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""当前数据"" FontSize=""11"" FontWeight=""Bold""
                               Foreground=""#F9E2AF"" Margin=""0,0,0,6""/>
                    <Border Background=""#1E2A3A"" CornerRadius=""3""
                            Padding=""8,6"">
                        <TextBlock FontSize=""12"" Foreground=""#A6E3A1""
                                   FontFamily=""Consolas""
                                   TextWrapping=""Wrap"">
                            <TextBlock.Text>
                                <Binding Path=""Value"" TargetNullValue=""(无数据)""/>
                            </TextBlock.Text>
                        </TextBlock>
                    </Border>
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</DataTemplate>";

            using var stringReader = new System.IO.StringReader(xaml);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (DataTemplate)System.Windows.Markup.XamlReader.Load(xmlReader);
        }

        /// <summary>
        /// 子画布出口节点参数面板模板（右侧设置面板，显示当前数据）
        /// </summary>
        private DataTemplate CreateExitSettingsTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:WhileLoopPlugin;assembly=Plugin.WhileLoop""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:SubEditorExitNode}"">
    <ScrollViewer VerticalScrollBarVisibility=""Auto"">
        <StackPanel Margin=""0,8,0,0"">
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""出口节点"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#CBA6F7"" Margin=""0,0,0,8""/>
                    <TextBlock FontSize=""11"" Foreground=""#CDD6F4""
                               Margin=""0,0,0,4"">
                        <TextBlock.Text>
                            <Binding Path=""NodeName"" StringFormat=""名称: {0}""/>
                        </TextBlock.Text>
                    </TextBlock>
                    <TextBlock FontSize=""11"" Foreground=""#6C7086""
                               Margin=""0,0,0,8"">
                        <TextBlock.Text>
                            <Binding Path=""DataType"" StringFormat=""数据类型: {0}""/>
                        </TextBlock.Text>
                    </TextBlock>
                    <Separator Background=""#313244"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""当前数据"" FontSize=""11"" FontWeight=""Bold""
                               Foreground=""#F9E2AF"" Margin=""0,0,0,6""/>
                    <Border Background=""#2A1E3A"" CornerRadius=""3""
                            Padding=""8,6"">
                        <TextBlock FontSize=""12"" Foreground=""#CBA6F7""
                                   FontFamily=""Consolas""
                                   TextWrapping=""Wrap"">
                            <TextBlock.Text>
                                <Binding Path=""Value"" TargetNullValue=""(无数据)""/>
                            </TextBlock.Text>
                        </TextBlock>
                    </Border>
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</DataTemplate>";

            using var stringReader = new System.IO.StringReader(xaml);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (DataTemplate)System.Windows.Markup.XamlReader.Load(xmlReader);
        }

        /// <summary>
        /// 参数面板模板（右侧设置面板）
        /// </summary>
        private DataTemplate CreateSettingsPanelTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:WhileLoopPlugin;assembly=Plugin.WhileLoop""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:WhileLoopOperationViewModel}"">
 <TabControl Background=""#1E1E2E"" BorderThickness=""0"">
        <TabControl.Resources>
            <Style TargetType=""TabItem"">
                <Setter Property=""Background"" Value=""#181825""/>
                <Setter Property=""Foreground"" Value=""#A6ADC8""/>
                <Setter Property=""FontSize"" Value=""12""/>
                <Setter Property=""Padding"" Value=""12,6""/>
                <Setter Property=""Template"">
                    <Setter.Value>
                        <ControlTemplate TargetType=""TabItem"">
                            <Border x:Name=""Border""
                                    Background=""{TemplateBinding Background}""
                                    BorderBrush=""#313244""
                                    BorderThickness=""0,0,0,2""
                                    Padding=""{TemplateBinding Padding}""
                                    CornerRadius=""4,4,0,0"">
                                <ContentPresenter Content=""{TemplateBinding Header}""
                                                  HorizontalAlignment=""Center""
                                                  VerticalAlignment=""Center""/>
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property=""IsSelected"" Value=""True"">
                                    <Setter TargetName=""Border"" Property=""Background"" Value=""#313244""/>
                                    <Setter TargetName=""Border"" Property=""BorderBrush"" Value=""#89B4FA""/>
                                    <Setter Property=""Foreground"" Value=""#CDD6F4""/>
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>
        </TabControl.Resources>

        <!-- ==================== Tab 1: 参数设定 ==================== -->
        <TabItem Header=""参数设定"">   
    <ScrollViewer VerticalScrollBarVisibility=""Auto"">
        <StackPanel Margin=""0,8,0,0"">
     <!--    子画布操作                                                                                           -->
     <!--   <Border Background=""#181825"" BorderBrush=""#313244""                                                         -->
     <!--           BorderThickness=""1"" CornerRadius=""4""                                                               -->
     <!--           Padding=""10"" Margin=""0,0,0,12"">                                                                    -->
     <!--       <StackPanel>                                                                                               -->
     <!--           <TextBlock Text=""子画布"" FontSize=""12"" FontWeight=""Bold""                                         -->
     <!--                      Foreground=""#89B4FA"" Margin=""0,0,0,8""/>                                                 -->
     <!--           <Button Content=""展开子画布"" Height=""30""                                                           -->
     <!--                   Command=""{Binding OpenSubEditorCommand}""                                                     -->
     <!--                   Background=""#45475A"" Foreground=""#CDD6F4""                                                  -->
     <!--                   BorderThickness=""0"" Cursor=""Hand""                                                          -->
     <!--                   FontSize=""12"" FontWeight=""Bold""                                                            -->
     <!--                   ToolTip=""打开子画布窗口，在其中拖入插件进行连线操作""/>                                       -->
     <!--           <TextBlock FontSize=""10"" Foreground=""#585B70""                                                      -->
     <!--                      TextWrapping=""Wrap"" Margin=""0,6,0,0""                                                    -->
     <!--                      Text=""在子画布中可拖入任意插件，进行连线、删除等操作。右键菜单可返回上一级。""/>           -->
     <!--       </StackPanel>                                                                                              -->
     <!--   </Border> -->

            <!-- 输入管理 -->
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""输入管理"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                    <TextBlock FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,6"">
                        <Run Text=""当前输入数: ""/>
                        <Run Text=""{Binding Inputs.Count, Mode=OneWay}"" />
                        <!-- 后半句高亮橙色加粗 -->
                        <Run Text=""        (新增输入仅为通讯协议实例)"" Foreground=""#FF9500"" FontWeight=""Bold""/>
                    </TextBlock>
                    

                    <!-- 输入列表 -->
                    <ItemsControl ItemsSource=""{Binding Inputs}"">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background=""#313244"" CornerRadius=""3""
                                        Padding=""6,4"" Margin=""0,0,0,4"">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width=""*""/>
                                            <ColumnDefinition Width=""Auto""/>
                                            <ColumnDefinition Width=""Auto""/>
                                        </Grid.ColumnDefinitions>
                                        <StackPanel Grid.Column=""0"" Orientation=""Horizontal"">
                                            <TextBox Text=""{Binding Title, UpdateSourceTrigger=PropertyChanged}""
                                                     IsReadOnly=""{Binding IsProtect}"" 
                                                     FontSize=""11"" Foreground=""#CDD6F4""
                                                     Background=""Transparent""
                                                     BorderThickness=""0""
                                                     VerticalAlignment=""Center""
                                                     MinWidth=""40""
                                                     Margin=""0,0,8,0""
                                                     CaretBrush=""#CDD6F4""/>
                                            <TextBlock FontSize=""10"" Foreground=""#585B70""
                                                       VerticalAlignment=""Center"">
                                                <TextBlock.Text>
                                                    <Binding Path=""DataType"" StringFormat=""({0})""/>
                                                </TextBlock.Text>
                                            </TextBlock>
                                        </StackPanel>
                                        <Button Grid.Column=""2"" Width=""20"" Height=""20""
                                                FontSize=""10"" Background=""Transparent"" Foreground=""#1E1E2E""
                                                ToolTip=""删除此输入""
                                                BorderThickness=""0"" Cursor=""Hand""
                                                Command=""{Binding DataContext.DeleteInputCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}""
                                                Visibility=""{Binding IsProtect, Converter={x:Static local:WhileLoopOperationViewModel.BoolToVisibilityInverseConverter}}"" 
                                                CommandParameter=""{Binding}"">
                                                <Button.Content>
                                                    <Path 
                                                        Data=""M559.786667 505.173333 L754.346667 310.613333 C767.999999 296.960000 767.999999 276.480000 754.346667 262.826667 C740.693334 249.173334 720.213334 249.173334 706.560000 262.826667 L512 457.386667 L317.44 262.826667 C303.786667 249.173334 283.306667 249.173334 269.653333 262.826667 C256.000000 276.480000 256.000000 296.960000 269.653333 310.613333 L464.213333 505.173333 L269.653333 699.733333 C256.000000 713.386667 256.000000 733.866667 269.653333 747.520000 C283.306667 761.173333 303.786667 761.173333 317.44 747.520000 L512 552.960000 L706.56 747.520000 C720.213334 761.173333 740.693334 761.173333 754.346667 747.520000 C767.999999 733.866667 767.999999 713.386667 754.346667 699.733333 L559.786667 505.173333 Z""
                                                        Fill=""#DC3535""
                                                        Stretch=""Uniform""/>
                                                </Button.Content>
                                        </Button>
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>

                    <!-- 新增输入 -->
                    <Grid Margin=""0,8,0,0"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""*""/>
                            <ColumnDefinition Width=""6""/>
                            <ColumnDefinition Width=""50""/>
                        </Grid.ColumnDefinitions>
                        <ComboBox Grid.Column=""0"" x:Name=""InputTypeCombo"" Height=""26""
                                  FontSize=""11"" Background=""#45475A""
                                  Foreground=""#CDD6F4"" BorderThickness=""0""
                                  ItemsSource=""{x:Static local:WhileLoopOperationViewModel.InDataTypes}""/>
                        <Button Grid.Column=""2"" Content=""新增"" Height=""26""
                                FontSize=""11"" Background=""#45475A""
                                Foreground=""#A6E3A1"" BorderThickness=""0""
                                Cursor=""Hand""
                                Command=""{Binding AddInputCommand}""
                                CommandParameter=""{Binding SelectedItem, ElementName=InputTypeCombo}""/>
                    </Grid>
                </StackPanel>
            </Border>

            <!-- 输出管理 -->
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""输出管理"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""{Binding Outputs.Count, StringFormat='当前输出数: {0}'}""
                               FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,6""/>

                    <!-- 输出列表 -->
                    <ItemsControl ItemsSource=""{Binding Outputs}"">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background=""#313244"" CornerRadius=""3""
                                        Padding=""6,4"" Margin=""0,0,0,4"">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width=""*""/>
                                            <ColumnDefinition Width=""Auto""/>
                                        </Grid.ColumnDefinitions>
                                        <StackPanel Grid.Column=""0"" Orientation=""Horizontal"">
                                            <TextBox Text=""{Binding Title, UpdateSourceTrigger=PropertyChanged}""
                                                     IsReadOnly=""{Binding IsProtect}"" 
                                                     FontSize=""11"" Foreground=""#CDD6F4""
                                                     Background=""Transparent""
                                                     BorderThickness=""0""
                                                     VerticalAlignment=""Center""
                                                     MinWidth=""40""
                                                     Margin=""0,0,8,0""
                                                     CaretBrush=""#CDD6F4""/>
                                            <TextBlock FontSize=""10"" Foreground=""#585B70""
                                                       VerticalAlignment=""Center"">
                                                <TextBlock.Text>
                                                    <Binding Path=""DataType"" StringFormat=""({0})""/>
                                                </TextBlock.Text>
                                            </TextBlock>
                                        </StackPanel>
                                        <Button Grid.Column=""1"" Width=""20"" Height=""20""
                                                FontSize=""10"" Background=""Transparent"" Foreground=""#1E1E2E""
                                                ToolTip=""删除此输出""  
                                                BorderThickness=""0"" Cursor=""Hand""
                                                Command=""{Binding DataContext.DeleteOutputCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}""
                                                Visibility=""{Binding IsProtect, Converter={x:Static local:WhileLoopOperationViewModel.BoolToVisibilityInverseConverter}}"" 
                                                CommandParameter=""{Binding}"">
                                                <Button.Content>
                                                    <Path 
                                                        Data=""M559.786667 505.173333 L754.346667 310.613333 C767.999999 296.960000 767.999999 276.480000 754.346667 262.826667 C740.693334 249.173334 720.213334 249.173334 706.560000 262.826667 L512 457.386667 L317.44 262.826667 C303.786667 249.173334 283.306667 249.173334 269.653333 262.826667 C256.000000 276.480000 256.000000 296.960000 269.653333 310.613333 L464.213333 505.173333 L269.653333 699.733333 C256.000000 713.386667 256.000000 733.866667 269.653333 747.520000 C283.306667 761.173333 303.786667 761.173333 317.44 747.520000 L512 552.960000 L706.56 747.520000 C720.213334 761.173333 740.693334 761.173333 754.346667 747.520000 C767.999999 733.866667 767.999999 713.386667 754.346667 699.733333 L559.786667 505.173333 Z""
                                                        Fill=""#DC3535""
                                                        Stretch=""Uniform""/>
                                                </Button.Content>
                                        </Button>
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>

                    <!-- 新增输出 -->
                    <Grid Margin=""0,8,0,0"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""*""/>
                            <ColumnDefinition Width=""6""/>
                            <ColumnDefinition Width=""50""/>
                        </Grid.ColumnDefinitions>
                        <ComboBox Grid.Column=""0"" x:Name=""OutputTypeCombo"" Height=""26""
                                  FontSize=""11"" Background=""#45475A""
                                  Foreground=""#CDD6F4"" BorderThickness=""0""
                                  ItemsSource=""{x:Static local:WhileLoopOperationViewModel.AvailableDataTypes}""/>
                        <Button Grid.Column=""2"" Content=""新增"" Height=""26""
                                FontSize=""11"" Background=""#45475A""
                                Foreground=""#A6E3A1"" BorderThickness=""0""
                                Cursor=""Hand""
                                Command=""{Binding AddOutputCommand}""
                                CommandParameter=""{Binding SelectedItem, ElementName=OutputTypeCombo}""/>
                    </Grid>
                </StackPanel>
            </Border>
        </StackPanel>
            </ScrollViewer>
</TabItem>

        <!-- ==================== Tab 2: 当前数据 ==================== -->
        <TabItem Header=""当前数据"">
 <ScrollViewer VerticalScrollBarVisibility=""Auto"">
                <StackPanel Margin=""0,8,0,0"">

            <!-- 当前数据 -->
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""当前数据"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#F9E2AF"" Margin=""0,0,0,8""/>

                    <!-- 输入数据 -->
                    <TextBlock Text=""输入数据"" FontSize=""11"" Foreground=""#89B4FA""
                               Margin=""0,0,0,4""/>
                    <ItemsControl ItemsSource=""{Binding Inputs}"">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background=""#1E2A3A"" CornerRadius=""3""
                                        Padding=""6,4"" Margin=""0,0,0,3"">
                                    <StackPanel Orientation=""Horizontal"">
                                        <TextBlock FontSize=""11"" Foreground=""#CDD6F4"">
                                            <TextBlock.Text>
                                                <MultiBinding StringFormat=""{}{0}: {1}"">
                                                    <Binding Path=""Title""/>
                                                    <Binding Path=""Value"" TargetNullValue=""(无数据)""/>
                                                </MultiBinding>
                                            </TextBlock.Text>
                                        </TextBlock>
                                    </StackPanel>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>

                    <!-- 输出数据 -->
                    <TextBlock Text=""输出数据"" FontSize=""11"" Foreground=""#A6E3A1""
                               Margin=""0,8,0,4""/>
                    <ItemsControl ItemsSource=""{Binding Outputs}"">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background=""#1E2A2E"" CornerRadius=""3""
                                        Padding=""6,4"" Margin=""0,0,0,3"">
                                    <StackPanel Orientation=""Horizontal"">
                                        <TextBlock FontSize=""11"" Foreground=""#CDD6F4"">
                                            <TextBlock.Text>
                                                <MultiBinding StringFormat=""{}{0}: {1}"">
                                                    <Binding Path=""Title""/>
                                                    <Binding Path=""Value"" TargetNullValue=""(无数据)""/>
                                                </MultiBinding>
                                            </TextBlock.Text>
                                        </TextBlock>
                                    </StackPanel>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </Border>

            <TextBlock FontSize=""11"" Foreground=""#585B70"" TextWrapping=""Wrap"">
                <Run Text=""提示:(操作步骤)&#10;""/>
                <Run Text=""1.右键点击节点 → 展开，可在子画布中拖入其他插件进行连线。&#10;"" />
                <Run Text=""2.子画布中右键菜单可选择返回上一级。""/>
            </TextBlock>
        </StackPanel>
        </ScrollViewer>
 </TabItem>
    </TabControl>
</DataTemplate>";

            using var stringReader = new System.IO.StringReader(xaml);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (DataTemplate)System.Windows.Markup.XamlReader.Load(xmlReader);
        }
    }

    internal class NullToVisibilityConverterImpl : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    
}