using System.Collections.Generic;

namespace Matterless.Module.RemoteConfigs
{
    public class Meta
    {
        public int uid { get; set; }
        public int mod { get; set; }
        //public List<object> variants { get; set; }
        //public List<object> seg { get; set; }
    }

    public class ConfigResponse
    {
        public string data { get; set; }
        public string hash { get; set; }
        public Meta meta { get; set; }
    }

    public class MultipleConfigsResponse : Dictionary<string, ConfigResponse>
    {

    }

    public class CatalogModel
    {
        public Dictionary<string,string> catalog { get; set; }

        public bool HasConfigWithHash(string config, string hash)
        {
            if (catalog == null)
                return false;

            if (!catalog.ContainsKey(config))
                return false;

            if (catalog[config] != hash)
                return false;

            return true;
        }

        public void SetConfig(string config, string hash)
        {
            if (catalog == null)
                catalog = new();

            if (!catalog.ContainsKey(config))
                catalog.Add(config, hash);
            else
                catalog[config] = hash;
        }
    }
}