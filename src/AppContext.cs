using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

using ZMBA;

namespace WindowsRecentFilesFilterer {
   public class AppContext : System.Windows.Forms.ApplicationContext {
      private Task _taskLoadCfg;
      private Configuration _cfg;
      private DispatcherTimer _timer;

      internal Configuration Cfg { get => Common.BlockUntilFinished(ref _taskLoadCfg, ref _cfg); set => _cfg = value; }

      internal TrayIcon TrayIcon { get; }

      internal IWshRuntimeLibrary.WshShell WindowsScriptShell { get; } = new IWshRuntimeLibrary.WshShell();

      internal LocationWatcherManager LocationWatcherMan { get; }


      internal event Action ConfigChangeAlert;
      internal event Action TimerTick;


      public AppContext() {
         _taskLoadCfg = Task.Run(async () => this._cfg = await Configuration.TryGetConfiguration());
         LocationWatcherMan = new LocationWatcherManager(this);

         TrayIcon = new TrayIcon(this);
         TrayIcon.LoadedConfig += () => this.ConfigChangeAlert();


         _timer = new DispatcherTimer();
         _timer.Tick += (object sender, EventArgs e) => this.TimerTick();
         _timer.Interval = TimeSpan.FromSeconds(_cfg.FilterInterval);
         _timer.Start();

         this.ConfigChangeAlert += () => { _timer.Interval = TimeSpan.FromSeconds(_cfg.FilterInterval); };

         LocationWatcherMan.Initialize();
      }



      // Called from the Dispose method of the base class
      protected override void Dispose(bool disposing) {
         _timer.Stop();
         TrayIcon.Dispose();
         base.Dispose(disposing);
      }
   }
}
