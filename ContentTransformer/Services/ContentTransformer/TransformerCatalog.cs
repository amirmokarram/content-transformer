using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace ContentTransformer.Services.ContentTransformer
{
    internal class TransformerCatalog
    {
        [JsonProperty("transformers")]
        public TransformerConfig[] Transformers { get; set; }

        public static TransformerCatalog TryLoad()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string directoryName = Path.GetDirectoryName(assembly.Location);
            if (directoryName == null)
                throw new DirectoryNotFoundException();
            string catalogFileName = Path.Combine(directoryName, "TransformerCatalog.json");
            if (!File.Exists(catalogFileName))
                return new TransformerCatalog();
            string configJsonContent = File.ReadAllText(catalogFileName);
            return JsonConvert.DeserializeObject<TransformerCatalog>(configJsonContent);
        }
    }
    internal class TransformerConfig
    {
        [JsonProperty("typeName")]
        public string TypeName { get; set; }
        [JsonProperty("contentSource")]
        public TransformerContentSourceConfig ContentSource { get; set; }
    }
    internal class TransformerContentSourceConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("config")]
        public Dictionary<string, string> Config { get; set; }
    }
}
