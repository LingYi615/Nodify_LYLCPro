using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using PluginBase.Attributes;
using PluginBase.Base;
using PluginBase.Interfaces;
using PluginBase.Models;
using static System.Net.Mime.MediaTypeNames;

namespace IfPlugin
{
    /// <summary>
    /// IF 条件插件
    /// - 所有分支共用一个子画布
    /// - 每个输入代表一个分支条件（bool，可设置取反）
    /// - 第一个输入为 IF，后续为 ELSE IF
    /// - 无 ELSE 分支
    /// - 默认输出 "Flow"（bool），执行前 false，执行后 true
    /// - 可新增其他输出传递数据
    /// </summary>
    [Plugin("IF条件", Description = "IF条件判断",
        Version = "2.0.0",
        Author = "LYCorePro",
        Icon = "M714.176 108.928c-3.008 29.568-46.272 26.496-57.728 26.496-69.824 0-145.664-36.224-186.368 112.96l-19.008 93.568H547.2c17.6 0 32 14.336 32 32s-14.4 32-32 32H437.44l-86.4 424.384s-8.32 68.224-50.24 65.6c-48-3.008-25.984-71.296-25.984-71.296l85.632-418.688H294.4c-16.704 0-30.016-12.864-31.424-29.248 1.408-29.952 14.72-34.752 31.424-34.752h79.808l20.032-95.488c14.208-67.648 43.52-124.48 94.592-157.504 51.008-33.024 134.976-27.008 175.104-22.464 16.896 1.92 54.016 5.376 50.24 42.432zM198.272 376.576c3.84-17.344-7.04-34.304-24.384-38.144-17.28-3.84-34.304 7.104-38.144 24.384L26.816 852.672c-3.648 16.32-1.664 30.4 27.2 38.208 16.32 2.112 31.808-8.128 35.328-24.384l108.928-489.92zm64.128 4.736c0-1.92 0.448-2.88 0.576-4.608-0.192-0.96-0.576-1.792-0.576-2.752v7.36zm-61.632-209.664c-26.496 0-48 21.504-48 48s21.504 48 48 48c26.624 0 48-21.504 48-48s-21.376-48-48-48zm479.936 712.448c16.384-13.696 20.736-40.576 9.856-60.032-0.384-0.832-43.648-79.232-36.736-191.616 6.848-109.696 71.36-184.64 72.896-186.368 14.528-16.32 15.616-43.52 2.368-60.736-13.376-17.152-36.096-17.984-50.752-1.6-3.52 4.032-86.976 98.752-95.808 244.288-8.896 143.296 46.336 241.6 48.576 245.824 6.4 11.008 16.384 17.344 27.008 17.984 7.552 0.512 15.68-1.984 22.592-7.744zm188.864 4.544c10.496 1.664 21.504-2.048 29.888-11.52 3.136-3.584 76.096-87.168 95.36-229.312 19.52-144.256-43.776-256-46.4-260.48-11.136-19.392-33.472-23.808-49.856-10.048-16.384 13.824-20.48 40.768-9.344 60.032 1.024 2.048 49.6 89.92 34.88 198.72-14.976 111.36-72.512 178.368-73.152 179.008-14.4 16.512-15.36 43.776-1.984 60.864 5.632 7.104 12.992 11.392 20.608 12.736z")]
    [PluginCategory("流程控制")]
    [PluginIO(InputCount = 1, OutputCount = 1, AllowMultipleInputs = true, AllowMultipleOutputs = true)]
    [PluginTag("IF判断")]
    public class IfPlugin : PluginBase.Base.PluginBase
    {
        public override string Name => "IF条件";
        public override Version Version => new(2, 0, 0);
        public override string Description => "IF 条件分支插件，所有分支共用一个子画布。每个输入为一个分支条件（bool，可设置取反），第一个为 IF，后续为 ELSE IF。默认 Flow 输出传递流程步。";
        public override string Author => "LYCorePro";

        public override Type[] OperationTypes => new[] { typeof(IfOperationViewModel) };

        public override void Initialize(IPluginHost host)
        {
            base.Initialize(host);

            var nodeTemplate = CreateNodeContentTemplate();
            if (!System.Windows.Application.Current.Resources.Contains(typeof(IfOperationViewModel)))
            {
                System.Windows.Application.Current.Resources.Add(typeof(IfOperationViewModel), nodeTemplate);
            }

            var entryTemplate = CreateEntryNodeTemplate();
            if (!System.Windows.Application.Current.Resources.Contains(typeof(IfEntryNode)))
            {
                System.Windows.Application.Current.Resources.Add(typeof(IfEntryNode), entryTemplate);
            }

            var exitTemplate = CreateExitNodeTemplate();
            if (!System.Windows.Application.Current.Resources.Contains(typeof(IfExitNode)))
            {
                System.Windows.Application.Current.Resources.Add(typeof(IfExitNode), exitTemplate);
            }

            // 注册子画布入口/出口节点的参数面板模板（右侧设置面板）
            var entrySettingsKey = typeof(IfEntryNode).FullName + ".Settings";
            var entrySettingsTemplate = CreateEntrySettingsTemplate();
            System.Windows.Application.Current.Resources[entrySettingsKey] = entrySettingsTemplate;

            var exitSettingsKey = typeof(IfExitNode).FullName + ".Settings";
            var exitSettingsTemplate = CreateExitSettingsTemplate();
            System.Windows.Application.Current.Resources[exitSettingsKey] = exitSettingsTemplate;

        }

        public override DataTemplate? GetTemplate(Type operationType)
        {
            if (operationType == typeof(IfOperationViewModel))
            {
                return CreateSettingsPanelTemplate();
            }
            return base.GetTemplate(operationType);
        }

        /// <summary>
        /// 节点内容模板
        /// </summary>
        private DataTemplate CreateNodeContentTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:IfPlugin;assembly=Plugin.If""
              DataType=""{x:Type local:IfOperationViewModel}"">
    <Border Background=""#1E1E2E"" CornerRadius=""4"" Padding=""8,6"" Margin=""4,2""
            BorderBrush=""#45475A"" BorderThickness=""1"">
        <TextBlock Text=""{Binding InstanceInfo}""
                   FontSize=""11"" Foreground=""#A6ADC8""
                   TextWrapping=""Wrap""
                   TextTrimming=""CharacterEllipsis""
                   MaxWidth=""200""
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
              xmlns:local=""clr-namespace:IfPlugin;assembly=Plugin.If""
              DataType=""{x:Type local:IfEntryNode}"">
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
              xmlns:local=""clr-namespace:IfPlugin;assembly=Plugin.If""
              DataType=""{x:Type local:IfExitNode}"">
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
              xmlns:local=""clr-namespace:IfPlugin;assembly=Plugin.If""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:IfEntryNode}"">
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
              xmlns:local=""clr-namespace:IfPlugin;assembly=Plugin.If""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:IfExitNode}"">
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
        /// 右侧参数面板模板
        /// </summary>
        private DataTemplate CreateSettingsPanelTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:IfPlugin;assembly=Plugin.If""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:IfOperationViewModel}"">
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
            <!-- 分支管理 -->
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""分支条件"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""{Binding InvertInputs, StringFormat='分支数: {0}'}""
                               FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,6""/>

                    <ItemsControl ItemsSource=""{Binding BranchInputs}"">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background=""#313244"" CornerRadius=""3""
                                        Padding=""6,4"" Margin=""0,0,0,4"">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width=""Auto""/>
                                            <ColumnDefinition Width=""*""/>
                                            <ColumnDefinition Width=""Auto""/>
                                            <ColumnDefinition Width=""Auto""/>
                                        </Grid.ColumnDefinitions>
                                        <!-- 分支类型标签 -->
                                        <Border Grid.Column=""0"" CornerRadius=""2""
                                                Padding=""4,1"" Margin=""0,0,6,0""
                                                VerticalAlignment=""Center"">
                                            <Border.Style>
                                                <Style TargetType=""Border"">
                                                    <Setter Property=""Background"" Value=""#4A90D9""/>
                                                    <Style.Triggers>
                                                        <DataTrigger Binding=""{Binding IsDeletable}"" Value=""True"">
                                                            <Setter Property=""Background"" Value=""#F9E2AF""/>
                                                        </DataTrigger>
                                                    </Style.Triggers>
                                                </Style>
                                            </Border.Style>
                                            <TextBlock Text=""{Binding BranchLabel}""
                                                       FontSize=""10"" FontWeight=""Bold""
                                                       Foreground=""#1E1E2E""/>
                                        </Border>
                                        <!-- 条件名称 -->
                                        <TextBox Grid.Column=""1""
                                                 Text=""{Binding Connector.Title, UpdateSourceTrigger=PropertyChanged}""
                                                 FontSize=""11"" Foreground=""#CDD6F4""
                                                 Background=""Transparent""
                                                 BorderThickness=""0""
                                                 VerticalAlignment=""Center""
                                                 MinWidth=""50""
                                                 CaretBrush=""#CDD6F4""/>
                                        <!-- 取反复选框 -->
                                        <CheckBox Grid.Column=""2""
                                                  IsChecked=""{Binding Invert}""
                                                  VerticalAlignment=""Center""
                                                  Margin=""4,0,4,0""
                                                  Foreground=""#A6ADC8""
                                                  FontSize=""10"">
                                            <CheckBox.Content>
                                                <TextBlock Text=""置反"" FontSize=""18""
                                                           Foreground=""#F9E2AF""/>
                                            </CheckBox.Content>
                                        </CheckBox>                                     
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>

                    <!-- 新增分支按钮 -->
                    <Button Content="" + 添加 ELSE IF 分支"" Height=""28""
                            Margin=""0,8,0,0""
                            Command=""{Binding AddBranchCommand}""
                            Background=""#45475A"" Foreground=""#F9E2AF""
                            BorderThickness=""0"" Cursor=""Hand""
                            FontSize=""11""/>
                </StackPanel>
            </Border>
<!-- 输入管理 -->
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""输入管理"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""{Binding Inputs.Count, StringFormat='当前输入数: {0}'}""
                               FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,6""/>
 
                    <ItemsControl ItemsSource=""{Binding Inputs}"">
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
                                         <Button Grid.Column=""2"" Width=""20"" Height=""20""
                                                FontSize=""10"" Background=""Transparent"" Foreground=""#1E1E2E""
                                                ToolTip=""删除此输入""
                                                BorderThickness=""0"" Cursor=""Hand""
                                                Command=""{Binding DataContext.DeleteInputCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}""
                                                Visibility=""{Binding IsProtect, Converter={x:Static local:IfOperationViewModel.BoolToVisibilityInverseConverter}}"" 
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
 
                    <Grid Margin=""0,8,0,0"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""*""/>
                            <ColumnDefinition Width=""6""/>
                            <ColumnDefinition Width=""50""/>
                        </Grid.ColumnDefinitions>
                        <ComboBox Grid.Column=""0"" x:Name=""ParamInputType"" Height=""26""
                                  FontSize=""11"" Background=""#45475A""
                                  Foreground=""#CDD6F4"" BorderThickness=""0""
                                  ItemsSource=""{x:Static local:IfOperationViewModel.AvailableDataTypes}""/>
                        <Button Grid.Column=""2"" Content=""新增"" Height=""26""
                                FontSize=""11"" Background=""#45475A""
                                Foreground=""#A6E3A1"" BorderThickness=""0""
                                Cursor=""Hand""
                                Command=""{Binding AddInputCommand}""
                                CommandParameter=""{Binding SelectedItem, ElementName=ParamInputType}""/>
                    </Grid>
                </StackPanel>
            </Border>
            <!--输出管理-->
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""输出管理"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""{Binding Outputs.Count, StringFormat='当前输出数: {0}'}""
                               FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,6""/>
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
                                        <!-- 数据类型 -->
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
                                                Visibility=""{Binding IsProtect, Converter={x:Static local:IfOperationViewModel.BoolToVisibilityInverseConverter}}"" 
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
                    <Grid Margin=""0,8,0,0"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""*""/>
                            <ColumnDefinition Width=""6""/>
                            <ColumnDefinition Width=""50""/>
                        </Grid.ColumnDefinitions>
                        <ComboBox Grid.Column=""0"" x:Name=""ParamOutputType"" Height=""26""
                                  FontSize=""11"" Background=""#45475A""
                                  Foreground=""#CDD6F4"" BorderThickness=""0""
                                  ItemsSource=""{x:Static local:IfOperationViewModel.AvailableDataTypes}""/>
                        <Button Grid.Column=""2"" Content=""新增"" Height=""26""
                                FontSize=""11"" Background=""#45475A""
                                Foreground=""#A6E3A1"" BorderThickness=""0""
                                Cursor=""Hand""
                                Command=""{Binding AddOutputCommand}""
                                CommandParameter=""{Binding SelectedItem, ElementName=ParamOutputType}""/>
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
                <Run Text=""2.条件由分支的 Bool 类型输入决定。&#10;""/>
                <Run Text=""3.子画布中右键菜单可选择返回上一级。""/>
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
}