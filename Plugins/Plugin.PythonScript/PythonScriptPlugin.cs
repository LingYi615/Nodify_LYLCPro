using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using PluginBase.Attributes;
using PluginBase.Base;
using PluginBase.Interfaces;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PythonScriptPlugin
{
    /// <summary>
    /// Python 脚本插件
    /// - 初始无输入/输出，支持动态添加
    /// - 输入/输出数据类型可自定义修改
    /// - 支持代码编辑和实时校验
    /// - 使用内置 Python 引擎执行（支持 Python 核心库）
    /// </summary>
    [Plugin("Python脚本", Description = "编辑Python脚本",
        Version = "1.0.0",
        Author = "LYCorePro",
        Icon = "M551.384615 0H472.615385a157.538462 157.538462 0 0 0-157.538462 157.538462v78.76923h236.307692v78.769231H157.538462a157.538462 157.538462 0 0 0-157.538462 157.538462v78.76923a157.538462 157.538462 0 0 0 157.538462 157.538462h78.76923v-59.076923A120.516923 120.516923 0 0 1 354.461538 527.753846h315.076924a40.172308 40.172308 0 0 0 39.384615-40.96V157.538462a157.538462 157.538462 0 0 0-157.538462-157.538462zM433.230769 157.538462a39.384615 39.384615 0 1 1 39.384616-39.384616 39.384615 39.384615 0 0 1-39.384616 39.384616z M866.461538 315.076923h-78.76923v171.716923a120.516923 120.516923 0 0 1-118.153846 122.092308h-315.076924a40.172308 40.172308 0 0 0-39.384615 40.96V866.461538a157.538462 157.538462 0 0 0 157.538462 157.538462h78.76923a157.538462 157.538462 0 0 0 157.538462-157.538462v-78.76923H472.615385V708.923077h304.836923a325.316923 325.316923 0 0 1 85.070769-130.756923 242.609231 242.609231 0 0 1 157.538461-65.378462V472.615385A157.538462 157.538462 0 0 0 866.461538 315.076923z m-275.692307 551.384615a39.384615 39.384615 0 1 1-39.384616 39.384616 39.384615 39.384615 0 0 1 39.384616-39.384616z M889.304615 607.310769A319.803077 319.803077 0 0 0 795.569231 787.692308a236.307692 236.307692 0 0 0 17.329231 172.504615 140.209231 140.209231 0 0 0 128.393846 62.227692 193.772308 193.772308 0 0 0 127.606154-46.473846 267.027692 267.027692 0 0 0 78.76923-119.729231H1063.384615a185.895385 185.895385 0 0 1-32.295384 51.987693 100.036923 100.036923 0 0 1-78.769231 32.295384 70.892308 70.892308 0 0 1-66.166154-37.80923 169.353846 169.353846 0 0 1-2.363077-115.003077 251.273846 251.273846 0 0 1 50.412308-117.366154A106.338462 106.338462 0 0 1 1016.910769 630.153846a68.529231 68.529231 0 0 1 64.590769 29.144616 107.913846 107.913846 0 0 1 11.815385 48.836923h86.646154a148.873846 148.873846 0 0 0-11.815385-84.283077A136.270769 136.270769 0 0 0 1029.513846 551.384615a203.224615 203.224615 0 0 0-140.209231 55.926154z")]
    [PluginCategory("脚本")]
    [PluginIO(InputCount = 0, OutputCount = 1, AllowMultipleInputs = true, AllowMultipleOutputs = true)]
    [PluginTag("PythonScript")]
    public class PythonScriptPlugin : PluginBase.Base.PluginBase
    {
        public override string Name => "Python脚本";
        public override Version Version => new(1, 0, 0);
        public override string Description => "Python 脚本执行插件，支持代码编辑、实时校验，使用内置 Python 引擎执行。输入输出类型可自定义。";
        public override string Author => "LYCorePro";

        public override Type[] OperationTypes => new[] {typeof(PythonScriptOperationModelView)};

        public override void Initialize(IPluginHost host)
        {
            base.Initialize(host);

            var nodeTemplate = CreateNodeContentTemplate();
            if (!System.Windows.Application.Current.Resources.Contains(typeof(PythonScriptOperationModelView)))
            {
                System.Windows.Application.Current.Resources.Add(typeof(PythonScriptOperationModelView), nodeTemplate);
           }
       }

        public override DataTemplate? GetTemplate(Type operationType)
        {
            if (operationType == typeof(PythonScriptOperationModelView))
            {
                return CreateSettingsPanelTemplate();
           }
            return base.GetTemplate(operationType);
       }

        private DataTemplate CreateNodeContentTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:PythonScriptPlugin;assembly=Plugin.PythonScript""
              DataType=""{x:Type local:PythonScriptOperationModelView}"">
    <Border Background=""#181825"" CornerRadius=""4"" Padding=""8,6"" Margin=""4,2""
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

        private DataTemplate CreateSettingsPanelTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:PythonScriptPlugin;assembly=Plugin.PythonScript""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:PythonScriptOperationModelView}"">
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

            <!-- Python 代码编辑器 -->
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <Grid Margin=""0,0,0,8"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""*""/>
                            <ColumnDefinition Width=""Auto""/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column=""0"" Text=""Python 代码"" FontSize=""12"" FontWeight=""Bold""
                                   Foreground=""#89B4FA"" VerticalAlignment=""Center""/>
                        <Button Grid.Column=""1"" Content=""校验"" Height=""24""
                                FontSize=""10"" Background=""#45475A""
                                Foreground=""#F9E2AF"" BorderThickness=""0""
                                Cursor=""Hand"" Padding=""10,0""
                                Command=""{Binding ValidateCommand}""/>
                    </Grid>
                    <TextBox Text=""{Binding PythonCode, UpdateSourceTrigger=PropertyChanged}""
                             AcceptsReturn=""True""
                             TextWrapping=""NoWrap""
                             MinHeight=""200""
                             MaxHeight=""400""
                             MinWidth=""390""
                             FontFamily=""Consolas"" FontSize=""12""
                             Background=""#1E1E2E"" Foreground=""#CDD6F4""
                             BorderBrush=""#313244"" BorderThickness=""1""
                             CaretBrush=""#CDD6F4""
                             HorizontalScrollBarVisibility=""Auto""
                             VerticalScrollBarVisibility=""Auto""
                             ToolTip=""输入 Python 脚本代码。支持的模块: math, json, re, random, datetime, collections, itertools, functools。通过 result 变量返回结果，或通过输入变量名引用输入值。""/>
                    <!-- 校验结果 -->
                    <Border Background=""#DC3535"" CornerRadius=""3""
                            Padding=""6,4"" Margin=""0,4,0,0""
                            Visibility=""{Binding IsValidationError, Converter={x:Static local:PythonScriptPlugin.BoolToVisibilityConverter}}"">
                        <TextBlock Text=""{Binding ValidationResult}""
                                   FontSize=""10"" Foreground=""White""
                                   FontFamily=""Consolas"" TextWrapping=""Wrap""/>
                    </Border>
                    <TextBlock Text=""{Binding CodePreview}""
                               FontSize=""10"" Foreground=""#585B70""
                               FontFamily=""Consolas"" Margin=""0,4,0,0""/>
                </StackPanel>
            </Border>

            <!-- 输入管理 -->
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""输入管理"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""{Binding Inputs.Count, StringFormat='{}当前输入数: {0}'}""
                               FontSize = ""11"" Foreground = ""#6C7086"" Margin=""0,0,0,6""/>

                    <ItemsControl ItemsSource = ""{Binding Inputs}"">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background = ""#313244"" CornerRadius=""3""
                                        Padding = ""6,4"" Margin = ""0,0,0,4"">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width = "" * ""/>
                                            <ColumnDefinition Width = ""Auto""/>
                                            <ColumnDefinition Width = ""Auto""/>
                                        </Grid.ColumnDefinitions>
                                        <TextBox Grid.Column = ""0""
                                                 Text = ""{Binding Title, UpdateSourceTrigger = PropertyChanged}""
                                                 FontSize = ""11"" Foreground = ""#CDD6F4""
                                                 Background = ""Transparent""
                                                 BorderThickness = ""0""
                                                 VerticalAlignment = ""Center""
                                                 MinWidth = ""40""
                                                 CaretBrush = ""#CDD6F4""
                                                 ToolTip = ""输入变量名（在代码中通过此名称引用）""/>
                                        <ComboBox Grid.Column = ""1""
                                                  ItemsSource = ""{x:Static local:PythonScriptOperationModelView.AvailableDataTypes}""
                                                  SelectedItem = ""{Binding DataType, UpdateSourceTrigger = PropertyChanged}""
                                                  Width = ""100""
                                                  Height = ""22""
                                                  FontSize = ""10""   VerticalAlignment = ""Center""
                                                  Background = ""#45475A""
                                                  Foreground = ""#CDD6F4""
                                                  BorderThickness = ""0""
                                                  Margin = ""6,0,30,0""/>
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

                    <Grid Margin = ""0,8,0,0"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width = "" * ""/>
                            <ColumnDefinition Width = ""6""/>
                            <ColumnDefinition Width = ""50""/>
                        </Grid.ColumnDefinitions>
                        <ComboBox Grid.Column = ""0"" x:Name = ""InputTypeCombo"" Height = ""26""
                                  FontSize = ""11"" Background = ""#45475A""
                                  Foreground = ""#CDD6F4"" BorderThickness=""0""
                                  ItemsSource = ""{x:Static local:PythonScriptOperationModelView.AvailableDataTypes}""/>
                        <Button Grid.Column = ""2"" Content = ""新增"" Height = ""26""
                                FontSize = ""11"" Background = ""#45475A""
                                Foreground = ""#A6E3A1"" BorderThickness=""0""
                                Cursor = ""Hand""
                                Command = ""{Binding AddInputCommand}""
                                CommandParameter = ""{Binding SelectedItem, ElementName = InputTypeCombo}""/>
                    </Grid>
                </StackPanel>
            </Border>

            <!-- 输出管理 -->
            <Border Background = ""#181825"" BorderBrush=""#313244""
                    BorderThickness = ""1"" CornerRadius = ""4""
                    Padding = ""10"" Margin = ""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text = ""输出管理"" FontSize = ""12"" FontWeight = ""Bold""
                               Foreground = ""#89B4FA"" Margin=""0,0,0,8""/>
                    <TextBlock Text = ""{Binding Outputs.Count, StringFormat = '{}当前输出数: {0}'}""
                               FontSize = ""11"" Foreground = ""#6C7086"" Margin=""0,0,0,6""/>

                    <ItemsControl ItemsSource = ""{Binding Outputs}"">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background = ""#313244"" CornerRadius=""3""
                                        Padding = ""6,4"" Margin = ""0,0,0,4"">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width = "" * ""/>
                                            <ColumnDefinition Width = ""Auto""/>
                                            <ColumnDefinition Width = ""Auto""/>
                                        </Grid.ColumnDefinitions>
                                        <TextBox Grid.Column = ""0""
                                                 Text = ""{Binding Title, UpdateSourceTrigger = PropertyChanged}""
                                                 FontSize = ""11"" Foreground = ""#CDD6F4""
                                                 Background = ""Transparent""
                                                 BorderThickness = ""0""
                                                 VerticalAlignment = ""Center""
                                                 MinWidth = ""40""
                                                 CaretBrush = ""#CDD6F4""/>
                                        <ComboBox Grid.Column = ""1""
                                                  ItemsSource = ""{x:Static local:PythonScriptOperationModelView.AvailableDataTypes}""
                                                  SelectedItem = ""{Binding DataType, UpdateSourceTrigger = PropertyChanged}""
                                                  Width = ""100""
                                                  Height = ""22""
                                                  FontSize = ""10"" VerticalAlignment = ""Center""
                                                  Background = ""#45475A""
                                                  Foreground = ""#CDD6F4""
                                                  BorderThickness = ""0""
                                                  Margin = ""6,0,30,0""/>
                                        <Button Grid.Column=""2"" Width=""20"" Height=""20""
                                                FontSize=""10"" Background=""Transparent"" Foreground=""#1E1E2E""
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

                    <Grid Margin = ""0,8,0,0"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width = "" * ""/>
                            <ColumnDefinition Width = ""6""/>
                            <ColumnDefinition Width = ""50""/>
                        </Grid.ColumnDefinitions>
                        <ComboBox Grid.Column = ""0"" x:Name = ""OutputTypeCombo"" Height = ""26""
                                  FontSize = ""11"" Background = ""#45475A""
                                  Foreground = ""#CDD6F4"" BorderThickness=""0""
                                  ItemsSource = ""{x:Static local:PythonScriptOperationModelView.AvailableDataTypes}""/>
                        <Button Grid.Column = ""2"" Content = ""新增"" Height = ""26""
                                FontSize = ""11"" Background = ""#45475A""
                                Foreground = ""#A6E3A1"" BorderThickness=""0""
                                Cursor = ""Hand""
                                Command = ""{Binding AddOutputCommand}""
                                CommandParameter = ""{Binding SelectedItem, ElementName = OutputTypeCombo}""/>
                    </Grid>
                </StackPanel>
            </Border>

            <!-- 使用说明 -->
            <Border Background = ""#181825"" BorderBrush=""#313244""
                    BorderThickness = ""1"" CornerRadius = ""4""
                    Padding = ""10"" Margin = ""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text = ""使用说明(内部嵌入了Python，需要安装Python 3.6+)"" FontSize = ""12"" FontWeight = ""Bold""
                               Foreground = ""#F9E2AF"" Margin=""0,0,0,8""/>
                    <TextBlock Text = ""1.输入变量名即为代码中的变量名，可直接引用""
                               FontSize = ""10"" Foreground = ""#A6ADC8""
                               FontFamily = ""Consolas"" Margin = ""0,0,0,2""/>
                    <TextBlock Text = ""2.使用 result 变量返回结果（如 result = x + 1）""
                               FontSize = ""10"" Foreground = ""#A6ADC8""
                               FontFamily = ""Consolas"" Margin = ""0,0,0,2""/>
                    <TextBlock Text = ""3.支持的模块: math, json, re, random, datetime, collections, itertools, functools""
                               FontSize = ""10"" Foreground = ""#A6ADC8""
                               FontFamily = ""Consolas"" Margin = ""0,0,0,2""/>
                    <TextBlock Text = ""4.支持的内置函数: len, str, int, float, bool, list, dict, tuple, set, min, max, sum, abs, round, pow, sorted, reversed, range, enumerate, zip, filter, map, ord, chr, bin, hex, oct""
                               FontSize = ""10"" Foreground = ""#A6ADC8""
                               FontFamily = ""Consolas"" Margin = ""0,0,0,2""/>
                    <TextBlock Text = ""5.支持的字符串方法: upper, lower, strip, replace, split, join, startswith, endswith, find, count, format""
                               FontSize = ""10"" Foreground = ""#A6ADC8""
                               FontFamily = ""Consolas"" Margin = ""0,0,0,2""/>
                    <TextBlock Text = ""6.支持的列表方法: append, extend, insert, remove, pop, clear, index, sort, reverse, copy""
                               FontSize = ""10"" Foreground = ""#A6ADC8""
                               FontFamily = ""Consolas"" Margin = ""0,0,0,2""/>
                    <TextBlock Text = ""7.支持的字典方法: get, keys, values, items, update""
                               FontSize = ""10"" Foreground = ""#A6ADC8""
                               FontFamily = ""Consolas""/>
                </StackPanel>
            </Border>

            <TextBlock Text = ""提示: 点击'校验'按钮检查代码语法。代码执行时会将输入变量注入到执行环境，result 变量值将作为输出。""
                       FontSize = ""11"" Foreground = ""#585B70"" TextWrapping=""Wrap""/>
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
        </StackPanel>
    </ScrollViewer>
</TabItem>
    </TabControl>
</DataTemplate> ";

            using var stringReader = new System.IO.StringReader(xaml);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (DataTemplate)System.Windows.Markup.XamlReader.Load(xmlReader);
       }

        public static readonly IValueConverter BoolToVisibilityConverter = new BooleanToVisibilityConverter();
   }
}