using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using ZMBA;

namespace WindowsRecentFilesFilterer {


	internal class Configuration {
		internal const string DefaultConfigFileName = "WindowsRecentFilesFilterer.config";

      internal bool GoodConfigExists;
		internal int FilterInterval;
		internal LocationNode[] LocationNodes;
		internal FilterNode[] FilterNodes;


		private Configuration() {
			LoadConfigXML(Properties.Resources.defaultConfigXML);
		}

		private void LoadConfigXML(string sxml) {
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(sxml);
			XmlNode root = doc.SelectSingleNode("//configuration");

         Int32.TryParse(root.SelectSingleNode("filterinterval")?.Attributes["seconds"]?.Value, out var interval);
         if(interval > 0) { FilterInterval = Math.Max(60, interval); }

         XmlNodeList nodes = root.SelectNodes("filters/filter");
			FilterNodes = new FilterNode[nodes.Count];
			for(var i = 0; i < FilterNodes.Length; i++) {
				FilterNodes[i] = new FilterNode(nodes[i]);
         }

			nodes = root.SelectNodes("locations/location");
			LocationNodes = new LocationNode[nodes.Count];
			for(var i = 0; i < LocationNodes.Length; i++) {
				LocationNodes[i] = new LocationNode(nodes[i]);
            LocationNodes[i].AddApplicableFilters(FilterNodes);
            LocationNodes[i].AddApplicableFilters(nodes[i].SelectNodes("filter").Cast<XmlNode>().Select(x => new FilterNode(x)));
			}


		}



		internal static async Task<Configuration> TryGetConfiguration() {
         Configuration cfg = new Configuration();
			if(File.Exists(DefaultConfigFileName)) {
            cfg.LoadConfigXML(await ReadConfigFile(DefaultConfigFileName));
            cfg.GoodConfigExists = true;
			}
         return cfg;
      }

      internal static async Task<Configuration> GetConfiguration() {
         Configuration cfg = new Configuration();
         cfg.LoadConfigXML(await ReadConfigFile(DefaultConfigFileName));
         cfg.GoodConfigExists = true;
         return cfg;
      }

      public async Task SaveCurrentConfig() {
         await SaveConfigFile(DefaultConfigFileName, Properties.Resources.defaultConfigXML);
      }

      private static async Task<bool> SaveConfigFile(string sFilePath, string sxml) {
			using(StreamWriter writer = new StreamWriter(new FileStream(sFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8)) {
				await writer.WriteAsync(sxml);
				return true;
			}
		}

		private static async Task<string> ReadConfigFile(string sFilePath) {
			using(StreamReader reader = new StreamReader(new FileStream(sFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8)) { 
				return await reader.ReadToEndAsync();
			}
		}


		internal class LocationNode {
			public string sWatch;
			public string sPath;
			public string sFullPath;
         public List<FilterNode> lsFilters = new List<FilterNode>();
         public LocationNode(XmlNode node) {
            sWatch = node.Attributes["watch"]?.Value.Trim() ?? "*.*";
				sPath = node.Attributes["path"].Value.Trim();
				sFullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(sPath));
         }
         internal void AddApplicableFilters(IEnumerable<FilterNode> filters) {
            foreach(var filter in filters) {
               if(sFullPath.Like(filter.sLocation)) {
                  lsFilters.Add(filter);
               }
            }
         }
      }

		internal class FilterNode {
         public bool bToTargetPath;
         public string sLocation;			
			public string sInclude;
			public string sExclude;
         public FilterNode(XmlNode node) {
            bool.TryParse(node.Attributes["totargetpath"]?.Value.Trim(), out bToTargetPath);
            sLocation = node.Attributes["location"]?.Value.Trim() ?? "*";
				sInclude = node.Attributes["include"]?.Value.Trim() ?? "";
				sExclude = node.Attributes["exclude"]?.Value.Trim() ?? "";
         }
		}


	}
}