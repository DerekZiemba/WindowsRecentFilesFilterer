using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using ZMBA;

namespace WindowsRecentFilesFilterer {

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
   internal class LocationWatcherManager : IDisposable {
      private AppContext _ctx;
      private List<LocationWatcher> _watchers = new List<LocationWatcher>();

      internal event Action FilterRunComplete;

      public LocationWatcherManager(AppContext ctx) {
         _ctx = ctx;
         ctx.ConfigChangeAlert += Initialize;
         ctx.TimerTick += ()=> RunFiltersAsync();
      }


      public void Initialize() {
         for(var i = 0; i < _watchers.Count; i++) {
            _watchers[i].Dispose();
         }
         _watchers.Clear();
         for(var i = 0; i < _ctx.Cfg.LocationNodes.Length; i++) {
            _watchers.Add(new LocationWatcher(_ctx, _ctx.Cfg.LocationNodes[i]));
         }
         RunFiltersAsync();

      }


      public async Task RunFiltersAsync() {
         await Task.Run((Action)RunFilters);
      }
      public void RunFilters() {
         for(var i = 0; i < _watchers.Count; i++) {
            _watchers[i].RunFilters();
         }
         FilterRunComplete();
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
            if(disposing) {  // Dispose managed resources.  
               _ctx.ConfigChangeAlert -= Initialize;
               for(var i=0; i<_watchers.Count; i++) {
                  _watchers[i].Dispose();
               }
               _watchers = null;
            }

            // Call the appropriate methods to clean up unmanaged resources here.
            // If disposing is false, only the following code is executed.
            _disposed = true;
         }
      }

      #endregion

   }

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

}
