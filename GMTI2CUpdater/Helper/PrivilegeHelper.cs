using System.Security.Principal;

namespace GMTI2CUpdater.Helper
{
    public static class PrivilegeHelper
    {
        public static bool IsRunAsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
