using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using ZMBA;


namespace WindowsRecentFilesFilterer {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

   internal class TrayIcon : IDisposable {
      private AppContext _appctx;
      private NotifyIcon _notifyIcon;
      private IContainer _components;

      private ToolStripMenuItem _miLastRunTime;
      private ToolStripMenuItem _miRunFilters;
      private ToolStripMenuItem _miConfigFile;
      private ToolStripMenuItem _miRunAtStartup;
      private ToolStripMenuItem _miExit;

      internal event Action LoadedConfig;

      public TrayIcon(AppContext context) {
         _appctx = context;
         _components = new System.ComponentModel.Container();
         _notifyIcon = new NotifyIcon(this._components) {
            ContextMenuStrip = new ContextMenuStrip(),
            Icon = Properties.Resources.AppIcon,
            Text = Program.ProcessName,
            Visible = true
         };

         _miLastRunTime = new ToolStripMenuItem("Last Run: ") { Enabled = false };
         _appctx.LocationWatcherMan.FilterRunComplete += (sender, args) => {
            _miLastRunTime.Text = "Last Run: " + DateTime.Now.ToLongTimeString() + ". Runtime: " + args.RuntimeMilliseconds + "ms";
         };

         _miRunFilters = new ToolStripMenuItem("Run Filters");
         _miRunFilters.Click += (sender, e) => { _appctx.LocationWatcherMan.RunFiltersAsync(); };

         _miConfigFile = new ToolStripMenuItem(Configuration.DefaultConfigFileName);

         _miRunAtStartup = new ToolStripMenuItem("Run at Startup");
         _miRunAtStartup.Click += (sender, e) => { ConfigureStartupLocation(!_miRunAtStartup.Checked); };

         _miExit = new ToolStripMenuItem("Exit");
         _miExit.Click += (sender, e) => { _notifyIcon.Visible = false; Application.Exit(); };
;
         _notifyIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[] { _miRunFilters, _miLastRunTime, new ToolStripSeparator(), _miConfigFile, _miRunAtStartup, new ToolStripSeparator(), _miExit });

         _notifyIcon.ContextMenuStrip.Opening += (object sender, System.ComponentModel.CancelEventArgs e) => {
            e.Cancel = false;
         };

         ConfigureStartupLocation();
         RebuildMenuItems();

         context.ConfigChangeAlert += RebuildMenuItems;


      }


      public void RebuildMenuItems() {
         _miConfigFile.Text = (_appctx.Cfg.GoodConfigExists ? "Reload " : "Create ") + Configuration.DefaultConfigFileName;
         _miConfigFile.Click -= HandleCreateConfigFileEvent;
         _miConfigFile.Click -= HandleLoadConfigFileEvent;
         if(_appctx.Cfg.GoodConfigExists) {
            _miConfigFile.Click += HandleLoadConfigFileEvent;
         } else {
            _miConfigFile.Click += HandleCreateConfigFileEvent;
         }
      }

      private void HandleCreateConfigFileEvent(Object sender, EventArgs ev) => SaveOrLoadConfigFile(false);
      private void HandleLoadConfigFileEvent(Object sender, EventArgs ev) => SaveOrLoadConfigFile(true);

      private async void SaveOrLoadConfigFile(bool bLoad) {
         ToolStripMenuItem micf = _miConfigFile;
         micf.Click -= HandleCreateConfigFileEvent; micf.Click -= HandleLoadConfigFileEvent;
         try {
            if(bLoad) {
               _appctx.Cfg = await Configuration.GetConfiguration();
               this.LoadedConfig();
            } else {
               await _appctx.Cfg.SaveCurrentConfig();
            }
            micf.Text = "Reload " + Configuration.DefaultConfigFileName;
            micf.Click -= HandleCreateConfigFileEvent; micf.Click -= HandleLoadConfigFileEvent;
            micf.Click += HandleLoadConfigFileEvent;

         } catch(Exception ex) {
            micf.Text = "Create New " + Configuration.DefaultConfigFileName;
            micf.Click -= HandleCreateConfigFileEvent; micf.Click -= HandleLoadConfigFileEvent;
            micf.Click += HandleCreateConfigFileEvent;
            string msg = "Failed to " + (bLoad ? "load " : "save ") + Configuration.DefaultConfigFileName + "\n\n" + ex.ToString();
            MessageBox.Show(msg, Program.ProcessName, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ConfigureStartupLocation(bool? bEnable = null) {
         string startupDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
         string shortcutPath = startupDirectory + "\\" + Application.ProductName + ".lnk";
         IWshRuntimeLibrary.IWshShortcut shortcut = ((IWshRuntimeLibrary.IWshShortcut)_appctx.WindowsScriptShell.CreateShortcut(shortcutPath));

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
            foreach(Delegate ev in LoadedConfig.GetInvocationList()) { LoadedConfig -= (Action)ev; }
         }
         // Call the appropriate methods to clean up unmanaged resources here. If disposing is false, only the following code is executed. 
         Interop.DestroyIcon(iconhandle);
         _disposed = true;
      }
      protected bool _disposed;

      #endregion

   }
}
