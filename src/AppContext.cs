using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

using ZMBA;

namespace WindowsRecentFilesFilterer {
   public class AppContext : System.Windows.Forms.ApplicationContext, IDisposable  {
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


      #region IDisposable


      ~AppContext() {
         Dispose(false);
      } 
    
      // Dispose(bool disposing) executes in two distinct scenarios.
      // If disposing equals true, the method has been called directly or indirectly by a user's code. Managed and unmanaged resources can be disposed.
      // If disposing equals false, the method has been called by the runtime from inside the finalizer and you should not reference other objects. Only unmanaged resources can be disposed.
      protected override void Dispose(bool disposing) { // If disposing equals true, dispose all managed and unmanaged resources.
         if(_disposed) { return; } //Guard against repeat disposals
         if(disposing) { // Dispose managed resources.  
            _timer.Stop();
            TrayIcon.Dispose();   
            foreach(Delegate ev in ConfigChangeAlert.GetInvocationList()) { ConfigChangeAlert -= (Action)ev; }
            foreach(Delegate ev in TimerTick.GetInvocationList()) { TimerTick -= (Action)ev; }
         }
         base.Dispose(disposing);
         // Call the appropriate methods to clean up unmanaged resources here. If disposing is false, only the following code is executed.      
         _disposed = true;
      }
      protected bool _disposed;

      #endregion

   }
}
