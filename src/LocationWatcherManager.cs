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

      internal event EventHandler<FilterRunCompleteEventArgs> FilterRunComplete;

      public LocationWatcherManager(AppContext ctx) {
         _ctx = ctx;
         ctx.ConfigChangeAlert += HandleConfigChange;
         ctx.FilterIntervalTick += HandleFilterIntervalEvent;
      }


      private void HandleConfigChange(object sender, EventArgs args) {
         for(var i = 0; i < _watchers.Count; i++) {
            _watchers[i].Dispose();
         }
         _watchers.Clear();
         for(var i = 0; i < _ctx.Cfg.FilterLocations.Length; i++) {
            _watchers.Add(new LocationWatcher(_ctx, _ctx.Cfg.FilterLocations[i]));
         }
         RunFiltersAsync();
      }

      private void HandleFilterIntervalEvent(object sender, EventArgs args)=> RunFiltersAsync();

      public async Task RunFiltersAsync() {
         await Task.Run((Action)RunFilters);
      }
      public void RunFilters() {
         var sw = System.Diagnostics.Stopwatch.StartNew();
         for(var i = 0; i < _watchers.Count; i++) {
            _watchers[i].RunFilters();
         }
         FilterRunComplete(this, new FilterRunCompleteEventArgs() { RuntimeMilliseconds = sw.ElapsedMilliseconds });
      }


      public class FilterRunCompleteEventArgs : System.EventArgs {
         public long RuntimeMilliseconds;
      }

      #region IDisposable
      ~LocationWatcherManager() {
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
         if(disposing) { // Dispose managed resources.  
            _ctx.ConfigChangeAlert -= HandleConfigChange;
            _ctx.FilterIntervalTick -= HandleFilterIntervalEvent;
            for(var i=0; i<_watchers.Count; i++) {
               _watchers[i].Dispose();
            }
            _watchers = null;

            foreach(Delegate ev in FilterRunComplete.GetInvocationList()) { FilterRunComplete -= (EventHandler<FilterRunCompleteEventArgs>)ev; }
         }
         // Call the appropriate methods to clean up unmanaged resources here. If disposing is false, only the following code is executed. 
         _disposed = true;
      }
      protected bool _disposed;


      #endregion

   }

}
