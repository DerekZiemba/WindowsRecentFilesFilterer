using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

using ZMBA;

namespace WindowsRecentFilesFilterer {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

  public class AppContext: System.Windows.Forms.ApplicationContext, IDisposable {
    private Task _taskLoadCfg;
    private Configuration _cfg;
    private DispatcherTimer _timer;
    private event EventHandler _configChangeAlert;

    internal event EventHandler ConfigChangeAlert { add => _configChangeAlert += value; remove => _configChangeAlert -= value; }
    internal event EventHandler FilterIntervalTick { add => _timer.Tick += value; remove => _timer.Tick -= value; }

    internal Dispatcher UIDispatcher { get; set; }
    internal Configuration Cfg { get => Common.BlockUntilFinished(ref _taskLoadCfg, ref _cfg); set => _cfg = value; }

    internal TrayIcon TrayIcon { get; private set; }

    internal IWshRuntimeLibrary.WshShell WindowsScriptShell { get; } = new IWshRuntimeLibrary.WshShell();

    internal LocationWatcherManager LocationWatcherMan { get; private set; }


    public AppContext() {
      UIDispatcher = Dispatcher.CurrentDispatcher;
      _timer = new DispatcherTimer();


    }

    internal void Init() {
      _taskLoadCfg = Task.Run(async () => {
        this._cfg = await Configuration.GetConfiguration();
        UIDispatcher.BeginInvoke(this._configChangeAlert, new[] { this, null });
      });

      LocationWatcherMan = new LocationWatcherManager();

      TrayIcon = new TrayIcon();
      TrayIcon.ConfigChange += (sender, args) => this._configChangeAlert(sender, args);

      _timer.Interval = TimeSpan.FromSeconds(_cfg.FilterInterval);
      this.ConfigChangeAlert += (sender, args) => { _timer.Interval = TimeSpan.FromSeconds(_cfg.FilterInterval); };

      _timer.Start();
      this._configChangeAlert(this, null);
    }

    public void ShowError(string message, Exception ex, [CallerMemberName] string caller = null, [CallerLineNumber] int line = -1, [CallerFilePath] string file = null) {
      const string NotAvailable = "N/A";
      const string SectionBreak = "-------------------";

      var statements = new List<string>(11);
      statements.Add("Message: " + (message.NotWhitespace() ? message : NotAvailable));
      statements.Add("Reason: " + ((ex?.Message).NotWhitespace() ? ex?.Message : NotAvailable));
      if((ex?.InnerException?.Message).NotWhitespace()) { statements.Add("Inner Reason: " + ex.InnerException.Message); }
      statements.Add(SectionBreak);
      statements.Add("ShowError() caller information:");
      statements.Add("\t Called From Line #: " + (line > -1 ? line.ToString() : NotAvailable));
      statements.Add("\t Called By Method: " + (caller.NotWhitespace() ? caller : NotAvailable));
      statements.Add("\t Called From File: " + (file.NotWhitespace() ? file : NotAvailable));
      if(ex != null) {
        statements.Add(SectionBreak);
        statements.Add("Exception Details: ");
        statements.Add(ex.ToString());
      }

      string errorbody = statements.ToStringJoin("\n", bFilterWhitespace: false);

      UIDispatcher.Invoke((Func<string, string, MessageBoxButtons, MessageBoxIcon, DialogResult>)MessageBox.Show, errorbody, Program.ProcessName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
