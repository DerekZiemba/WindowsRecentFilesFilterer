using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WindowsRecentFilesFilterer {

  internal static class Interop {

    [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")] internal static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr hWnd, Int32 nCmdShow);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] internal static extern bool DestroyIcon(IntPtr handle);


    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int GetFinalPathNameByHandle(IntPtr handle, [In, Out] StringBuilder path, int bufLen, int flags);


  }
}
