using System.Windows.Forms;

namespace OpenSAAB.Collector.Tray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
