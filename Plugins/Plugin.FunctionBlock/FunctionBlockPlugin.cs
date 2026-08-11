using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using PluginBase.Attributes;
using PluginBase.Base;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace FunctionBlockPlugin
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
    [Plugin("功能块", Description = "功能块助力画面简洁",
        Version = "1.0.0",
        Author = "LYCorePro",
        Icon = "M73.6 294.4L480 535.872v464.192a127.936 127.936 0 0 1-32-13.056l-315.392-182.144a128 128 0 0 1-64-110.848v-364.16c0-12.16 1.728-24.192 4.992-35.584zM950.4 294.4c3.264 11.328 4.992 23.296 4.992 35.52v364.16a128 128 0 0 1-64 110.848L576 987.072c-10.176 5.824-20.928 10.24-32 13.056V535.936l406.4-241.6zM132.48 403.712v290.304a64 64 0 0 0 25.728 51.264l6.336 4.16L416 894.72l-0.064-322.368-283.392-168.512zM891.328 403.712L607.936 572.288l0.064 322.304 251.392-145.088a64 64 0 0 0 31.552-47.872l0.448-7.552V403.84zM576 36.928l315.392 182.144c9.728 5.632 18.56 12.416 26.368 20.16L512 480.512L106.24 239.232c7.808-7.744 16.64-14.528 26.368-20.16L448 36.928a128 128 0 0 1 128 0zM487.68 88.64l-7.68 3.712L228.544 237.504l283.392 168.512 283.456-168.512L544 92.352a64 64 0 0 0-56.32-3.712z")]
    [PluginCategory("流程控制")]
    [PluginIO(InputCount = 0, OutputCount = 0)] // OutputCount=0 表示输出数量动态可变
    [PluginTag("功能块")]
    public class FunctionBlockPlugin : PluginBase.Base.PluginBase
    {
        public static readonly IValueConverter NullToVisibilityConverter = new NullToVisibilityConverterImpl();
        public static readonly IValueConverter BoolToVisibilityConverter = new BooleanToVisibilityConverter();

        public override string Name => "功能块";
        public override Version Version => new(1, 0, 0);
        public override string Description => "功能块（子图容器），支持动态输入输出、数据类型自定义、子画布嵌套。右键可展开子画布，在其中拖入其他插件进行连线操作。";
        public override string Author => "LYCorePro";

        public override Type[] OperationTypes => new[] { typeof(FunctionBlockOperationViewModel) };

        public override void Initialize(IPluginHost host)
        {
            base.Initialize(host);

            var nodeTemplate = CreateNodeContentTemplate();
            if (!System.Windows.Application.Current.Resources.Contains(typeof(FunctionBlockOperationViewModel)))
            {
                System.Windows.Application.Current.Resources.Add(typeof(FunctionBlockOperationViewModel), nodeTemplate);
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
            if (operationType == typeof(FunctionBlockOperationViewModel))
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
              xmlns:local=""clr-namespace:FunctionBlockPlugin;assembly=Plugin.FunctionBlock""
              DataType=""{x:Type local:FunctionBlockOperationViewModel}"">
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
              xmlns:local=""clr-namespace:FunctionBlockPlugin;assembly=Plugin.FunctionBlock""
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
              xmlns:local=""clr-namespace:FunctionBlockPlugin;assembly=Plugin.FunctionBlock""
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
              xmlns:local=""clr-namespace:FunctionBlockPlugin;assembly=Plugin.FunctionBlock""
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
              xmlns:local=""clr-namespace:FunctionBlockPlugin;assembly=Plugin.FunctionBlock""
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
              xmlns:local=""clr-namespace:FunctionBlockPlugin;assembly=Plugin.FunctionBlock""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:FunctionBlockOperationViewModel}"">
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
                    <TextBlock Text=""{Binding Inputs.Count, StringFormat='当前输入数: {0}'}""
                               FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,6""/>

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
                                  ItemsSource=""{x:Static local:FunctionBlockOperationViewModel.AvailableDataTypes}""/>
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
                                                FontSize=""10""  Background=""Transparent"" Foreground=""#1E1E2E""
                                                ToolTip=""删除此输出""
                                                
                                                BorderThickness=""0"" Cursor=""Hand""
                                                Command=""{Binding DataContext.DeleteOutputCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}""
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
                                  ItemsSource=""{x:Static local:FunctionBlockOperationViewModel.AvailableDataTypes}""/>
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