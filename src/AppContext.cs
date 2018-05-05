using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

using ZMBA;

namespace WindowsRecentFilesFilterer {
   #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

   public class AppContext : System.Windows.Forms.ApplicationContext, IDisposable  {
      private Task _taskLoadCfg;
      private Configuration _cfg;
      private DispatcherTimer _timer;
      private event EventHandler _configChangeAlert;

      internal event EventHandler ConfigChangeAlert { add=>_configChangeAlert+=value; remove=>_configChangeAlert-=value; }
      internal event EventHandler FilterIntervalTick  { add=>_timer.Tick+=value; remove=>_timer.Tick-=value; }

      internal Dispatcher UIDispatcher { get; set; }
      internal Configuration Cfg { get => Common.BlockUntilFinished(ref _taskLoadCfg, ref _cfg); set => _cfg = value; }

      internal TrayIcon TrayIcon { get; }

      internal IWshRuntimeLibrary.WshShell WindowsScriptShell { get; } = new IWshRuntimeLibrary.WshShell();

      internal LocationWatcherManager LocationWatcherMan { get; }


      public AppContext() {
         UIDispatcher = Dispatcher.CurrentDispatcher;

         _taskLoadCfg = Task.Run(async () => {
            this._cfg = await Configuration.GetConfiguration();
            UIDispatcher.BeginInvoke(this._configChangeAlert, new[] { this, null });
         });

         _timer = new DispatcherTimer();
         LocationWatcherMan = new LocationWatcherManager(this);

         TrayIcon = new TrayIcon(this);
         TrayIcon.ConfigChange += (sender, args) => this._configChangeAlert(sender, args);

         _timer.Interval = TimeSpan.FromSeconds(_cfg.FilterInterval);
         this.ConfigChangeAlert += (sender, args) => { _timer.Interval = TimeSpan.FromSeconds(_cfg.FilterInterval); };

         _timer.Start();
         this._configChangeAlert(this, null);
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
            foreach(Delegate ev in _configChangeAlert.GetInvocationList()) { _configChangeAlert -= (EventHandler)ev; }
         }
         base.Dispose(disposing);
         // Call the appropriate methods to clean up unmanaged resources here. If disposing is false, only the following code is executed.      
         _disposed = true;
      }
      protected bool _disposed;

      #endregion

   }
}
