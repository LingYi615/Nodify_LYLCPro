using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PluginBase.Attributes;
using PluginBase.Base;
using PluginBase.Interfaces;
using PluginBase.Models;

namespace SerialWritePlugin
{
    /// <summary>
    /// 串口写入插件
    /// 输入 SerialAgreement 通讯实例 + 写入变量，将数据写入设备
    /// 输出 outflag（可选）
    /// </summary>
    [Plugin("串口写入", Description = "串口协议写入",
            Version = "1.0.0",
            Author = "LYCorePro",
            Icon = "M511.472588 1023.999607a511.240218 511.240218 0 1 1 499.442367-401.520203 39.326171 39.326171 0 0 1-46.798143 29.88789 39.326171 39.326171 0 0 1-29.88789-46.798143A432.587877 432.587877 0 1 0 511.472588 945.347265a443.599205 443.599205 0 0 0 91.236716-9.438281 39.326171 39.326171 0 0 1 46.798143 29.88789 39.326171 39.326171 0 0 1-30.281152 46.798143A522.251546 522.251546 0 0 1 511.472588 1023.999607z M39.55854 515.905482a39.326171 39.326171 0 0 1-28.314843-66.85449c117.978512-121.124606 311.856533-193.48476 520.678499-193.48476a766.467066 766.467066 0 0 1 476.239927 152.585542 39.326171 39.326171 0 0 1-49.550975 61.348827 683.882108 683.882108 0 0 0-426.688952-135.282027c-187.979096 0-361.407508 63.315135-464.048813 169.495795a39.326171 39.326171 0 0 1-28.314843 12.191113z M511.472588 1023.999607a39.326171 39.326171 0 0 1-27.52832-11.011328c-121.124606-117.978512-193.48476-311.856533-193.484759-520.678499A764.500757 764.500757 0 0 1 443.438312 16.069853a39.326171 39.326171 0 1 1 60.955565 49.550975 686.241678 686.241678 0 0 0-135.282027 426.688952c0 187.979096 63.708396 361.407508 169.889057 464.048813A39.326171 39.326171 0 0 1 511.472588 1023.999607z M693.159496 515.905482a39.326171 39.326171 0 0 1-39.326171-39.326171 637.870488 637.870488 0 0 0-136.06855-410.171959 39.326171 39.326171 0 0 1 4.71914-55.449901 39.326171 39.326171 0 0 1 55.056639 4.325879 719.275661 719.275661 0 0 1 154.945113 461.295981 39.326171 39.326171 0 0 1-39.326171 39.326171zM511.472588 697.199129c-199.383685 0-385.396472-57.809471-497.476059-154.551851a39.326171 39.326171 0 1 1 51.124022-59.775779C163.435977 567.816027 330.572203 618.546787 511.472588 618.546787a39.326171 39.326171 0 0 1 0 78.652342zM747.429611 987.81953h-3.539355a39.326171 39.326171 0 0 1-34.60703-29.494628L617.653248 595.344347a39.326171 39.326171 0 0 1 12.977637-39.326171 39.326171 39.326171 0 0 1 41.685741-5.505664l326.800478 152.192281a39.326171 39.326171 0 0 1-4.325879 72.753415l-149.83271 47.977928-60.955565 140.787691a39.326171 39.326171 0 0 1-36.573339 23.595703z m-33.427245-331.91288L758.047678 827.368754l22.022655-51.124022a39.326171 39.326171 0 0 1 23.988964-21.629394l71.966892-23.202441z")]
    [PluginCategory("通讯")]
    [PluginIO(InputCount = 1, OutputCount = 0)]
    [PluginTag("串口写入")]
    public class SerialWritePlugin : PluginBase.Base.PluginBase
    {
        public static readonly IValueConverter NullToVisibilityConverter = new NullToVisibilityConverterImpl();

        public override string Name => "串口写入";
        public override Version Version => new(1, 0, 0);
        public override string Description => "串口数据写入插件，将变量数据写入到通讯实例中";
        public override string Author => "LYCorePro";

        public override Type[] OperationTypes => new[] { typeof(SerialWriteOperationViewModel) };

        public override void Initialize(IPluginHost host)
        {
            base.Initialize(host);

            var nodeTemplate = CreateNodeContentTemplate();
            if (!Application.Current.Resources.Contains(typeof(SerialWriteOperationViewModel)))
            {
                Application.Current.Resources.Add(typeof(SerialWriteOperationViewModel), nodeTemplate);
            }
        }

        public override DataTemplate? GetTemplate(Type operationType)
        {
            if (operationType == typeof(SerialWriteOperationViewModel))
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
              xmlns:local=""clr-namespace:SerialWritePlugin;assembly=Plugin.SerialWrite""
              DataType=""{x:Type local:SerialWriteOperationViewModel}"">
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
              xmlns:local=""clr-namespace:SerialWritePlugin;assembly=Plugin.SerialWrite""
              xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
              xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
              mc:Ignorable=""d""
              d:DataContext=""{d:DesignInstance Type=local:SerialWriteOperationViewModel}"">
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
                    <!-- ====== inflag / outflag 流程控制 ====== -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12"">
                        <StackPanel>
                            <TextBlock Text=""流程控制"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>

                            <!-- inflag -->
                            <Grid Margin=""0,0,0,8"">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""Auto""/>
                                    <ColumnDefinition Width=""*""/>
                                </Grid.ColumnDefinitions>
                                <CheckBox IsChecked=""{Binding IsInFlagEnabled}""
                                          Foreground=""#CDD6F4"" FontSize=""11""
                                          VerticalAlignment=""Center""
                                          Content=""InFlag""/>
                                <ComboBox Grid.Column=""1""
                                          ItemsSource=""{x:Static local:SerialWriteOperationViewModel.InOutDataTypes}""
                                          SelectedItem=""{Binding InFlagDataType}""
                                          Height=""24"" FontSize=""11""
                                          Background=""#45475A"" Foreground=""#CDD6F4""
                                          BorderThickness=""0""
                                          Visibility=""{Binding IsInFlagEnabled, Converter={StaticResource BoolToVisibilityConverter}}""/>
                            </Grid>
                            <TextBlock Text=""输入流程控制信号，为 false 或 null 时阻止执行""
                                       FontSize=""10"" Foreground=""#585B70""
                                       Margin=""18,0,0,8""/>

                            <!-- outflag -->
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""Auto""/>
                                    <ColumnDefinition Width=""*""/>
                                </Grid.ColumnDefinitions>
                                <CheckBox IsChecked=""{Binding IsOutFlagEnabled}""
                                          Foreground=""#CDD6F4"" FontSize=""11""
                                          VerticalAlignment=""Center""
                                          Content=""OutFlag""/>
                                <ComboBox Grid.Column=""1""
                                          ItemsSource=""{x:Static local:SerialWriteOperationViewModel.InOutDataTypes}""
                                          SelectedItem=""{Binding OutFlagDataType}""
                                          Height=""24"" FontSize=""11""
                                          Background=""#45475A"" Foreground=""#CDD6F4""
                                          BorderThickness=""0""
                                          Visibility=""{Binding IsOutFlagEnabled, Converter={StaticResource BoolToVisibilityConverter}}""/>
                            </Grid>
                            <TextBlock Text=""输出执行结果，全部成功为 true/1/OK""
                                       FontSize=""10"" Foreground=""#585B70""
                                       Margin=""18,0,0,0""/>
                        </StackPanel>
                    </Border>

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
                                <TextBlock Grid.Column=""0"" Text=""写入变量"" FontSize=""12"" FontWeight=""Bold""
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
                            <ListView ItemsSource=""{Binding WriteVariables}""
                                      Background=""Transparent""
                                      BorderThickness=""0""
                                      MaxHeight=""300""
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
                                                             Height=""22"" FontSize=""11""   MinWidth=""70""
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
                                                              ItemsSource=""{x:Static local:SerialWritePlugin.DataTypeOptions}""
                                                              Height=""22"" FontSize=""11""   MinWidth=""70""
                                                              Background=""#45475A"" Foreground=""#CDD6F4""
                                                              BorderThickness=""0""
                                                              VerticalAlignment=""Center"" Margin=""0,1""/>
                                                </DataTemplate>
                                            </GridViewColumn.CellTemplate>
                                        </GridViewColumn>
                                        <GridViewColumn Header=""地址"" Width=""180"">
                                            <GridViewColumn.CellTemplate>
                                                <DataTemplate>
                                                    <TextBox Text=""{Binding Address, UpdateSourceTrigger=PropertyChanged}""
                                                             Height=""22"" FontSize=""11"" MinWidth=""130""
                                                             Background=""#45475A"" Foreground=""#CDD6F4""
                                                             BorderThickness=""0"" CaretBrush=""#CDD6F4""
                                                             ToolTip=""写入地址""
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

                            <TextBlock Text=""未配置写入变量，点击 + 添加按钮注册新变量。""
                                       FontSize=""11"" Foreground=""#585B70""
                                       Margin=""0,4,0,0"">
                                <TextBlock.Style>
                                    <Style TargetType=""TextBlock"">
                                        <Style.Triggers>
                                            <DataTrigger Binding=""{Binding WriteVariables.Count}"" Value=""0"">
                                                <Setter Property=""Visibility"" Value=""Visible""/>
                                            </DataTrigger>
                                            <DataTrigger Binding=""{Binding WriteVariables.Count}"" Value=""1"">
                                                <Setter Property=""Visibility"" Value=""Collapsed""/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </TextBlock.Style>
                            </TextBlock>
                        </StackPanel>
                    </Border>

                     <TextBlock FontSize=""11"" Foreground=""#585B70"" TextWrapping=""Wrap"">
                        <Run Text=""提示:(操作步骤)&#10;""/>
                        <Run Text=""1.配置写入变量后，将通讯实例连接到输入。&#10;"" />
                        <Run Text=""2.上游节点连接变量输入，点击执行即可写入数据。""/>
                    </TextBlock>
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
                            Visibility=""{Binding InputInfo, Converter={x:Static local:SerialWritePlugin.NullToVisibilityConverter}, FallbackValue=Collapsed}"">
                            <StackPanel>
                                <TextBlock Text=""输入信息"" FontSize=""12"" FontWeight=""Bold""
                                           Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                              <TextBlock Text=""{Binding InputInfo, Mode=OneWay}""
                                           Foreground=""#CDD6F4""
                                           FontFamily=""Consolas"" FontSize=""11""
                                           TextWrapping=""Wrap""
                                           IsHitTestVisible=""True""/>
                            <ItemsControl ItemsSource=""{Binding WriteVariables}"" AlternationCount=""1000"">
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
                                <Grid Margin=""0,0,0,4""
                                  Visibility=""{Binding IsInFlagEnabled, Converter={StaticResource BoolToVisibilityConverter}}"">
                                <Grid.ColumnDefinitions>
                                   <ColumnDefinition Width=""Auto""/>
                                    <ColumnDefinition Width=""*""/>
                                    <ColumnDefinition Width=""60""/>
                                    <ColumnDefinition Width=""*""/>
                                </Grid.ColumnDefinitions>
                                <TextBlock Grid.Column=""0"" Width=""24""/>
                                <TextBlock Grid.Column=""1""
                                           Text=""OutFlag""
                                           FontSize=""11"" Foreground=""#CDD6F4""
                                           VerticalAlignment=""Center""
                                           Margin=""0,0,6,0""/>
                                <TextBlock Grid.Column=""2""
                                           Text=""{Binding  InFlagDataType}""
                                           FontSize=""11"" Foreground=""#89B4FA""
                                           VerticalAlignment=""Center""
                                           Margin=""0,0,6,0""/>
                                <TextBox Grid.Column=""3""
                                         Text=""{Binding Value, Mode=OneWay}""
                                         IsReadOnly=""True""  MinWidth=""140""
                                         Height=""22"" FontSize=""11"" Margin=""-3,0,0,0""
                                         Background=""#45475A"" Foreground=""#A6E3A1""
                                         BorderThickness=""0"" CaretBrush=""#CDD6F4""
                                         VerticalAlignment=""Center""/>
                            </Grid>
</StackPanel>

                    </Border>

                    <!-- 输出信息 -->
                    <Border Background=""#181825"" BorderBrush=""#313244""
                            BorderThickness=""1"" CornerRadius=""4""
                            Padding=""10"" Margin=""0,0,0,12""
                            Visibility=""{Binding IsOutFlagEnabled, Converter={StaticResource BoolToVisibilityConverter}}"">
                        <StackPanel>
                            <TextBlock Text=""输出信息"" FontSize=""12"" FontWeight=""Bold""
                                       Foreground=""#89B4FA"" Margin=""0,0,0,8""/>
                            

                            <!-- outflag 输出信息 -->
                            <Grid Margin=""0,0,0,4"">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width=""Auto""/>
                                    <ColumnDefinition Width=""*""/>
                                    <ColumnDefinition Width=""60""/>
                                    <ColumnDefinition Width=""*""/>
                                </Grid.ColumnDefinitions>
                                <TextBlock Grid.Column=""0"" Width=""24""/>
                                <TextBlock Grid.Column=""1""
                                           Text=""OutFlag""
                                           FontSize=""11"" Foreground=""#CDD6F4""
                                           VerticalAlignment=""Center""
                                           Margin=""0,0,6,0""/>
                                <TextBlock Grid.Column=""2""
                                           Text=""{Binding OutFlagDataType}""
                                           FontSize=""11"" Foreground=""#89B4FA""
                                           VerticalAlignment=""Center""
                                           Margin=""0,0,6,0""/>
                                <TextBox Grid.Column=""3""
                                         Text=""{Binding OutFlagValueText, Mode=OneWay}""
                                         IsReadOnly=""True"" MinWidth=""140""
                                         Height=""22"" FontSize=""11""
                                         Background=""#45475A"" Foreground=""#A6E3A1""
                                         BorderThickness=""0"" CaretBrush=""#CDD6F4""
                                         VerticalAlignment=""Center""/>
                            </Grid>
                        </StackPanel>
                    </Border>

                    <TextBlock Text=""执行后逐行显示每个变量的 [下标] 名称 类型 写入值。""
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
        public static DataType[] DataTypeOptions { get; } = new[]
        {
            DataType.String,
            DataType.Int16,
            DataType.Double,
            DataType.Bool,
            DataType.Any
        };
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