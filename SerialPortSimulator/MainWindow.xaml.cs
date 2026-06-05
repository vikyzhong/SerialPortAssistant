using System.Windows;

namespace SerialPortSimulator;

public partial class MainWindow : Window
{
    private readonly ViewModels.MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            """
            【虚拟串口联调步骤】

            1. 安装 com0com，创建一对虚拟串口，例如 COM3 ↔ COM4

            2. 本模拟器（下位机）打开 COM4 @ 115200

            3. 串口助手（主机）打开 COM3 @ 115200

            4. 勾选「自动发送 DATA / LOG」，在串口助手查看曲线与日志

            5. 在串口助手发送 AT+GMR
               本模拟器自动回复版本行 + OK

            【协议格式】
            DATA:1000,2000,3000
            LOG:1,消息文本  或  LOG:[等级字节]+消息
            AT+GMR →  版本信息 + OK
            CMD…   →  ACK…
            """,
            "虚拟串口联调说明",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
