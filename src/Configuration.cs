using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZMBA;

namespace WindowsRecentFilesFilterer {


   internal class Configuration {
      internal const string DefaultConfigFileName = "WindowsRecentFilesFilterer.config";

      internal bool IsDefaultConfig { get; private set; }
      internal bool ConfigFileExists { get; private set; }
      internal bool LoadConfigFailed { get; private set; }


      internal int FilterInterval { get; private set; }
      internal LocationNode[] FilterLocations { get; private set; }
      internal FilterNode[] FilterRules { get; private set; }


      private Configuration() { }

      private void LoadConfigXML(string sxml) {
         XmlDocument doc = new XmlDocument();
         doc.LoadXml(sxml);
         XmlNode root = doc.SelectSingleNode("//configuration");

         Int32.TryParse(root.SelectSingleNode("filterinterval")?.Attributes["seconds"]?.Value, out var interval);
         if(interval > 0) { FilterInterval = Math.Max(60, interval); }

         FilterRules = root.SelectNodes("filters/filter").Cast<XmlNode>().Select(x => new FilterNode(x)).OrderBy(x => x.nRank).ToArray();

         XmlNodeList nodes = root.SelectNodes("locations/location");
         FilterLocations = new LocationNode[nodes.Count];
         for(var i = 0; i < FilterLocations.Length; i++) {
            FilterLocations[i] = new LocationNode(nodes[i]);
            FilterLocations[i].AddApplicableFilters(FilterRules);
            FilterLocations[i].AddApplicableFilters(nodes[i].SelectNodes("filter").Cast<XmlNode>().Select(x => new FilterNode(x)));
         }
      }


      internal static async Task<Configuration> GetConfiguration() {
         Configuration cfg = new Configuration();
         cfg.LoadConfigXML(Properties.Resources.defaultConfigXML);
         cfg.IsDefaultConfig = true;
         cfg.ConfigFileExists = File.Exists(DefaultConfigFileName);

         if(cfg.ConfigFileExists) {
            try {
               cfg.LoadConfigXML(await ReadConfigFile(DefaultConfigFileName));
               cfg.IsDefaultConfig = false;
            } catch(Exception ex) {
               cfg.LoadConfigFailed = true;
               cfg.LoadConfigXML(Properties.Resources.defaultConfigXML);
               Program.Ctx.ShowError("Failed to load custom configuration", ex);
            }
         }
         return cfg;
      }

      public async Task SaveCurrentConfig() {
         try {
            await SaveConfigFile(DefaultConfigFileName, Properties.Resources.defaultConfigXML);
         } catch(Exception ex) {
            Program.Ctx.ShowError("Failed to save configuration", ex);
         }
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

      internal enum LocationNodeType {
         Folder,
         Registry
      }
      internal enum FilterNodeType {
         FileName,
         Shortcut
      }

      internal class LocationNode {
         public LocationNodeType eType;
         public int nRank;
         public string sWatch;
         public string sPath;
         public string sFullPath;
         public List<FilterNode> lsFilters = new List<FilterNode>();
         public LocationNode(XmlNode node) {
            Enum.TryParse(node.Attributes["type"].Value.Trim(), true, out eType);
            if(!int.TryParse(node.Attributes["rank"]?.Value.Trim(), out nRank)) { nRank = int.MaxValue; }
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
         public FilterNodeType eType;
         public int nRank;
         public string sLocation;
         public string sInclude;
         public string sExclude;
         public FilterNode(XmlNode node) {
            Enum.TryParse(node.Attributes["type"]?.Value.Trim(), true, out eType);
            if(!int.TryParse(node.Attributes["rank"]?.Value.Trim(), out nRank)) { nRank = int.MaxValue; }
            sLocation = node.Attributes["location"]?.Value.Trim() ?? "*";
            sInclude = node.Attributes["include"]?.Value.Trim() ?? "";
            sExclude = node.Attributes["exclude"]?.Value.Trim() ?? "";
         }
      }


   }
}
