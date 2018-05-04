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

   internal class TrayIcon : IDisposable {
      private AppContext _appctx;
      private NotifyIcon _notifyIcon;
      private IContainer _components;

      private MenuItem[] _menuItems;
      private MenuItem _menuItem_LastRunTime;
      private MenuItem _menuItem_RunFilters;
      private MenuItem _menuItem_ConfigFile;
      private MenuItem _menuItem_Exit;

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

         _menuItem_LastRunTime = new MenuItem("Last Run: ") { Enabled = false };
         _appctx.LocationWatcherMan.FilterRunComplete += () => {
            _menuItem_LastRunTime.Text = "Last Run: " + DateTime.Now.ToLongTimeString();
         };

         _menuItem_RunFilters = new MenuItem("Run Filters");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
         _menuItem_RunFilters.Click +=  (sender, e) => { _appctx.LocationWatcherMan.RunFiltersAsync(); };
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

         _menuItem_ConfigFile = new MenuItem(Configuration.DefaultConfigFileName);

         _menuItem_Exit = new MenuItem("Exit");
         _menuItem_Exit.Click += (sender, e) => { _notifyIcon.Visible = false; Application.Exit(); };

         _menuItems = new[] {   _menuItem_LastRunTime,
                                _menuItem_RunFilters,
                              new MenuItem("-"),
                              _menuItem_ConfigFile,
                              new MenuItem("-"),
                              _menuItem_Exit
         };

         RebuildMenuItems();

         context.ConfigChangeAlert += RebuildMenuItems;

      }
      ~TrayIcon() {
         Dispose(false);
      }

      public void RebuildMenuItems() {
         _notifyIcon.ContextMenu.MenuItems.Clear();

         for(int i = 0, len = _menuItems.Length; i < len; i++) {
            _notifyIcon.ContextMenu.MenuItems.Add(_menuItems[i]);
         }

         _menuItem_ConfigFile.Text = (_appctx.Cfg.GoodConfigExists ? "Reload " : "Create ") + Configuration.DefaultConfigFileName;
         _menuItem_ConfigFile.Click -= HandleCreateConfigFileEvent;
         _menuItem_ConfigFile.Click -= HandleLoadConfigFileEvent;
         if(_appctx.Cfg.GoodConfigExists) {
            _menuItem_ConfigFile.Click += HandleLoadConfigFileEvent;
         } else {
            _menuItem_ConfigFile.Click += HandleCreateConfigFileEvent;
         }
      }

      private void HandleCreateConfigFileEvent(Object sender, EventArgs ev) => SaveOrLoadConfigFile(false);
      private void HandleLoadConfigFileEvent(Object sender, EventArgs ev) => SaveOrLoadConfigFile(true);

      private async void SaveOrLoadConfigFile(bool bLoad) {
         MenuItem micf = _menuItem_ConfigFile;
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


      #region Disposable Methods
      protected bool _disposed;

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
      protected virtual void Dispose(bool disposing) {
         // Check to see if Dispose has already been called.
         if(!_disposed) {
            // If disposing equals true, dispose all managed and unmanaged resources.
            var iconhandle = _notifyIcon.Icon.Handle;
            if(disposing) {
               // Dispose managed resources.  
               _components.Dispose();
               _notifyIcon.Dispose();
            }

            // Call the appropriate methods to clean up unmanaged resources here.
            // If disposing is false, only the following code is executed.
            Interop.DestroyIcon(iconhandle);

            // Note disposing has been done.
            _disposed = true;
         }
      }

      #endregion

   }
}
