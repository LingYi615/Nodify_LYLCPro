using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PluginBase.Attributes;
using PluginBase.Base;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace ParameterEnactmentPlugin
{
    /// <summary>
    /// 参数设定插件
    /// 输出动态生成的变量列表，每个变量对应一个输出连接器
    /// </summary>
    [Plugin("参数设定", Description = "直接传输至其它插件",
            Version = "1.0.0",
            Author = "LYCorePro",
            Icon = "M768 128H256C185.6 128 128 185.6 128 256v512c0 70.4 57.6 128 128 128h128c19.2 0 32-12.8 32-32s-12.8-32-32-32H256c-35.2 0-64-28.8-64-64V256c0-35.2 28.8-64 64-64h512c35.2 0 64 28.8 64 64v160c0 19.2 12.8 32 32 32s32-12.8 32-32V256c0-70.4-57.6-128-128-128z M864 544h-160c-12.8-38.4-48-64-89.6-64-41.6 0-76.8 25.6-89.6 64h-38.4c-19.2 0-32 12.8-32 32s12.8 32 32 32h38.4c12.8 38.4 48 64 89.6 64 41.6 0 76.8-28.8 89.6-64h160c19.2 0 32-12.8 32-32s-12.8-32-32-32z m-246.4 64c-19.2 0-32-12.8-32-32s12.8-32 32-32 32 12.8 32 32-16 32-32 32zM864 768h-38.4c-12.8-38.4-48-64-89.6-64-41.6 0-76.8 28.8-89.6 64h-160c-19.2 0-32 12.8-32 32s12.8 32 32 32h160c12.8 38.4 48 64 89.6 64 41.6 0 76.8-25.6 89.6-64H864c19.2 0 32-12.8 32-32s-12.8-32-32-32z m-128 64c-19.2 0-32-12.8-32-32s12.8-32 32-32 32 12.8 32 32-12.8 32-32 32zM736 320c0-19.2-12.8-32-32-32H320c-19.2 0-32 12.8-32 32s12.8 32 32 32h384c19.2 0 32-12.8 32-32zM384 480h-64c-19.2 0-32 12.8-32 32s12.8 32 32 32h64c19.2 0 32-12.8 32-32s-12.8-32-32-32zM384 672h-64c-19.2 0-32 12.8-32 32s12.8 32 32 32h64c19.2 0 32-12.8 32-32s-12.8-32-32-32z")]
    [PluginCategory("数据处理")]
    [PluginIO(InputCount = 0, OutputCount = 0)] // OutputCount=0 表示输出数量动态可变
    [PluginTag("参数设定")]
    public class ParameterEnactmentPlugin : PluginBase.Base.PluginBase
    {
        /// <summary>Null 转 Visibility 转换器</summary>
        public static readonly IValueConverter NullToVisibilityConverter = new NullToVisibilityConverterImpl();

        public override string Name => "参数设定";
        public override Version Version => new(1, 0, 0);
        public override string Description => "可创建多个参数,直接传输至其它插件";
        public override string Author => "LYCorePro";

        public override Type[] OperationTypes => new[] { typeof(ParameterEnactmentOperationViewModel) };

        public override void Initialize(IPluginHost host)
        {
            base.Initialize(host);

            // 注册节点内容模板（显示在节点内部的小型信息区）
            var nodeTemplate = CreateNodeContentTemplate();
            if (!Application.Current.Resources.Contains(typeof(ParameterEnactmentOperationViewModel)))
            {
                Application.Current.Resources.Add(typeof(ParameterEnactmentOperationViewModel), nodeTemplate);
            }
        }

        public override DataTemplate? GetTemplate(Type operationType)
        {
            if (operationType == typeof(ParameterEnactmentOperationViewModel))
            {
                return CreateSettingsPanelTemplate();
            }
            return base.GetTemplate(operationType);
        }

        /// <summary>
        /// 创建节点内容显示模板
        /// StatusMessage 使用 TextTrimming 防止文字超出节点尺寸
        /// </summary>
        private DataTemplate CreateNodeContentTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:ParameterEnactmentPlugin;assembly=Plugin.ParameterEnactment""
              DataType=""{x:Type local:ParameterEnactmentOperationViewModel}"">
    <Border Background=""#181825"" CornerRadius=""4"" Padding=""8,6"" Margin=""4,2"">
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
        /// 创建右侧参数面板的 DataTemplate
        /// TabControl 结构：Tab1-参数设定（变量管理） / Tab2-当前数据
        /// </summary>
        private DataTemplate CreateSettingsPanelTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:ParameterEnactmentPlugin;assembly=Plugin.ParameterEnactment""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:ParameterEnactmentOperationViewModel}"">
    <TabControl Background=""#1E1E2E"" BorderThickness=""0"">
        <TabControl.Resources>
            <BooleanToVisibilityConverter x:Key=""BoolToVisibilityConverter""/>
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
                    <!-- 变量列表 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <Grid Margin=""0,0,0,8"">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""*""/>
                                    <ColumnDefinition Width=""Auto""/>
                                </Grid.ColumnDefinitions>
                                <TextBlock Grid.Column=""0"" Text=""读取变量"" FontSize=""12"" FontWeight=""Bold""
                                           Foreground=""#89B4FA"" VerticalAlignment=""Center""/>
                                <StackPanel Grid.Column=""1"" Orientation=""Horizontal"">
                                    <Button Content=""导入""
                                            Command=""{Binding ImportVariablesCommand}""
                                            Height=""24"" Width=""44""
                                            FontSize=""11""
                                            Background=""#A6E3A1"" Foreground=""#1E1E2E""
                                            BorderThickness=""0"" Cursor=""Hand""
                                            Margin=""0,0,4,0""
                                            ToolTip=""从 CSV/Excel 文件导入变量"">
                                        <Button.Resources>
                                            <Style TargetType=""Border"">
                                                <Setter Property=""CornerRadius"" Value=""4""/>
                                            </Style>
                                        </Button.Resources>
                                    </Button>
                                    <Button Content=""导出""
                                            Command=""{Binding ExportVariablesCommand}""
                                            Height=""24"" Width=""44""
                                            FontSize=""11""
                                            Background=""#F9E2AF"" Foreground=""#1E1E2E""
                                            BorderThickness=""0"" Cursor=""Hand""
                                            Margin=""0,0,4,0""
                                            ToolTip=""导出变量到 CSV 文件"">
                                        <Button.Resources>
                                            <Style TargetType=""Border"">
                                                <Setter Property=""CornerRadius"" Value=""4""/>
                                            </Style>
                                        </Button.Resources>
                                    </Button>
                                   <Button Command=""{Binding AddVariableCommand}""
                                        Height=""24"" Width=""56""
                                        FontSize=""11""
                                        Background=""#89B4FA"" Foreground=""#1E1E2E""
                                        BorderThickness=""0"" Cursor=""Hand"">
                                    <Button.Resources>
                                        <Style TargetType=""Border"">
                                            <Setter Property=""CornerRadius"" Value=""4""/>
                                        </Style>
                                    </Button.Resources>
                                    <StackPanel Orientation=""Horizontal"" VerticalAlignment=""Center"">
                                        <Path Width=""14"" Height=""14""
                                              Margin=""0,0,4,0""
                                              Fill=""{Binding Foreground, RelativeSource={RelativeSource AncestorType=Button}}""
                                              Stretch=""Uniform""
                                              Data=""M512 958.016611C392.351566 958.016611 279.8712 911.64865 195.263217 827.456955C110.62255 743.201613 44.000000 631.201183 44.000000 512.00172C44.000000 392.833221 90.624271 280.801828 175.232254 196.577449C259.840237 112.385754 372.311652 66.027789 491.960087 66.027789C611.608522 66.027789 724.088882 112.385754 808.696865 196.577449C893.368495 280.801828 940.000000 392.833221 940.000000 512.00172C940.032684 631.201183 893.408413 743.201613 808.696865 827.456955C724.088882 911.64865 611.608522 958.016611 512 958.016611ZM512 129.983389C409.376374 129.983389 312.928262 169.726864 240.416718 241.920172C167.936137 314.048112 128.000000 410.000500 128.000000 512.000000C128.000000 614.016396 167.903454 709.951888 240.384034 782.047144C312.895578 854.238927 409.407338 894.000172 512 894.000172C614.592662 894.000172 711.071738 854.238927 783.583282 782.047144C856.063863 709.88652 896.000000 614.016396 896.000000 512.000000C896.000000 409.983604 856.096546 314.080796 783.583282 241.920172C711.071738 169.759548 614.592662 129.983389 512 129.983389Z M736.00086 480.00086 L544.00086 480.00086 L544.00086 288.00086C544.00086 270.336138 529.664722 256.00086 512.00086 256.00086C494.336138 256.00086 480.00086 270.336138 480.00086 288.00086L480.00086 480.00086 L288.00086 480.00086C270.336138 480.00086 256.00086 494.336138 256.00086 512.00086C256.00086 529.664722 270.336138 544.00086 288.00086 544.00086L480.00086 544.00086 L480.00086 736.00086C480.00086 753.695686 494.336138 768.00086 512.00086 768.00086C529.664722 768.00086 544.00086 753.695686 544.00086 736.00086L544.00086 544.00086 L736.00086 544.00086C753.695686 544.00086 768.00086 529.664722 768.00086 512.00086C768.00086 494.336138 753.695686 480.00086 736.00086 480.00086Z""/>
                                        <TextBlock Text=""添加"" FontSize=""11"" VerticalAlignment=""Center""/>
                                    </StackPanel>
                                </Button>

                                </StackPanel>
                            </Grid>

                            <!-- 变量列表表格 -->
                            <ListView ItemsSource=""{Binding EnactmentVariables}""
                                      Background=""Transparent""
                                      BorderThickness=""0""
                                     
                                      ScrollViewer.HorizontalScrollBarVisibility=""Disabled"">
                                <ListView.Resources>
                                    <Style TargetType=""GridViewColumnHeader"">
                                        <Setter Property=""Background"" Value=""#313244""/>
                                        <Setter Property=""Foreground"" Value=""#89B4FA""/>
                                        <Setter Property=""FontSize"" Value=""11""/>
                                        <Setter Property=""FontWeight"" Value=""Bold""/>
                                        <Setter Property=""Padding"" Value=""6,4""/>
                                        <Setter Property=""BorderBrush"" Value=""#45475A""/>
                                        <Setter Property=""BorderThickness"" Value=""0,0,0,1""/>
                                        <Setter Property=""Template"">
                                            <Setter.Value>
                                                <ControlTemplate TargetType=""GridViewColumnHeader"">
                                                    <Border Background=""{TemplateBinding Background}""
                                                            BorderBrush=""{TemplateBinding BorderBrush}""
                                                            BorderThickness=""{TemplateBinding BorderThickness}""
                                                            Padding=""{TemplateBinding Padding}"">
                                                        <ContentPresenter HorizontalAlignment=""Left""
                                                                          VerticalAlignment=""Center""/>
                                                    </Border>
                                                </ControlTemplate>
                                            </Setter.Value>
                                        </Setter>
                                    </Style>
                                    <Style TargetType=""ListViewItem"">
                                        <Setter Property=""Background"" Value=""Transparent""/>
                                        <Setter Property=""BorderThickness"" Value=""0""/>
                                        <Setter Property=""Padding"" Value=""0""/>
                                        <Setter Property=""Margin"" Value=""0,0,0,2""/>
                                        <Setter Property=""Template"">
                                            <Setter.Value>
                                                <ControlTemplate TargetType=""ListViewItem"">
                                                    <Border Background=""{TemplateBinding Background}""
                                                            BorderBrush=""Transparent""
                                                            BorderThickness=""0""
                                                            CornerRadius=""3""
                                                            Padding=""0"">
                                                        <GridViewRowPresenter Content=""{TemplateBinding Content}""
                                                                              VerticalAlignment=""Center""/>
                                                    </Border>
                                                </ControlTemplate>
                                            </Setter.Value>
                                        </Setter>
                                    </Style>
                                </ListView.Resources>
                                <ListView.View>
                                    <GridView>
                                        <GridViewColumn Header=""名称"" Width=""80"">
                                            <GridViewColumn.CellTemplate>
                                                <DataTemplate>
                                                    <TextBox Text=""{Binding Name, UpdateSourceTrigger=PropertyChanged}""
                                                             Height=""22"" FontSize=""11"" MinWidth=""70""
                                                             Background=""#45475A"" Foreground=""#CDD6F4""
                                                             BorderThickness=""0"" CaretBrush=""#CDD6F4""
                                                             VerticalAlignment=""Center"" Margin=""2,1""/>
                                                </DataTemplate>
                                            </GridViewColumn.CellTemplate>
                                        </GridViewColumn>
                                        <GridViewColumn Header=""类型"" Width=""80"">
                                            <GridViewColumn.CellTemplate>
                                                <DataTemplate>
                                                    <ComboBox SelectedItem=""{Binding DataType}""
                                                              ItemsSource=""{x:Static local:ParameterEnactmentPlugin.DataTypeOptions}""
                                                              Height=""22"" FontSize=""11"" MinWidth=""70""
                                                              Background=""#45475A"" Foreground=""#CDD6F4""
                                                              BorderThickness=""0""
                                                              VerticalAlignment=""Center"" Margin=""0,1""/>
                                                </DataTemplate>
                                            </GridViewColumn.CellTemplate>
                                        </GridViewColumn>
                                        <GridViewColumn Header=""设定值"" Width=""180"">
                                            <GridViewColumn.CellTemplate>
                                                <DataTemplate>
                                                    <TextBox Text=""{Binding ParameterValue, UpdateSourceTrigger=PropertyChanged}""
                                                             Height=""22"" FontSize=""11"" MinWidth=""130""
                                                             Background=""#45475A"" Foreground=""#CDD6F4""
                                                             BorderThickness=""0"" CaretBrush=""#CDD6F4""
                                                             ToolTip=""设定值""
                                                             VerticalAlignment=""Center"" Margin=""2,1""/>
                                                </DataTemplate>
                                            </GridViewColumn.CellTemplate>
                                        </GridViewColumn>
                                        <GridViewColumn Header=""操作"" Width=""36"">
                                            <GridViewColumn.CellTemplate>
                                                <DataTemplate>
                                                    <Button
                                                            Command=""{Binding DataContext.RemoveVariableCommand, RelativeSource={RelativeSource AncestorType=ListView}}""
                                                            CommandParameter=""{Binding}""
                                                            Height=""22"" Width=""22""
                                                            FontSize=""10"" FontWeight=""Bold""
                                                            Background=""Transparent"" Foreground=""#1E1E2E""
                                                            BorderThickness=""0"" Cursor=""Hand""    ToolTip=""删除""
                                                            VerticalAlignment=""Center""
                                                            HorizontalAlignment=""Center"">
                                                        <Button.Resources>
                                                            <Style TargetType=""Border"">
                                                                <Setter Property=""CornerRadius"" Value=""3""/>
                                                            </Style>
                                                        </Button.Resources>
                                                        <Button.Content>
                                                            <Path 
                                                                Data=""M559.786667 505.173333 L754.346667 310.613333 C767.999999 296.960000 767.999999 276.480000 754.346667 262.826667 C740.693334 249.173334 720.213334 249.173334 706.560000 262.826667 L512 457.386667 L317.44 262.826667 C303.786667 249.173334 283.306667 249.173334 269.653333 262.826667 C256.000000 276.480000 256.000000 296.960000 269.653333 310.613333 L464.213333 505.173333 L269.653333 699.733333 C256.000000 713.386667 256.000000 733.866667 269.653333 747.520000 C283.306667 761.173333 303.786667 761.173333 317.44 747.520000 L512 552.960000 L706.56 747.520000 C720.213334 761.173333 740.693334 761.173333 754.346667 747.520000 C767.999999 733.866667 767.999999 713.386667 754.346667 699.733333 L559.786667 505.173333 Z""
                                                                Fill=""#FF6B00""
                                                                Stretch=""Uniform""/>
                                                        </Button.Content>
                                                    </Button>
                                                </DataTemplate>
                                            </GridViewColumn.CellTemplate>
                                        </GridViewColumn>
                                    </GridView>
                                </ListView.View>
                            </ListView>

                            <TextBlock Text=""未配置读取变量，点击 + 添加按钮注册新变量。""
                                       FontSize=""11"" Foreground=""#585B70""
                                       Margin=""0,4,0,0"">
                                <TextBlock.Style>
                                    <Style TargetType=""TextBlock"">
                                        <Style.Triggers>
                                            <DataTrigger Binding=""{Binding EnactmentVariables.Count}"" Value=""0"">
                                                <Setter Property=""Visibility"" Value=""Visible""/>
                                            </DataTrigger>
                                            <DataTrigger Binding=""{Binding EnactmentVariables.Count}"" Value=""1"">
                                                <Setter Property=""Visibility"" Value=""Collapsed""/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </TextBlock.Style>
                            </TextBlock>
                        </StackPanel>
                    </Border>

                </StackPanel>
            </ScrollViewer>
        </TabItem>

        <!-- ==================== Tab 2: 当前数据 ==================== -->
        <TabItem Header=""当前数据"">
            <ScrollViewer VerticalScrollBarVisibility=""Auto"">
                <StackPanel Margin=""0,8,0,0"">
                    <!-- 输出信息 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12""
                            Visibility=""{Binding OutputInfo, Converter={x:Static local:ParameterEnactmentPlugin.NullToVisibilityConverter}, FallbackValue=Collapsed}"">
                        <StackPanel>
                            <TextBlock Text=""输出信息"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                            <ItemsControl ItemsSource=""{Binding EnactmentVariables}""
                                          AlternationCount=""1000"">
                                <ItemsControl.ItemTemplate>
                                    <DataTemplate>
                                        <Grid Margin=""0,0,0,4"">
                                            <Grid.ColumnDefinitions>
                                                <ColumnDefinition Width=""Auto""/>
                                                <ColumnDefinition Width=""*""/>
                                                <ColumnDefinition Width=""60""/>
                                                <ColumnDefinition Width=""*""/>
                                            </Grid.ColumnDefinitions>
                                            <TextBlock Grid.Column=""0"" FontSize=""11""
                                                       Foreground=""#585B70""
                                                       VerticalAlignment=""Center""
                                                       Margin=""0,0,6,0"">
                                                <TextBlock.Text>
                                                    <MultiBinding StringFormat=""[{0}]"">
                                                        <Binding RelativeSource=""{RelativeSource AncestorType=ContentPresenter}""
                                                                 Path=""(ItemsControl.AlternationIndex)""/>
                                                    </MultiBinding>
                                                </TextBlock.Text>
                                            </TextBlock>
                                            <TextBlock Grid.Column=""1""
                                                       Text=""{Binding Name}""
                                                       FontSize=""11"" Foreground=""#CDD6F4""
                                                       VerticalAlignment=""Center""
                                                       Margin=""0,0,6,0""/>
                                            <TextBlock Grid.Column=""2""
                                                       Text=""{Binding DataType}""
                                                       FontSize=""11"" Foreground=""#89B4FA""
                                                       VerticalAlignment=""Center""
                                                       Margin=""0,0,6,0""/>
                                            <TextBox Grid.Column=""3""
                                                     Text=""{Binding Value, Mode=OneWay}""
                                                     IsReadOnly=""True"" MinWidth=""140""
                                                     Height=""22"" FontSize=""11""
                                                     Background=""#45475A"" Foreground=""#A6E3A1""
                                                     BorderThickness=""0"" CaretBrush=""#CDD6F4""
                                                     VerticalAlignment=""Center""/>
                                        </Grid>
                                    </DataTemplate>
                                </ItemsControl.ItemTemplate>
                            </ItemsControl>
                        </StackPanel>
                    </Border>

                    <TextBlock Text=""执行后逐行显示每个变量的 [下标] 名称 类型 值。""
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

        /// <summary>数据类型选项列表（供 ComboBox 绑定）</summary>
        public static DataType[] DataTypeOptions { get; } = Enum
            .GetValues(typeof(DataType))
            .Cast<DataType>()
            .Where(dt => dt != DataType.Any && dt != DataType.TCPAgreement && dt!= DataType.SerialAgreement)
            .ToArray();
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