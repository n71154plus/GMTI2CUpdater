using System.Security.Principal;

namespace GMTI2CUpdater.Helper
{
    /// <summary>
    /// 權限檢查相關的協助方法。
    /// </summary>
    public static class PrivilegeHelper
    {
        /// <summary>
        /// 判斷目前行程是否以系統管理員身分執行。
        /// </summary>
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
