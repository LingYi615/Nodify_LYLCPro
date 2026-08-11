using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Nodify;
using PluginBase.Attributes;
using PluginBase.Base;
using PluginBase.Interfaces;

namespace RegexPlugin
{
    /// <summary>
    /// 正则表达式插件
    /// 支持全部 Regex 语法、修饰符、元字符
    /// 输入/输出数据类型可自定义
    /// 操作模式：IsMatch / Match / Matches / Replace / Split
    /// </summary>
    [Plugin("正则表达式",Description = "提供正则表达式操作",
            Version = "1.0.0",
            Author = "LYCorePro",
            Icon = "M939.52 465.92C919.552 465.92 903.68 481.792 903.68 501.76V762.88C903.68 794.112 878.592 819.2 847.36 819.2H181.76C150.528 819.2 125.44 794.112 125.44 762.88V502.784C125.44 482.816 109.568 466.944 89.6 466.944S53.76 482.816 53.76 502.784V762.88C53.76 833.536 111.104 890.88 181.76 890.88H847.36C918.016 890.88 975.36 833.536 975.36 762.88V501.76C975.36 481.792 959.488 465.92 939.52 465.92ZM189.952 653.312C189.952 673.28 200.704 683.52 222.72 683.52C242.688 683.52 253.984 673.28 255.472 653.312V443.392L465.392 671.232C473.584 679.424 483.312 683.008 495.6 683.008C513.52 683.008 523.768 675.84 525.824 661.92C525.824 651.68 521.728 641.92 513.952 631.68L339.968 440.32H370.176C465.92 436.736 515.072 384.512 517.12 284.672C508.928 196.608 454.144 147.456 352.224 137.728H225.792C199.68 137.728 187.904 149.504 189.952 173.504V653.312ZM256 194.56H349.184C411.136 198.656 445.44 229.472 451.072 287.872C453.12 358.016 418.816 391.808 349.184 389.76H256V194.56ZM622.08 683.52H817.152C837.12 683.52 848.416 673.28 849.92 653.312C847.872 633.344 837.12 622.08 817.152 620.544H652.288V440.32H802.304C822.272 440.32 833.568 430.08 849.92 410.112C847.872 390.144 837.12 378.88 802.304 377.344H652.288V200.704H817.152C837.12 200.704 848.416 189.952 849.92 167.936C847.872 150.016 837.12 139.776 817.152 137.728H622.08C595.968 137.728 584.192 149.504 586.24 173.504V647.616C583.68 671.68 595.968 683.52 622.08 683.52Z")]
    [PluginCategory("数据处理")]
    [PluginIO(InputCount = 1, OutputCount = 1)]
    [PluginTag("Regex")]
    public class RegexPlugin : PluginBase.Base.PluginBase
    {
        public static readonly IValueConverter BoolToVisibilityConverter = new BooleanToVisibilityConverter();
        public static readonly IValueConverter NullToVisibilityConverter = new NullToVisibilityConverterImpl();
        public static readonly IValueConverter InverseBoolConverter = new InverseBoolConverterImpl();

        public override string Name => "正则表达式";
        public override Version Version => new(1, 0, 0);
        public override string Description => "正则表达式处理插件，支持 IsMatch/Match/Matches/Replace/Split，支持全部修饰符和元字符";
        public override string Author => "LYCorePro";

        public override Type[] OperationTypes => new[] { typeof(RegexOperationViewModel) };

        public override void Initialize(IPluginHost host)
        {
            base.Initialize(host);

            var nodeTemplate = CreateNodeContentTemplate();
            if (!Application.Current.Resources.Contains(typeof(RegexOperationViewModel)))
            {
                Application.Current.Resources.Add(typeof(RegexOperationViewModel), nodeTemplate);
            }
        }

        public override DataTemplate? GetTemplate(Type operationType)
        {
            if (operationType == typeof(RegexOperationViewModel))
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
              xmlns:local=""clr-namespace:RegexPlugin;assembly=Plugin.Regex""
              DataType=""{x:Type local:RegexOperationViewModel}"">
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

        private DataTemplate CreateSettingsPanelTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
              xmlns:local=""clr-namespace:RegexPlugin;assembly=Plugin.Regex""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:RegexOperationViewModel}"">
    <TabControl Background=""#1E1E2E"" BorderThickness=""0"">
        <TabControl.Resources>
            <BooleanToVisibilityConverter x:Key=""BoolToVisibility""/>
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
            <Style TargetType=""CheckBox"">
                <Setter Property=""Foreground"" Value=""#CDD6F4""/>
                <Setter Property=""FontSize"" Value=""11""/>
                <Setter Property=""Margin"" Value=""0,1,0,1""/>
            </Style>
            <Style TargetType=""ComboBox"">
                <Setter Property=""Height"" Value=""26""/>
                <Setter Property=""FontSize"" Value=""11""/>
                <Setter Property=""Background"" Value=""#45475A""/>
                <Setter Property=""Foreground"" Value=""#CDD6F4""/>
                <Setter Property=""BorderThickness"" Value=""0""/>
            </Style>
            <Style TargetType=""TextBox"">
                <Setter Property=""Background"" Value=""#313244""/>
                <Setter Property=""Foreground"" Value=""#CDD6F4""/>
                <Setter Property=""BorderThickness"" Value=""0""/>
                <Setter Property=""CaretBrush"" Value=""#CDD6F4""/>
                <Setter Property=""FontSize"" Value=""11""/>
            </Style>
        </TabControl.Resources>

        <!-- ==================== Tab 1: 参数设定 ==================== -->
        <TabItem Header=""参数设定"">
            <ScrollViewer VerticalScrollBarVisibility=""Auto"">
                <StackPanel Margin=""0,8,0,0"">
                    <!-- 正则表达式 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <TextBlock Text=""正则表达式"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,6""/>
                            <TextBox Text=""{Binding RegexPattern, UpdateSourceTrigger=PropertyChanged}""
                                     Height=""28"" FontFamily=""Consolas"" FontSize=""12""
                                     ToolTip=""输入正则表达式，如: \d{3}-\d{4}""
                                     Margin=""0,0,0,4""/>
                            <TextBlock Text=""{Binding PatternPreview}""
                                       FontSize=""10"" Foreground=""#585B70""
                                       FontFamily=""Consolas"" TextWrapping=""Wrap""/>
                        </StackPanel>
                    </Border>

                    <!-- 操作模式 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <TextBlock Text=""操作模式"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,6""/>
                            <ComboBox ItemsSource=""{x:Static local:RegexOperationViewModel.OperationModes}"" Foreground=""#45475A""
                                      SelectedItem=""{Binding OperationMode}""
                                      Margin=""0,0,0,8""/>
                            <!-- 替换字符串（仅 Replace 模式显示） -->
                            <TextBlock Text=""替换为"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""
                                       Visibility=""{Binding IsReplacementVisible, Converter={StaticResource BoolToVisibility}}""/>
                            <TextBox Text=""{Binding ReplacementPattern, UpdateSourceTrigger=PropertyChanged}""
                                     Height=""26"" FontFamily=""Consolas""
                                     ToolTip=""支持 $1 $2 反向引用，如: ($1)""
                                     Visibility=""{Binding IsReplacementVisible, Converter={StaticResource BoolToVisibility}}""/>
                        </StackPanel>
                    </Border>

                    <!-- 数据类型 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <TextBlock Text=""数据类型"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""*""/>
                                    <ColumnDefinition Width=""8""/>
                                    <ColumnDefinition Width=""*""/>
                                </Grid.ColumnDefinitions>
                                <StackPanel Grid.Column=""0"">
                                    <TextBlock Text=""输入类型"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                                    <ComboBox ItemsSource=""{x:Static local:RegexOperationViewModel.AllDataTypes}"" Foreground=""#45475A""
                                              SelectedItem=""{Binding InputDataType}""/>
                                </StackPanel>
                                <StackPanel Grid.Column=""2"">
                                    <TextBlock Text=""输出类型"" FontSize=""11"" Foreground=""#6C7086"" Margin=""0,0,0,2""/>
                                    <ComboBox ItemsSource=""{x:Static local:RegexOperationViewModel.AllDataTypes}""  Foreground=""#45475A"" 
                                              SelectedItem=""{Binding OutputDataType}""/>
                                </StackPanel>
                            </Grid>
                        </StackPanel>
                    </Border>

                    <!-- 修饰符 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <Grid Margin=""0,0,0,8"">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""*""/>
                                    <ColumnDefinition Width=""Auto""/>
                                </Grid.ColumnDefinitions>
                                <TextBlock Grid.Column=""0"" Text=""修饰符"" FontSize=""12"" FontWeight=""Bold""
                                           Foreground=""#89B4FA"" VerticalAlignment=""Center""/>
                                <TextBlock Grid.Column=""1"" FontSize=""11"" Foreground=""#585B70""
                                           VerticalAlignment=""Center"">
                                    <TextBlock.Text>
                                        <Binding Path=""ModifierFlags"" StringFormat=""活动: {0}""/>
                                    </TextBlock.Text>
                                </TextBlock>
                            </Grid>

                            <!-- 常用修饰符 -->
                            <TextBlock Text=""常用"" FontSize=""10"" Foreground=""#585B70"" Margin=""0,0,0,4""/>
                            <WrapPanel Margin=""0,0,0,8"">
                                <CheckBox IsChecked=""{Binding IgnoreCase}"" Margin=""0,0,12,0""
                                          ToolTip=""忽略大小写 — 不区分字母大小写匹配"">
                                    <TextBlock Text=""忽略大小写 (i)"" FontSize=""11"" Foreground=""#CDD6F4""/>
                                </CheckBox>
                                <CheckBox IsChecked=""{Binding Multiline}"" Margin=""0,0,12,0""
                                          ToolTip=""多行模式 — ^ 和 $ 匹配每行的首尾"">
                                    <TextBlock Text=""多行 (m)"" FontSize=""11"" Foreground=""#CDD6F4""/>
                                </CheckBox>
                                <CheckBox IsChecked=""{Binding Singleline}"" Margin=""0,0,12,0""
                                          ToolTip=""单行模式 — . 匹配换行符 \n"">
                                    <TextBlock Text=""单行 (s)"" FontSize=""11"" Foreground=""#CDD6F4""/>
                                </CheckBox>
                                <CheckBox IsChecked=""{Binding ExplicitCapture}"" Margin=""0,0,12,0""
                                          ToolTip=""仅显式捕获 — 括号默认不捕获，需用 (?&lt;name&gt;...) 命名捕获"">
                                    <TextBlock Text=""显式捕获 (n)"" FontSize=""11"" Foreground=""#CDD6F4""/>
                                </CheckBox>
                            </WrapPanel>

                            <!-- 高级修饰符 -->
                            <TextBlock Text=""高级"" FontSize=""10"" Foreground=""#585B70"" Margin=""0,0,0,4""/>
                            <WrapPanel>
                                <CheckBox IsChecked=""{Binding IgnorePatternWhitespace}"" Margin=""0,0,12,0""
                                          ToolTip=""忽略模式空白 — 忽略未转义空格和 # 注释"">
                                    <TextBlock Text=""忽略空白 (x)"" FontSize=""11"" Foreground=""#CDD6F4""/>
                                </CheckBox>
                                <CheckBox IsChecked=""{Binding RightToLeft}"" Margin=""0,0,12,0""
                                          ToolTip=""从右到左匹配"">
                                    <TextBlock Text=""右到左"" FontSize=""11"" Foreground=""#CDD6F4""/>
                                </CheckBox>
                                <CheckBox IsChecked=""{Binding ECMAScript}"" Margin=""0,0,12,0""
                                          ToolTip=""ECMAScript 兼容行为"">
                                    <TextBlock Text=""ECMAScript"" FontSize=""11"" Foreground=""#CDD6F4""/>
                                </CheckBox>
                                <CheckBox IsChecked=""{Binding CultureInvariant}"" Margin=""0,0,12,0""
                                          ToolTip=""忽略语言区域性差异"">
                                    <TextBlock Text=""区域无关"" FontSize=""11"" Foreground=""#CDD6F4""/>
                                </CheckBox>
                                <CheckBox IsChecked=""{Binding Compiled}"" Margin=""0,0,12,0""
                                          ToolTip=""编译为 IL 代码 — 执行更快但初始化稍慢"">
                                    <TextBlock Text=""编译 (C)"" FontSize=""11"" Foreground=""#CDD6F4""/>
                                </CheckBox>
                            </WrapPanel>
                        </StackPanel>
                    </Border>

                    <TextBlock Text=""输入正则表达式后，点击节点执行按钮即可进行匹配操作。""
                               FontSize=""11"" Foreground=""#585B70"" TextWrapping=""Wrap""/>
                </StackPanel>
            </ScrollViewer>
        </TabItem>

        <!-- ==================== Tab 2: 当前数据 ==================== -->
        <TabItem Header=""当前数据"">
            <ScrollViewer VerticalScrollBarVisibility=""Auto"">
                <StackPanel Margin=""0,8,0,0"">
                    <!-- 输入信息 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12""
                            Visibility=""{Binding InputInfo, Converter={x:Static local:RegexPlugin.NullToVisibilityConverter}, FallbackValue=Collapsed}"">
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
                            <TextBox Text=""{Binding OutputInfo, Mode=OneWay}""
                                     IsReadOnly=""True""
                                     Background=""#313244"" Foreground=""#A6E3A1""
                                     BorderThickness=""0"" CaretBrush=""#CDD6F4""
                                     FontFamily=""Consolas"" FontSize=""11""
                                     TextWrapping=""Wrap""
                                     MinHeight=""80""
                                     AcceptsReturn=""True""
                                     VerticalScrollBarVisibility=""Auto""/>
                        </StackPanel>
                    </Border>

                    <!-- 上次结果 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12""
                            Visibility=""{Binding LastResult, Converter={x:Static local:RegexPlugin.NullToVisibilityConverter}, FallbackValue=Collapsed}"">
                        <StackPanel>
                            <TextBlock Text=""执行结果"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                            <TextBox Text=""{Binding LastResult, Mode=OneWay}""
                                     IsReadOnly=""True""
                                     Background=""#313244"" Foreground=""#F9E2AF""
                                     BorderThickness=""0"" CaretBrush=""#CDD6F4""
                                     FontFamily=""Consolas"" FontSize=""11""
                                     TextWrapping=""Wrap""
                                     MinHeight=""40""
                                     AcceptsReturn=""True""
                                     VerticalScrollBarVisibility=""Auto""/>
                        </StackPanel>
                    </Border>

                    <TextBlock Text=""执行后显示输入数据、匹配结果和正则预览。""
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

    internal class InverseBoolConverterImpl : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : value;
        }
    }
}