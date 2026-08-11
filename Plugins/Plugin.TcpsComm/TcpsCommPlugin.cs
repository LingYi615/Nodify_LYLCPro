using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PluginBase.Attributes;
using PluginBase.Base;
using PluginBase.Interfaces;

namespace TcpsCommPlugin
{
    /// <summary>
    /// TCP 通讯插件
    /// 提供 TCP 客户端/服务器、UDP、串口、Modbus、S7、OPC UA 等多种通讯协议的连接功能
    /// 输出 ICommunication 通讯实例，供下游节点使用
    /// </summary>
    [Plugin("TCP 通讯",Description = "提供TCP通讯多种协议连接功能",
            Version = "1.0.0",
            Author = "LYCorePro",
            Icon = "M463.36 181.0432 A268.4416 268.4416 0 0 1 843.008 560.64 L738.048 665.6 L665.6 593.2032 L770.56 488.2432 A166.0416 166.0416 0 0 0 535.7568 253.44 L430.848 358.4 L358.4 286.0032 L463.36 181.0432 Z M488.2432 770.56 L593.2032 665.6 L665.6 737.9968 L560.64 842.9568 A268.3904 268.3904 0 1 1 181.0432 463.36 L286.0032 358.4 L358.4 430.7968 L253.44 535.7568 A166.0416 166.0416 0 1 0 488.2432 770.56 Z M394.5984 701.7984 L701.7984 394.5984 L629.4016 322.2016 L322.2016 629.4016 L394.5984 701.7984 Z")]
    [PluginCategory("通讯")]
    [PluginIO(InputCount = 0, OutputCount = 1)]
    [PluginTag("TCP通讯")]
    public class TcpsCommPlugin : PluginBase.Base.PluginBase
    {
        /// <summary>Null 转 Visibility 转换器（null → Collapsed，非 null → Visible）</summary>
        public static readonly IValueConverter NullToVisibilityConverter = new NullToVisibilityConverterImpl();

        public override string Name => "TCP 通讯";
        public override Version Version => new(1, 0, 0);
        public override string Description => "TCP/UDP/串口/Modbus/S7/OPC UA 通讯连接插件，输出通讯实例";
        public override string Author => "LYCorePro";

        public override Type[] OperationTypes => new[] { typeof(TcpsCommOperationViewModel) };

        public override void Initialize(IPluginHost host)
        {
            base.Initialize(host);

            // 注册节点内容模板（用 DataType 隐式匹配，显示在节点内部）
            var nodeTemplate = CreateNodeContentTemplate();
            if (!Application.Current.Resources.Contains(typeof(TcpsCommOperationViewModel)))
            {
                Application.Current.Resources.Add(typeof(TcpsCommOperationViewModel), nodeTemplate);
            }
        }

        public override DataTemplate? GetTemplate(Type operationType)
        {
            if (operationType == typeof(TcpsCommOperationViewModel))
            {
                return CreateSettingsPanelTemplate();
            }
            return base.GetTemplate(operationType);
        }

        /// <summary>
        /// 创建节点内容显示模板（显示在节点内部的小型信息区）
        /// 与参数面板模板分离，避免冲突
        /// </summary>
        private DataTemplate CreateNodeContentTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:TcpsCommPlugin;assembly=Plugin.TcpsComm""
              DataType=""{x:Type local:TcpsCommOperationViewModel}"">
    <Border Background=""#181825"" CornerRadius=""4"" Padding=""8,6"" Margin=""4,2"">
        <TextBlock Text=""{Binding InstanceInfo}""
                   FontSize=""11"" Foreground=""#A6ADC8""
                   TextWrapping=""Wrap""
                   FontFamily=""Consolas""/>
    </Border>
</DataTemplate>";

            using var stringReader = new System.IO.StringReader(xaml);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (DataTemplate)System.Windows.Markup.XamlReader.Load(xmlReader);
        }

        /// <summary>
        /// 创建右侧参数面板的 DataTemplate
        /// TabControl 结构：Tab1-参数设定 / Tab2-当前数据
        /// 此模板会被注册到 Application.Current.Resources 的复合 Key 下
        /// </summary>
        private DataTemplate CreateSettingsPanelTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:TcpsCommPlugin;assembly=Plugin.TcpsComm""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:TcpsCommOperationViewModel}"">
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
                <StackPanel Margin=""0,8,0,0"" >
                    <!-- 快速选择预置协议 -->
                    <TextBlock Text=""快速选择协议"" FontSize=""12""
                               Foreground=""#A6ADC8"" Margin=""0,0,0,4""/>
                    <ComboBox ItemsSource=""{x:Static local:ProtocolConfig.PresetProtocols}""
                              DisplayMemberPath=""Name""
                              SelectedItem=""{Binding SelectedPreset, Mode=TwoWay}""
                              Height=""28""
                              Margin=""0,0,0,12""/>

                    <Separator Background=""#313244"" Margin=""0,0,0,12""/>

                    <!-- 通讯类型 -->
                    <TextBlock Text=""通讯类型"" FontSize=""12""
                               Foreground=""#A6ADC8"" Margin=""0,0,0,4""/>
                    <ComboBox ItemsSource=""{x:Static local:TcpsCommOperationViewModel.CommunicationTypes}""
                              SelectedItem=""{Binding CommunicationType, Mode=TwoWay}""
                              Height=""28""
                              Margin=""0,0,0,12""/>

                    <!-- 网络参数 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <TextBlock Text=""网络参数"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                            <TextBlock Text=""远程 IP 地址"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2"" />
                            <TextBox Text=""{Binding RemoteIP, UpdateSourceTrigger=PropertyChanged}""
                                     IsEnabled=""{Binding IsNotHttpServer}""
                                     Height=""26"" Margin=""0,0,0,8"" Width=""300""
                                     Background=""#313244"" Foreground=""#CDD6F4""
                                     BorderThickness=""0"" CaretBrush=""#CDD6F4""/>
                            <TextBlock Text=""远程端口"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                            <TextBox Text=""{Binding RemotePort, UpdateSourceTrigger=PropertyChanged}""
                                     Height=""26"" Margin=""0,0,0,8"" Width=""300""
                                     Background=""#313244"" Foreground=""#CDD6F4""
                                     BorderThickness=""0"" CaretBrush=""#CDD6F4""/>
                            <TextBlock Text=""本地端口"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                            <TextBox Text=""{Binding LocalPort, UpdateSourceTrigger=PropertyChanged}""
                                     Height=""26"" Margin=""0,0,0,0"" Width=""300""
                                     Background=""#313244"" Foreground=""#CDD6F4""
                                     BorderThickness=""0"" CaretBrush=""#CDD6F4""/>
                        </StackPanel>
                    </Border>

                    <!-- 超时与重试 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <TextBlock Text=""超时与重试"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""*""/>
                                    <ColumnDefinition Width=""10""/>
                                    <ColumnDefinition Width=""*""/>
                                </Grid.ColumnDefinitions>
                                <StackPanel Grid.Column=""0"">
                                    <TextBlock Text=""超时 (ms)"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                                    <TextBox Text=""{Binding Timeout, UpdateSourceTrigger=PropertyChanged}""
                                             Height=""26"" Background=""#313244"" Foreground=""#CDD6F4"" Width=""150""
                                             BorderThickness=""0"" CaretBrush=""#CDD6F4""/>
                                </StackPanel>
                                <StackPanel Grid.Column=""2"">
                                    <TextBlock Text=""重试次数"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                                    <TextBox Text=""{Binding RetryCount, UpdateSourceTrigger=PropertyChanged}""
                                             Height=""26"" Background=""#313244"" Foreground=""#CDD6F4""  Width=""150""
                                             BorderThickness=""0"" CaretBrush=""#CDD6F4""/>
                                </StackPanel>
                            </Grid>
                        </StackPanel>
                    </Border>

                    <!-- 数据格式 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <TextBlock Text=""数据格式"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                            <CheckBox IsChecked=""{Binding IsSendByHex}"" Foreground=""#CDD6F4"" FontSize=""11"" Margin=""0,0,0,4"">
                                <TextBlock Text=""十六进制发送"" FontSize=""11"" Foreground=""#CDD6F4""/>
                            </CheckBox>
                            <CheckBox IsChecked=""{Binding IsReceivedByHex}"" Foreground=""#CDD6F4"" FontSize=""11"">
                                <TextBlock Text=""十六进制接收"" FontSize=""11"" Foreground=""#CDD6F4""/>
                            </CheckBox>
                        </StackPanel>
                    </Border>

                    <!-- 连接状态 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <TextBlock Text=""连接状态"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""Auto""/>
                                    <ColumnDefinition Width=""*""/>
                                </Grid.ColumnDefinitions>
                                <Ellipse Grid.Column=""0"" Width=""10"" Height=""10"" Margin=""0,0,8,0"" VerticalAlignment=""Center"">
                                    <Ellipse.Style>
                                        <Style TargetType=""Ellipse"">
                                            <Setter Property=""Fill"" Value=""#6C7086""/>
                                            <Style.Triggers>
                                                <DataTrigger Binding=""{Binding IsConnected}"" Value=""True"">
                                                    <Setter Property=""Fill"" Value=""#A6E3A1""/>
                                                </DataTrigger>
                                            </Style.Triggers>
                                        </Style>
                                    </Ellipse.Style>
                                </Ellipse>
                                <TextBlock Grid.Column=""1"" FontSize=""11"" VerticalAlignment=""Center"">
                                    <TextBlock.Style>
                                        <Style TargetType=""TextBlock"">
                                            <Setter Property=""Text"" Value=""未连接""/>
                                            <Setter Property=""Foreground"" Value=""#6C7086""/>
                                            <Style.Triggers>
                                                <DataTrigger Binding=""{Binding IsConnected}"" Value=""True"">
                                                    <Setter Property=""Text"" Value=""已连接""/>
                                                    <Setter Property=""Foreground"" Value=""#A6E3A1""/>
                                                </DataTrigger>
                                            </Style.Triggers>
                                        </Style>
                                    </TextBlock.Style>
                                </TextBlock>
                            </Grid>
                        </StackPanel>
                    </Border>

                    <TextBlock Text=""配置通讯参数后，点击节点执行按钮即可建立连接并输出 ICommunication 实例。""
                               FontSize=""11"" Foreground=""#585B70"" TextWrapping=""Wrap""/>
                </StackPanel>
            </ScrollViewer>
        </TabItem>

        <!-- ==================== Tab 2: 当前数据 ==================== -->
        <TabItem Header=""当前数据"">
            <ScrollViewer VerticalScrollBarVisibility=""Auto"">
                <StackPanel Margin=""0,8,0,0"">
                    <!-- 输入信息（此插件无输入，不显示） -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12""
                            Visibility=""{Binding InputInfo, Converter={x:Static local:TcpsCommPlugin.NullToVisibilityConverter}, FallbackValue=Collapsed}"">
                        <StackPanel>
                            <TextBlock Text=""输入信息"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                            <TextBox Text=""{Binding InputInfo, Mode=OneWay}""
                                     IsReadOnly=""True""
                                     Background=""#313244"" Foreground=""#CDD6F4""
                                     BorderThickness=""0"" CaretBrush=""#CDD6F4""
                                     FontFamily=""Consolas"" FontSize=""11""
                                     TextWrapping=""Wrap""
                                     MinHeight=""60""
                                     AcceptsReturn=""True""
                                     VerticalScrollBarVisibility=""Auto""/>
                        </StackPanel>
                    </Border>

                    <!-- 输出信息 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <TextBlock Text=""输出信息"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                           <TextBlock Text=""{Binding OutputInfo, Mode=OneWay}""
                                           Foreground=""#CDD6F4""
                                           FontFamily=""Consolas"" FontSize=""11""
                                           TextWrapping=""Wrap""
                                           IsHitTestVisible=""True""/>
                        </StackPanel>
                    </Border>

                    <TextBlock Text=""执行后显示通讯实例的 JSON 输出数据。""
                               FontSize=""11"" Foreground=""#585B70"" TextWrapping=""Wrap""/>
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

    /// <summary>
    /// Null 转 Visibility 转换器：null → Collapsed，非 null → Visible
    /// </summary>
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
