using System.Security.Principal;
using System.Windows;

namespace diskform4th.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!IsAdministrator())
            {
                MessageBox.Show("diskf0rm4th requires Administrator privileges to format and write to physical drives.\n\nPlease restart the application as Administrator.",
                                "Administrator Privileges Required",
                                MessageBoxButton.OK,
                                MessageBoxImage.Stop);
                Current.Shutdown();
            }
        }

        private static bool IsAdministrator()
        {
            if (System.OperatingSystem.IsWindows())
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return true; // Simple bypass for cross-platform dev/builds in mock env
        }
    }
}