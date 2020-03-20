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
		private List<WatchedLocation> _locations = new List<WatchedLocation>();

		internal event EventHandler<FilterRunCompleteEventArgs> FilterRunComplete;

		public LocationWatcherManager () {
			Program.Ctx.ConfigChangeAlert += HandleConfigChange;
			Program.Ctx.FilterIntervalTick += HandleFilterIntervalEvent;
		}


		private void HandleConfigChange (object sender, EventArgs args) {
			try {
				lock(_locations) {
					for(var i = 0; i < _locations.Count; i++) {
						_locations[i].Watcher.Dispose();
					}
					_locations.Clear();

					StringBuilder sb = StringBuilderCache.Take(255);
					for(var i = 0; i < Program.Ctx.Cfg.FilterLocations.Length; i++) {
						WatchedLocation loc = new WatchedLocation{Location= Program.Ctx.Cfg.FilterLocations[i]};
						string sPath = loc.Location.sFullPath;

						try {
							if(( File.GetAttributes(sPath) & FileAttributes.ReparsePoint ) == FileAttributes.ReparsePoint) {
								using(FileStream fs = File.OpenRead(sPath)) {
									sb.Clear();
									Interop.GetFinalPathNameByHandle(fs.SafeFileHandle.DangerousGetHandle(), sb, sb.Capacity, 0);
									sPath = sb.ToString();
								}
							}
							loc.Watcher = new FileSystemWatcher(sPath, loc.Location.sWatch);
							loc.Watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
							loc.Watcher.EnableRaisingEvents = true;
							loc.Watcher.Created += loc.HandleFileCreated;

							_locations.Add(loc);

						} catch(Exception ex) {
							sb.Clear();
							sb.Append("Failed to create FileSystemWatcher for Location: ").Append(loc.Location.sPath).AppendLine();
							sb.Append("Resolved Final Path Name: ").Append(sPath);
							Program.Ctx.ShowError(sb.ToStringClear(), ex);
						}
					}
					StringBuilderCache.Return(ref sb);
				}

				RunFiltersAsync();

			} catch(Exception ex) {
				Program.Ctx.ShowError("Failed to handle config change.", ex);
			}
		}

		private void HandleFilterIntervalEvent (object sender, EventArgs args) => RunFiltersAsync();

		public async Task RunFiltersAsync () {
			await Task.Run((Action)RunFilters);
		}

		private void RunFilters () {
			try {
				long millis = 0;
				lock(_locations) {
					var sw = System.Diagnostics.Stopwatch.StartNew();
					for(var i = 0; i < _locations.Count; i++) {
						WatchedLocation loc = _locations[i];
						string name = null;
						try {
							string[] filenames = Directory.GetFiles(loc.Location.sFullPath, loc.Location.sWatch);
							for(var j = 0; j < filenames.Length; j++) {
								name = filenames[j];
								if(MatchesFilter(loc.Location.lsFilters, name)) {
									File.Delete(name);
								}
							}
						} catch(Exception ex) {
							Program.Ctx.ShowError($"Failed to run filter at location: {loc.Location.sFullPath} for file: {name}", ex);
						}
					}
					millis = sw.ElapsedMilliseconds;
				}
				FilterRunComplete(this, new FilterRunCompleteEventArgs() { RuntimeMilliseconds = millis });
			} catch(Exception ex) {
				Program.Ctx.ShowError("Failed to run filters.", ex);
			}
		}


		private static bool MatchesFilter (List<Configuration.FilterNode> filters, string fullpath) {
			string targetPath = null;
			for(var i = 0; i < filters.Count; i++) {
				var filter = filters[i];
				if(filter.eType == Configuration.FilterNodeType.Shortcut) {
					if(targetPath == null) {
						targetPath = fullpath.EndsWith(".lnk") ? ( (IWshRuntimeLibrary.IWshShortcut)Program.Ctx.WindowsScriptShell.CreateShortcut(fullpath) ).TargetPath : "";
					}
					if(!String.IsNullOrEmpty(targetPath) && targetPath.Like(filter.sInclude) && !targetPath.Like(filter.sExclude)) {
						return true;
					}
				} else if(fullpath.Like(filter.sInclude) && !fullpath.Like(filter.sExclude)) {
					return true;
				}
			}
			return false;
		}

		private static void CheckNewlyCreatedFile (Configuration.LocationNode loc, string path) {
			if(MatchesFilter(loc.lsFilters, path)) {
				try {
					File.Delete(path);
				} catch(Exception) {
					TryLater();
				}
			}

			async void TryLater() {
				await Task.Delay(1000);
				try {
					File.Delete(path); 
				} catch(Exception ex) {
					Program.Ctx.ShowError("Failed to delete newly created file.", ex);
				}
			}
		}

		public class FilterRunCompleteEventArgs : System.EventArgs {
			public long RuntimeMilliseconds;
		}

		private struct WatchedLocation {
			public Configuration.LocationNode Location;
			public FileSystemWatcher Watcher;
			internal void HandleFileCreated (object sender, FileSystemEventArgs args) => CheckNewlyCreatedFile(Location, args.FullPath);
		}

		#region IDisposable
		~LocationWatcherManager () {
			Dispose(false);
		}

		// Implement IDisposable.
		// Do not make this method virtual.
		// A derived class should not be able to override this method.
		public void Dispose () {
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to take this object off the finalization queue and prevent finalization code for this object from executing a second time.
			GC.SuppressFinalize(this);
		}

		// Dispose(bool disposing) executes in two distinct scenarios.
		// If disposing equals true, the method has been called directly or indirectly by a user's code. Managed and unmanaged resources can be disposed.
		// If disposing equals false, the method has been called by the runtime from inside the finalizer and you should not reference other objects. Only unmanaged resources can be disposed.
		protected virtual void Dispose (bool disposing) { // If disposing equals true, dispose all managed and unmanaged resources.
			if(_disposed) { return; } //Guard against repeat disposals
			if(disposing) { // Dispose managed resources.  
				Program.Ctx.ConfigChangeAlert -= HandleConfigChange;
				Program.Ctx.FilterIntervalTick -= HandleFilterIntervalEvent;
				lock(_locations) {
					for(var i = 0; i < _locations.Count; i++) {
						_locations[i].Watcher.Dispose();
					}
					_locations.Clear();
				}
				_locations = null;

				foreach(Delegate ev in FilterRunComplete.GetInvocationList()) { FilterRunComplete -= (EventHandler<FilterRunCompleteEventArgs>)ev; }
			}
			// Call the appropriate methods to clean up unmanaged resources here. If disposing is false, only the following code is executed. 
			_disposed = true;
		}
		protected bool _disposed;


		#endregion

	}

}
