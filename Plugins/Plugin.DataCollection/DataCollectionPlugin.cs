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

namespace DataCollectionPlugin
{
    /// <summary>
    /// 数据集合插件
    /// - 多个输入（名称可变更）
    /// - 一个输出（数据类型可选 Array 或 JSON）
    /// - 勾选"数据类型一致"时生成数组，不一致时生成 JSON
    /// </summary>
    [Plugin("数据集合",Description = "提供数据整合操作",
            Version = "1.0.0",
            Author = "LYCorePro",
            Icon = "M143.744 510.08a2.688 2.688 0 0 0 0 3.84l271.552 271.488a2.688 2.688 0 0 0 3.776 0l241.344-241.344a2.688 2.688 0 0 0 0-3.84L388.928 268.8a2.688 2.688 0 0 0-3.84 0l-241.28 241.408zm-56.512 60.352a82.688 82.688 0 0 1 0-116.864l241.28-241.408a82.688 82.688 0 0 1 116.928 0l271.552 271.552a82.688 82.688 0 0 1 0 116.928l-241.344 241.344a82.688 82.688 0 0 1-116.928 0L87.232 570.432zM399.744 510.08a2.688 2.688 0 0 0 0 3.84l241.408 241.28a2.688 2.688 0 0 0 3.776 0l241.28-241.28a2.688 2.688 0 0 0 0-3.84l-241.28-241.28a2.688 2.688 0 0 0-3.84 0l-241.28 241.28zm-56.512 60.352a82.688 82.688 0 0 1 0-116.864l241.28-241.408a82.688 82.688 0 0 1 116.992 0l241.28 241.408a82.688 82.688 0 0 1 0 116.864l-241.28 241.408a82.688 82.688 0 0 1-116.928 0L343.232 570.432z")]
    [PluginCategory("数据处理")]
    [PluginIO(InputCount = 0, OutputCount = 1, AllowMultipleInputs = true)]
    [PluginTag("数据集合")]
    public class DataCollectionPlugin : PluginBase.Base.PluginBase
    {
        public override string Name => "数据集合";
        public override Version Version => new(1, 0, 0);
        public override string Description => "数据集合插件，收集多个输入数据，支持输出数组（类型一致时）或 JSON 字符串（类型不一致时）。";
        public override string Author => "LYCorePro";

        public override Type[] OperationTypes => new[] { typeof(DataCollectionOperationViewModel) };

        public override void Initialize(IPluginHost host)
        {
            base.Initialize(host);

            var nodeTemplate = CreateNodeContentTemplate();
            if (!System.Windows.Application.Current.Resources.Contains(typeof(DataCollectionOperationViewModel)))
            {
                System.Windows.Application.Current.Resources.Add(typeof(DataCollectionOperationViewModel), nodeTemplate);
            }
        }

        public override DataTemplate? GetTemplate(Type operationType)
        {
            if (operationType == typeof(DataCollectionOperationViewModel))
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
              xmlns:local=""clr-namespace:DataCollectionPlugin;assembly=Plugin.DataCollection""
              DataType=""{x:Type local:DataCollectionOperationViewModel}"">
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
              xmlns:local=""clr-namespace:DataCollectionPlugin;assembly=Plugin.DataCollection""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:DataCollectionOperationViewModel}"">
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

            <!-- 输出模式 -->
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""输出模式"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                    <CheckBox IsChecked=""{Binding RequireSameType}""
                              Foreground=""#CDD6F4"" FontSize=""11"" Margin=""0,0,0,8"">
                        <TextBlock Text=""输入数据类型一致"" FontSize=""11"" Foreground=""#CDD6F4""/>
                    </CheckBox>
                    <TextBlock FontSize=""10"" Foreground=""#585B70""
                               TextWrapping=""Wrap""
                               Text=""勾选时：所有输入数据类型必须一致，输出为数组（List&lt;object&gt;）。&#10;不勾选时：自动收集所有输入值，输出为 JSON 字符串。""/>
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

                    <Grid Margin=""0,8,0,0"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""*""/>
                            <ColumnDefinition Width=""6""/>
                            <ColumnDefinition Width=""50""/>
                        </Grid.ColumnDefinitions>
                        <ComboBox Grid.Column=""0"" x:Name=""InputTypeCombo"" Height=""26""
                                  FontSize=""11"" Background=""#45475A""
                                  Foreground=""#CDD6F4"" BorderThickness=""0""
                                  ItemsSource=""{x:Static local:DataCollectionOperationViewModel.AvailableDataTypes}""/>
                        <Button Grid.Column=""2"" Content=""新增"" Height=""26""
                                FontSize=""11"" Background=""#45475A""
                                Foreground=""#A6E3A1"" BorderThickness=""0""
                                Cursor=""Hand""
                                Command=""{Binding AddInputCommand}""
                                CommandParameter=""{Binding SelectedItem, ElementName=InputTypeCombo}""/>
                    </Grid>
                </StackPanel>
            </Border>

            <!-- 输出设置 -->
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""输出设置"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""输出数据类型"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,4""/>
                    <ComboBox ItemsSource=""{x:Static local:DataCollectionOperationViewModel.OutputTypeOptions}""
                              SelectedItem=""{Binding OutputType, UpdateSourceTrigger=PropertyChanged}""
                              Height=""28""
                              Background=""#313244"" Foreground=""#CDD6F4""
                              BorderThickness=""0"" Margin=""0,0,0,8""/>
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
                    <TextBlock Text=""输出数据"" FontSize=""11"" Foreground=""#A6E3A1""
                               Margin=""0,8,0,4""/>
                    <Border Background=""#1E2A2E"" CornerRadius=""3""
                            Padding=""6,4"">
                        <TextBlock FontSize=""11"" Foreground=""#CDD6F4""
                                   TextWrapping=""Wrap""
                                   Text=""{Binding Outputs[0].Value, TargetNullValue='(无数据)'}"" />
        
                            </Border>
        
                        </StackPanel>
        
                    </Border>
        

            <TextBlock FontSize=""11"" Foreground=""#585B70"" TextWrapping=""Wrap"">
                <Run Text=""提示:(操作步骤)&#10;""/>
                <Run Text=""1.勾选'数据类型一致'时，所有输入必须为同一类型，输出为数组。&#10;"" />
                <Run Text=""2.不勾选时自动收集，输出为 JSON。""/>
            </TextBlock>
        </StackPanel>
    </ScrollViewer>
 </TabItem>
    </TabControl>
</DataTemplate> ";
        
    using var stringReader = new System.IO.StringReader(xaml);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (DataTemplate)System.Windows.Markup.XamlReader.Load(xmlReader);
        }
    }
}