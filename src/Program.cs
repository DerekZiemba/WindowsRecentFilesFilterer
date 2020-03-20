using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using ZMBA;

namespace WindowsRecentFilesFilterer {

   static class Program {

      public static AppContext Ctx { get; private set; }
      public static string ProcessName { get; private set; }

      [STAThread]
      static void Main() {
         Process currentProcess = Process.GetCurrentProcess();
         ProcessName = currentProcess.ProcessName;

         foreach(var process in Process.GetProcesses()) {
            if(process.Id != currentProcess.Id && process.ProcessName.Eq(ProcessName)) {
               if(process.MainWindowHandle != null) {
                  Interop.ShowWindow(process.MainWindowHandle, 3);
                  Interop.SetForegroundWindow(process.MainWindowHandle);
               }
               return;
               
            }
         }

         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         try {
            Ctx = new AppContext();
				Ctx.Init();
            Application.Run(Ctx);
         } catch(Exception ex) {
            MessageBox.Show(ex.ToString(), ex.Message);
         }
      }

   }
}
