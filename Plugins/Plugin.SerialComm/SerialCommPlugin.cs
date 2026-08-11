using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PluginBase.Attributes;
using PluginBase.Base;
using PluginBase.Interfaces;

namespace SerialCommPlugin
{
    /// <summary>
    /// 串口通讯插件
    /// 提供串口通讯连接功能，输出 ICommunication 通讯实例
    /// </summary>
    [Plugin("串口通讯", Description = "提供串口通讯连接功能",
            Version = "1.0.0",
            Author = "LYCorePro",
            Icon = "M228.693333 256h598.613334c81.92 0 142.506667 75.52 125.013333 155.306667l-35.413333 160.426666a180.096 180.096 0 0 0-143.36-70.4c-100.266667 0-181.333333 81.066667-181.333334 181.333334 0 39.68 12.8 76.373333 34.56 106.666666H290.133333c-60.16 0-111.786667-41.813333-125.013333-100.693333l-61.44-277.333333A128.042667 128.042667 0 0 1 228.693333 256z m-9.813333 160a53.333333 53.333333 0 1 0 106.666667 0 53.333333 53.333333 0 0 0-106.666667 0z m170.666667 0a53.333333 53.333333 0 1 0 106.666666 0 53.333333 53.333333 0 0 0-106.666666 0z m170.666666 0a53.333333 53.333333 0 1 0 106.666667 0 53.333333 53.333333 0 0 0-106.666667 0z m170.666667 0a53.333333 53.333333 0 1 0 106.666667 0 53.333333 53.333333 0 0 0-106.666667 0z m-426.666667 192a53.333333 53.333333 0 1 0 106.666667 0 53.333333 53.333333 0 0 0-106.666667 0z m170.666667 0a53.333333 53.333333 0 1 0 106.666667 0 53.333333 53.333333 0 0 0-106.666667 0zM892.586667 649.813333l-16.64-2.133333a11.050667 11.050667 0 0 1-8.106667-6.826667c-3.413333-8.106667-7.68-15.786667-12.8-22.613333-2.133333-2.986667-2.986667-7.253333-1.28-10.666667l6.4-16.64c1.706667-4.693333 0-10.666667-4.693333-13.226666l-35.84-21.76a10.581333 10.581333 0 0 0-13.653334 2.56l-10.24 14.08c-2.133333 2.986667-5.973333 4.266667-9.813333 3.84-3.84-0.426667-8.106667-0.853333-12.373333-0.853334-4.266667 0-8.533333 0.426667-12.373334 0.853334-3.84 0.426667-7.68-0.853333-9.813333-3.84l-10.666667-14.08c-2.986667-4.266667-8.533333-5.12-13.226666-2.56l-36.266667 21.76c-4.266667 2.56-5.973333 8.533333-4.266667 13.226666l6.4 16.64c1.28 3.413333 0.853333 7.253333-1.706666 10.666667-4.693333 6.826667-8.96 14.506667-12.373334 22.613333-1.28 3.413333-4.266667 5.973333-8.106666 6.826667l-16.64 2.133333a11.093333 11.093333 0 0 0-8.96 11.093334v43.52c0 5.12 3.84 9.813333 8.96 10.666666l16.64 2.133334c3.413333 0.853333 6.826667 3.413333 8.106666 6.826666 3.413333 8.106667 7.68 15.786667 12.373334 22.613334 2.56 3.413333 2.986667 7.253333 1.706666 10.666666l-6.4 16.64c-2.133333 4.693333 0 10.666667 4.266667 13.226667l36.266667 21.76c4.693333 2.56 10.24 1.706667 13.226666-2.56l10.666667-14.08c2.133333-2.986667 5.973333-4.266667 9.386667-3.84 4.266667 0.426667 8.533333 0.853333 12.8 0.853333 4.266667 0 8.106667-0.426667 12.373333-0.853333 3.84-0.426667 7.68 0.853333 9.813333 3.84l10.24 14.08c3.413333 4.266667 8.96 5.12 13.226667 2.56l36.266667-21.76c4.693333-2.56 6.4-8.533333 4.266666-13.226667l-5.973333-16.64c-1.706667-3.413333-0.853333-7.253333 1.28-10.666666 5.12-6.826667 9.386667-14.506667 12.8-22.613334 1.28-3.413333 4.266667-5.973333 8.106667-6.826666l16.64-2.133334c5.12-0.853333 8.96-5.546667 8.96-10.666666v-43.52a11.093333 11.093333 0 0 0-8.96-11.093334z m-119.04 75.093334c-22.613333 0-40.533333-19.2-40.533334-42.24 0-23.466667 17.92-42.666667 40.533334-42.666667 22.186667 0 40.533333 19.2 40.533333 42.666667 0 23.04-18.346667 42.24-40.533333 42.24z")]
    [PluginCategory("通讯")]
    [PluginIO(InputCount = 0, OutputCount = 1)]
    [PluginTag("串口")]
    public class SerialCommPlugin : PluginBase.Base.PluginBase
    {
        public static readonly IValueConverter NullToVisibilityConverter = new NullToVisibilityConverterImpl();

        public override string Name => "串口通讯";
        public override Version Version => new(1, 0, 0);
        public override string Description => "串口通讯连接插件，输出通讯实例";
        public override string Author => "LYCorePro";

        public override Type[] OperationTypes => new[] { typeof(SerialCommOperationViewModel) };

        public override void Initialize(IPluginHost host)
        {
            base.Initialize(host);

            var nodeTemplate = CreateNodeContentTemplate();
            if (!Application.Current.Resources.Contains(typeof(SerialCommOperationViewModel)))
            {
                Application.Current.Resources.Add(typeof(SerialCommOperationViewModel), nodeTemplate);
            }
        }

        public override DataTemplate? GetTemplate(Type operationType)
        {
            if (operationType == typeof(SerialCommOperationViewModel))
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
              xmlns:local=""clr-namespace:SerialCommPlugin;assembly=Plugin.SerialComm""
              DataType=""{x:Type local:SerialCommOperationViewModel}"">
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

        private DataTemplate CreateSettingsPanelTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:SerialCommPlugin;assembly=Plugin.SerialComm""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:SerialCommOperationViewModel}"">
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
                    <!-- 串口参数 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <TextBlock Text=""串口参数"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>

                            <TextBlock Text=""串口号"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                            <ComboBox ItemsSource=""{x:Static local:SerialCommOperationViewModel.PortNames}""
                                      Text=""{Binding PortName, UpdateSourceTrigger=PropertyChanged}""
                                      IsEditable=""True""
                                      Height=""28"" Margin=""0,0,0,8""
                                      Background=""#313244"" Foreground=""#CDD6F4""
                                      BorderThickness=""0""/>

                            <TextBlock Text=""波特率"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                            <ComboBox ItemsSource=""{x:Static local:SerialCommOperationViewModel.BaudRates}""
                                      Text=""{Binding BaudRate, UpdateSourceTrigger=PropertyChanged}""
                                      IsEditable=""True""
                                      Height=""28"" Margin=""0,0,0,8""
                                      Background=""#313244"" Foreground=""#CDD6F4""
                                      BorderThickness=""0""/>

                            <Grid Margin=""0,0,0,8"">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""*""/>
                                    <ColumnDefinition Width=""10""/>
                                    <ColumnDefinition Width=""*""/>
                                </Grid.ColumnDefinitions>
                                <StackPanel Grid.Column=""0"">
                                    <TextBlock Text=""数据位"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                                    <ComboBox ItemsSource=""{x:Static local:SerialCommOperationViewModel.DataBitsOptions}""
                                              SelectedItem=""{Binding DataBits}""
                                              Height=""28""
                                              Background=""#313244"" Foreground=""#CDD6F4""
                                              BorderThickness=""0""/>
                                </StackPanel>
                                <StackPanel Grid.Column=""2"">
                                    <TextBlock Text=""停止位"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                                    <ComboBox ItemsSource=""{x:Static local:SerialCommOperationViewModel.StopBitsOptions}""
                                              SelectedItem=""{Binding StopBits}""
                                              Height=""28""
                                              Background=""#313244"" Foreground=""#CDD6F4""
                                              BorderThickness=""0""/>
                                </StackPanel>
                            </Grid>

                            <TextBlock Text=""校验位"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                            <ComboBox ItemsSource=""{x:Static local:SerialCommOperationViewModel.ParityOptions}""
                                      SelectedItem=""{Binding Parity}""
                                      Height=""28"" Margin=""0,0,0,0""
                                      Background=""#313244"" Foreground=""#CDD6F4""
                                      BorderThickness=""0""/>
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
                                             Height=""26"" Background=""#313244"" Foreground=""#CDD6F4""  Width=""150""
                                             BorderThickness=""0"" CaretBrush=""#CDD6F4""/>
                                </StackPanel>
                                <StackPanel Grid.Column=""2"">
                                    <TextBlock Text=""重试次数"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                                    <TextBox Text=""{Binding RetryCount, UpdateSourceTrigger=PropertyChanged}""
                                             Height=""26"" Background=""#313244"" Foreground=""#CDD6F4""     Width=""150""
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

                    <TextBlock Text=""配置串口参数后，点击节点执行按钮即可建立连接并输出 ICommunication 实例。""
                               FontSize=""11"" Foreground=""#585B70"" TextWrapping=""Wrap""/>
                </StackPanel>
            </ScrollViewer>
        </TabItem>

        <!-- ==================== Tab 2: 当前数据 ==================== -->
        <TabItem Header=""当前数据"">
            <ScrollViewer VerticalScrollBarVisibility=""Auto"">
                <StackPanel Margin=""0,8,0,0"">
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