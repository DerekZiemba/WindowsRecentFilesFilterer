using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using ZMBA;

namespace WindowsRecentFilesFilterer {

   internal class LocationWatcher : IDisposable {
      private AppContext _ctx;
      private FileSystemWatcher _fileWatcher;

      public readonly Configuration.LocationNode Location;
      
      public LocationWatcher(AppContext ctx, Configuration.LocationNode node) {
         _ctx = ctx;
         Location = node;

         //Check if symlink and if so resolve the target
         string sPath = node.sFullPath;
         if((File.GetAttributes(sPath) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint) {
            using(FileStream fs = File.OpenRead(sPath)) {
               StringBuilder path = new StringBuilder(512);
               Interop.GetFinalPathNameByHandle(fs.SafeFileHandle.DangerousGetHandle(), path, path.Capacity, 0);
               sPath = path.ToString();
            }
         }

         _fileWatcher = new FileSystemWatcher(sPath, node.sWatch);
         _fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
         _fileWatcher.EnableRaisingEvents = true;
         _fileWatcher.Created += HandleFileCreated;
      }



      public async Task RunFiltersAsync() {
         await Task.Run((Action)RunFilters);
      }

      public void RunFilters() {
         string[] filenames = Directory.GetFiles(Location.sFullPath, Location.sWatch);
         for(var i = 0; i < filenames.Length; i++) {
            string name = filenames[i];
            if(MatchesFilter(name)) {
               File.Delete(name);
            }
         }
      }


      private void HandleFileCreated(object sender, FileSystemEventArgs args) {
         if(MatchesFilter(args.FullPath)) {
            File.Delete(args.FullPath);
         }
      }


      private bool MatchesFilter(string fullpath) {
         string targetPath = null;
         if(fullpath.EndsWith(".lnk")) {
            targetPath = ((IWshRuntimeLibrary.IWshShortcut)_ctx.WindowsScriptShell.CreateShortcut(fullpath)).TargetPath;
         }

         for(var i=0; i< Location.lsFilters.Count; i++) {
            var filter = Location.lsFilters[i];
            if(filter.bToTargetPath) {
               if(!String.IsNullOrEmpty(targetPath) && targetPath.Like(filter.sInclude) && !targetPath.Like(filter.sExclude)) {
                  return true;
               }
            } else if(fullpath.Like(filter.sInclude) && !fullpath.Like(filter.sExclude)) {
               return true;
            }            
         }
         return false;        
      }




      #region IDisposable

      
      ~LocationWatcher() {
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
            _fileWatcher.Dispose();
         }
         // Call the appropriate methods to clean up unmanaged resources here. If disposing is false, only the following code is executed. 
         _disposed = true;
      }
      protected bool _disposed;

      #endregion

   }

}
