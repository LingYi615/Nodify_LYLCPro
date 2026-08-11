using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
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

namespace ExpressionPlugin
{
    /// <summary>
    /// 表达式插件
    /// - 初始无输入/输出，支持动态添加
    /// - 输入/输出数据类型可自定义修改
    /// - 支持算术运算、三角函数、字符串处理、Linq 风格函数
    /// </summary>
    [Plugin("表达式", Description = "支持算数、三角函数等",
        Version = "1.0.0",
        Author = "LYCorePro",
        Icon = "M780.651821 172.762532V31.993062h102.377797v140.76947H1023.799088v102.377797h-140.76947v140.769471h-102.377797V275.140329H639.88235V172.762532h140.769471zM502.2482 375.214625a40.631188 40.631188 0 0 1-8.702113 27.769978 32.376978 32.376978 0 0 1-25.274519 11.581488 26.426269 26.426269 0 0 1-22.907032-10.045821 136.930303 136.930303 0 0 1-14.268905-32.632923 42.102869 42.102869 0 0 0-6.398613-13.757017 16.124503 16.124503 0 0 0-12.157363-4.862945 15.228697 15.228697 0 0 0-13.948975 8.190224 83.18196 83.18196 0 0 0-8.382182 21.11542L345.738143 529.805099h60.72283l-10.493724 34.552506h-58.035413l-61.810595 213.521692c-13.245127 44.150425-28.857741 87.405044-46.70987 129.699872-12.797225 30.969284-31.609145 58.867233-55.220024 81.710279a126.564551 126.564551 0 0 1-91.116239 34.16859 91.308198 91.308198 0 0 1-60.978776-17.916115 53.108482 53.108482 0 0 1-22.075212-40.951119 43.318605 43.318605 0 0 1 11.325544-30.52138 36.919993 36.919993 0 0 1 29.0497-13.117156c11.133585-2.175528 22.395143 2.559445 29.049699 12.22135 4.670987 9.469946 7.550363 19.835698 8.382183 30.521381 0 16.764364 8.446168 25.338505 20.603531 25.338504a34.360548 34.360548 0 0 0 29.0497-21.11542c10.23778-20.475559 18.236045-42.230841 23.802838-64.625984l100.138282-338.67855h-62.7064l9.853863-34.168589h62.7064l6.718543-24.122769c7.678335-30.20145 17.276253-59.763039 28.985714-88.492808 12.285336-26.106338 29.817533-49.141342 51.380857-67.377387A118.374328 118.374328 0 0 1 430.839686 319.930615c11.517502 0.383917 22.907032 2.879376 33.656701 7.294418 10.23778 3.839167 19.387795 10.23778 26.682213 18.619962a45.046231 45.046231 0 0 1 9.917849 30.521381l1.151751-1.279723zM767.854596 570.500273c0.063986 15.484642-2.047556 30.905297-6.398612 45.750078h-33.912645c4.28707-15.036739 6.654557-30.713339 6.910501-46.389939a25.08256 25.08256 0 0 0-2.559445-10.685683 10.877641 10.877641 0 0 0-10.493724-5.502807 14.076947 14.076947 0 0 0-9.597919 3.391265c-6.974487 6.462598-13.565058 13.437086-19.707726 20.731504l-66.161651 72.94418 28.985714 99.178491c2.751403 9.150016 6.078682 18.108073 9.853863 26.810185 3.519237 7.038474 7.550363 10.365752 12.54128 10.365752 4.926931 0 15.612614-6.718543 22.331157-20.155629 5.758751-11.38953 10.55771-23.290949 14.204919-35.704256h31.993062a218.960513 218.960513 0 0 1-29.0497 66.545568 120.869786 120.869786 0 0 1-36.600062 37.495868 71.536486 71.536486 0 0 1-36.856007 11.901419 48.437495 48.437495 0 0 1-42.678744-20.47556 196.309425 196.309425 0 0 1-26.170325-57.58751l-13.629044-47.029801-76.015514 87.021127a90.348406 90.348406 0 0 1-58.0994 37.751813c-20.603532 0-31.03327-14.588836-31.033269-43.57455 0.255944-21.051434 4.479029-41.782938 12.477294-61.042761h38.327687a218.832541 218.832541 0 0 0-11.069599 48.501481c0 7.678335 2.047556 11.581488 6.718543 11.581488 4.607001 0 10.173794-4.862945 19.451781-14.588836l89.964489-101.609963-17.404225-58.291358a426.211565 426.211565 0 0 0-8.446168-26.23431 50.677009 50.677009 0 0 0-8.126238-13.757017 16.124503 16.124503 0 0 0-11.901419-5.11889c-13.629044 0-24.954588 18.555976-34.232576 55.156038h-32.249006a305.853668 305.853668 0 0 1 25.274519-64.050109c8.382182-15.484642 19.64374-29.113686 33.080826-39.991327 11.517502-8.830085 25.274519-13.629044 39.479437-13.69303 17.148281-0.639861 33.592715 7.038474 44.7263 20.731504 12.797225 16.636392 22.267171 35.832229 27.833964 56.43576l5.822737 18.939893 54.580163-61.042762c14.076947-19.323809 35.064395-31.993062 58.035414-35.064395a31.225228 31.225228 0 0 1 32.568936 17.084295c4.670987 10.685683 7.166446 22.395143 7.230432 34.168589v-0.895805z")]
    [PluginCategory("数据处理")]
    [PluginIO(InputCount = 0, OutputCount = 0, AllowMultipleInputs = true, AllowMultipleOutputs = true)]
    [PluginTag("表达式")]
    public class ExpressionPlugin : PluginBase.Base.PluginBase
    {
        public override string Name => "表达式";
        public override Version Version => new(1, 0, 0);
        public override string Description => "表达式计算插件，支持算术运算、三角函数、字符串处理、Linq 风格函数。输入输出类型可自定义。";
        public override string Author => "LYCorePro";

        public override Type[] OperationTypes => new[] {typeof(ExpressionOperationModelView)};

        public override void Initialize(IPluginHost host)
        {
            base.Initialize(host);

            var nodeTemplate = CreateNodeContentTemplate();
            if (!System.Windows.Application.Current.Resources.Contains(typeof(ExpressionOperationModelView)))
            {
                System.Windows.Application.Current.Resources.Add(typeof(ExpressionOperationModelView), nodeTemplate);
           }
       }

        public override DataTemplate? GetTemplate(Type operationType)
        {
            if (operationType == typeof(ExpressionOperationModelView))
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
              xmlns:local=""clr-namespace:ExpressionPlugin;assembly=Plugin.Expression""
              DataType=""{x:Type local:ExpressionOperationModelView}"">
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
              xmlns:local=""clr-namespace:ExpressionPlugin;assembly=Plugin.Expression""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:ExpressionOperationModelView}"">
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

            <!-- 表达式输入 -->
            <Border Background=""#181825"" BorderBrush=""#313244""
                    BorderThickness=""1"" CornerRadius=""4""
                    Padding=""10"" Margin=""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text=""表达式"" FontSize=""12"" FontWeight=""Bold""
                               Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                    <TextBox Text=""{Binding Expression, UpdateSourceTrigger=PropertyChanged}""
                             AcceptsReturn=""True""
                             TextWrapping=""Wrap""
                             MinHeight=""80""
                             MaxHeight=""200""
                             MinWidth=""390""
                             FontFamily=""Consolas"" FontSize=""12""
                             Background=""#313244"" Foreground=""#CDD6F4""
                             BorderThickness=""0"" CaretBrush=""#CDD6F4""
                             VerticalScrollBarVisibility=""Auto""
                             ToolTip=""支持: 算术运算、三角函数(sin/cos/tan/sqrt/abs/pow/log/ln)、字符串函数(concat/substring/replace/trim等)、Linq函数(sum/avg/min/max/count/where等)、逻辑函数(if/and/or/not)""/>
                    <TextBlock Text=""{Binding ExpressionPreview}""
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
                                                 ToolTip = ""输入名称（在表达式中通过此名称引用）""/>
                                        <ComboBox Grid.Column = ""1""
                                                  ItemsSource = ""{x:Static local:ExpressionOperationModelView.AvailableDataTypes}""
                                                  SelectedItem = ""{Binding DataType, UpdateSourceTrigger = PropertyChanged}""
                                                  Width = ""100""
                                                  Height = ""22""
                                                  FontSize = ""10""
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
                                  ItemsSource = ""{x:Static local:ExpressionOperationModelView.AvailableDataTypes}""/>
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
                                                  ItemsSource = ""{x:Static local:ExpressionOperationModelView.AvailableDataTypes}""
                                                  SelectedItem = ""{Binding DataType, UpdateSourceTrigger = PropertyChanged}""
                                                  Width = ""100""
                                                  Height = ""22""
                                                  FontSize = ""10""
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
                                  ItemsSource = ""{x:Static local:ExpressionOperationModelView.AvailableDataTypes}""/>
                        <Button Grid.Column = ""2"" Content = ""新增"" Height = ""26""
                                FontSize = ""11"" Background = ""#45475A""
                                Foreground = ""#A6E3A1"" BorderThickness=""0""
                                Cursor = ""Hand""
                                Command = ""{Binding AddOutputCommand}""
                                CommandParameter = ""{Binding SelectedItem, ElementName = OutputTypeCombo}""/>
                    </Grid>
                </StackPanel>
            </Border>

            <!-- 函数参考 -->
            <Border Background = ""#181825"" BorderBrush=""#313244""
                    BorderThickness = ""1"" CornerRadius = ""4""
                    Padding = ""10"" Margin = ""0,0,0,12"">
                <StackPanel>
                    <TextBlock Text = ""函数参考"" FontSize = ""12"" FontWeight = ""Bold""
                               Foreground = ""#F9E2AF"" Margin=""0,0,0,8""/>
                    <TextBlock Text = ""数学: sin(x) cos(x) tan(x) sqrt(x) abs(x) pow(x, y) log(b, x) log10(x) ln(x) ceil(x) floor(x) round(x[, d])""
                               FontSize = ""10"" Foreground = ""#A6ADC8""
                               FontFamily = ""Consolas"" TextWrapping = ""Wrap"" Margin = ""0,0,0,4""/>
                    <TextBlock Text = ""字符串: concat(a, b, ...) substring(s, start[, len]) replace(s, old, new) trim(s) toupper(s) tolower(s) length(s) contains(s, sub) startswith(s, sub) endswith(s, sub) indexof(s, sub) split(s, sep) join(arr, sep) format(fmt, args...)""
                               FontSize = ""10"" Foreground = ""#A6ADC8""
                               FontFamily = ""Consolas"" TextWrapping = ""Wrap"" Margin = ""0,0,0,4""/>
                    <TextBlock Text = ""Linq: sum(col) avg(col) min(col) max(col) count(col) first(col) last(col) distinct(col) where(col, cond) select(col, prop) any(col[, cond]) all(col, cond) orderby(col, prop)""
                               FontSize = ""10"" Foreground = ""#A6ADC8""
                               FontFamily = ""Consolas"" TextWrapping = ""Wrap"" Margin = ""0,0,0,4""/>
                    <TextBlock Text = ""逻辑: if (cond,trueVal,falseVal) and(a, b, ...) or(a, b, ...) not(x)""
                               FontSize = ""10"" Foreground = ""#A6ADC8""
                               FontFamily = ""Consolas"" TextWrapping = ""Wrap"" Margin = ""0,0,0,4""/>
                    <TextBlock Text = ""变量: 输入名称即为变量名，可在表达式中直接引用。多行模式支持赋值: var = expr""
                               FontSize = ""10"" Foreground = ""#585B70""
                               FontFamily = ""Consolas"" TextWrapping = ""Wrap""/>
                </StackPanel>
            </Border>

            <TextBlock Text = ""提示: 输入名称作为变量名在表达式中直接使用。多行表达式每行独立计算，最后一行结果为最终输出。支持赋值语句 var = expression。""
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
   }
}