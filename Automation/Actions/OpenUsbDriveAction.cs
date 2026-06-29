using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;

namespace HolidayCountdown.Automation.Actions;

/// <summary>
/// 自动打开已插入的 U 盘（可移动磁盘）。
/// </summary>
[ActionInfo(
    "holidaycountdown.action.openUsbDrive",
    "打开U盘",
    "\ue8a7",
    defaultGroupToMenu: "HolidayCountdown"
)]
public class OpenUsbDriveAction : ActionBase
{
    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        try
        {
            foreach (var drive in DriveInfo.GetDrives()
                         .Where(d => d.DriveType == DriveType.Removable && d.IsReady))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = drive.RootDirectory.FullName,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // 忽略单个 U 盘打开失败
                }
            }
        }
        catch
        {
            // 忽略枚举失败
        }
    }
}
