using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;

using ZMBA;


namespace WindowsRecentFilesFilterer {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

  internal class TrayIcon: IDisposable {
    private NotifyIcon _notifyIcon;
    private IContainer _components;

    private ToolStripMenuItem _miLastRunTime;
    private ToolStripMenuItem _miRunFilters;
    private ToolStripMenuItem _miCreateNewConfigFile;
    private ToolStripMenuItem _miConfigFile;
    private ToolStripMenuItem _miRunAtStartup;
    private ToolStripMenuItem _miExit;

    internal event EventHandler ConfigChange;

    public TrayIcon() {
      _components = new System.ComponentModel.Container();
      _notifyIcon = new NotifyIcon(this._components) {
        ContextMenuStrip = new ContextMenuStrip(),
        Icon = Properties.Resources.AppIcon,
        Text = Program.ProcessName,
        Visible = true
      };


      _miRunFilters = new ToolStripMenuItem("Run Filters Now");
      _miRunFilters.Click += (sender, e) => { Program.Ctx.LocationWatcherMan.RunFiltersAsync(); };
      _notifyIcon.ContextMenuStrip.Items.Add(_miRunFilters);

      _miLastRunTime = new ToolStripMenuItem("Last Run: ") { Enabled = false };
      Program.Ctx.LocationWatcherMan.FilterRunComplete += (sender, args) => {
        var str = "Last Run: " + DateTime.Now.ToLongTimeString() + ". Runtime: " + args.RuntimeMilliseconds + "ms";
        _miLastRunTime.Text = str;
      };
      _notifyIcon.ContextMenuStrip.Items.Add(_miLastRunTime);


      _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());


      _miConfigFile = new ToolStripMenuItem(Configuration.DefaultConfigFileName);
      _miConfigFile.ToolTipText = "The config file allows you to specify which folders and files to filter. It's located in this .exe's directory.";
      _notifyIcon.ContextMenuStrip.Items.Add(_miConfigFile);

      _miCreateNewConfigFile = new ToolStripMenuItem("Create New " + Configuration.DefaultConfigFileName);
      _miCreateNewConfigFile.ToolTipText = "The current config could not be loaded. Click here to generate a new one using default configuration.";
      _miCreateNewConfigFile.Click += HandleCreateConfigFileEvent;
      _miCreateNewConfigFile.Visible = false;
      _notifyIcon.ContextMenuStrip.Items.Add(_miCreateNewConfigFile);


      _miRunAtStartup = new ToolStripMenuItem("Run at Startup");
      _miRunAtStartup.Click += (sender, e) => { ConfigureStartupLocation(!_miRunAtStartup.Checked); };
      _notifyIcon.ContextMenuStrip.Items.Add(_miRunAtStartup);


      _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());


      _miExit = new ToolStripMenuItem("Exit");
      _miExit.Click += (sender, e) => { _notifyIcon.Visible = false; Application.Exit(); };
      _notifyIcon.ContextMenuStrip.Items.Add(_miExit);

      Program.Ctx.ConfigChangeAlert += (sender, args) => {
        _miConfigFile.Text = (Program.Ctx.Cfg.ConfigFileExists ? "Reload " : "Create ") + Configuration.DefaultConfigFileName;
        _miConfigFile.Click -= HandleCreateConfigFileEvent;
        _miConfigFile.Click -= HandleLoadConfigFileEvent;
        if(Program.Ctx.Cfg.ConfigFileExists) {
          _miConfigFile.Click += HandleLoadConfigFileEvent;
        } else {
          _miConfigFile.Click += HandleCreateConfigFileEvent;
        }

        _miCreateNewConfigFile.Visible = Program.Ctx.Cfg.LoadConfigFailed;
      };

      ConfigureStartupLocation();

    }


    private void HandleCreateConfigFileEvent(Object sender, EventArgs ev) => SaveOrLoadConfigFile(false);
    private void HandleLoadConfigFileEvent(Object sender, EventArgs ev) => SaveOrLoadConfigFile(true);

    private async void SaveOrLoadConfigFile(bool bLoad) {
      if(!bLoad) {
        await Program.Ctx.Cfg.SaveCurrentConfig();

      }
      Program.Ctx.Cfg = await Configuration.GetConfiguration();
      this.ConfigChange(this, null);
    }

    private void ConfigureStartupLocation(bool? bEnable = null) {
      string startupDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
      string shortcutPath = startupDirectory + "\\" + Application.ProductName + ".lnk";
      IWshRuntimeLibrary.IWshShortcut shortcut = ((IWshRuntimeLibrary.IWshShortcut)Program.Ctx.WindowsScriptShell.CreateShortcut(shortcutPath));

      if(bEnable == null) {
        if(File.Exists(shortcutPath)) {
          if(shortcut.TargetPath.EqIgCase(Application.ExecutablePath)) {
            _miRunAtStartup.Checked = true;
          } else {
            _miRunAtStartup.Checked = false;
            _miRunAtStartup.Text = "Run at Startup (ERROR! Startup does not point to this .exe) ";
            _miRunAtStartup.ToolTipText = "The startup entry does not point to this .exe. Click to fix.";
          }
        } else {
          _miRunAtStartup.Text = "Run at Startup";
          _miRunAtStartup.ToolTipText = "Click to add startup entry at: " + shortcutPath;
          _miRunAtStartup.Checked = false;
        }
      } else if(bEnable == true) {
        shortcut.TargetPath = Application.ExecutablePath;
        shortcut.WorkingDirectory = Application.StartupPath;
        shortcut.Description = "Launch " + Application.ProductName;
        shortcut.IconLocation = Application.ExecutablePath;
        shortcut.Save();
        _miRunAtStartup.Text = "Run at Startup";
        _miRunAtStartup.ToolTipText = "Startup Entry location: " + shortcutPath;
        _miRunAtStartup.Checked = true;
      } else if(bEnable == false) {
        if(File.Exists(shortcutPath)) {
          File.Delete(shortcutPath);
        }
        _miRunAtStartup.Text = "Run at Startup";
        _miRunAtStartup.ToolTipText = "Click to add startup entry at: " + shortcutPath;
        _miRunAtStartup.Checked = false;
      }

    }




    #region IDisposable
    ~TrayIcon() {
      Dispose(false);
    }

    // Implement IDisposable.
    // Do not make this method virtual.
    // A derived class should not be able to override this method.
    public void Dispose() {
      Dispose(true);
      // This object will be cleaned up by the Dispose method.
      // Therefore, you should call GC.SupressFinalize to take this object off the finalization queue and prevent finalization code for this object from executing a second time.
      GC.SuppressFinalize(this);
    }

    // Dispose(bool disposing) executes in two distinct scenarios.
    // If disposing equals true, the method has been called directly or indirectly by a user's code. Managed and unmanaged resources can be disposed.
    // If disposing equals false, the method has been called by the runtime from inside the finalizer and you should not reference other objects. Only unmanaged resources can be disposed.
    protected virtual void Dispose(bool disposing) { // If disposing equals true, dispose all managed and unmanaged resources.
      if(_disposed) { return; } //Guard against repeat disposals
      IntPtr iconhandle = _notifyIcon.Icon.Handle;
      if(disposing) { // Dispose managed resources.  
        _components.Dispose();
        _notifyIcon.Dispose();
        foreach(Delegate ev in ConfigChange.GetInvocationList()) { ConfigChange -= (EventHandler)ev; }
      }
      // Call the appropriate methods to clean up unmanaged resources here. If disposing is false, only the following code is executed. 
      Interop.DestroyIcon(iconhandle);
      _disposed = true;
    }
    protected bool _disposed;

    #endregion

  }
}
