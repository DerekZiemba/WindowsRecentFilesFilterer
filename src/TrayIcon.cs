using System;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;


namespace WindowsRecentFilesFilterer {
    #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

   internal class TrayIcon : IDisposable {
      private AppContext _appctx;
      private NotifyIcon _notifyIcon;
      private IContainer _components;

      private MenuItem[] _menuItems;
      private MenuItem _miLastRunTime;
      private MenuItem _miRunFilters;
      private MenuItem _miConfigFile;
      private MenuItem _miRunAtStartup;
      private MenuItem _miExit;

      internal event Action LoadedConfig;

      public TrayIcon(AppContext context) {
         _appctx = context;
         _components = new System.ComponentModel.Container();
         _notifyIcon = new NotifyIcon(this._components) {
            ContextMenu = new ContextMenu(),
            Icon = Properties.Resources.AppIcon,
            Text = Program.ProcessName,
            Visible = true
         };

         _miLastRunTime = new MenuItem("Last Run: ") { Enabled = false };
         _appctx.LocationWatcherMan.FilterRunComplete += (sender, args) => {
            _miLastRunTime.Text = "Last Run: " + DateTime.Now.ToLongTimeString() + ". Runtime: " + args.RuntimeMilliseconds + "ms";
         };

         _miRunFilters = new MenuItem("Run Filters");
         _miRunFilters.Click +=  (sender, e) => { _appctx.LocationWatcherMan.RunFiltersAsync(); };

         _miConfigFile = new MenuItem(Configuration.DefaultConfigFileName);

         _miRunAtStartup = new MenuItem("Run at Startup");


         _miExit = new MenuItem("Exit");
         _miExit.Click += (sender, e) => { _notifyIcon.Visible = false; Application.Exit(); };

         _menuItems = new[] { _miRunFilters, _miLastRunTime, new MenuItem("-"), _miConfigFile, new MenuItem("-"), _miExit };

         RebuildMenuItems();

         context.ConfigChangeAlert += RebuildMenuItems;

      }


      public void RebuildMenuItems() {
         _notifyIcon.ContextMenu.MenuItems.Clear();

         for(int i = 0, len = _menuItems.Length; i < len; i++) {
            _notifyIcon.ContextMenu.MenuItems.Add(_menuItems[i]);
         }

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
         MenuItem micf = _miConfigFile;
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
