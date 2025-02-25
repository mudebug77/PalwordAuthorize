using ET;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Swan.Formatters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public class IPData
    {
        public int hash;
        public string Country;
        public string City;
    }

    [Singleton]
    public class IPCountryPool : Singleton<IPCountryPool>, ISingletonAwake
    {
        private List<IPData> IPHashAndCountry;
        public void Awake()
        {
            IPHashAndCountry = new List<IPData>(200);
        }
        public IPData PushHistoryCountry(int iphash, string country, string city)
        {
            var data = new IPData() { hash = iphash, Country = country, City = city };
            IPHashAndCountry.Add(data);
            if (IPHashAndCountry.Count > 198)
            {
                IPHashAndCountry.RemoveAt(0);
            }
            return data;
        }
        public async ETTask<IPData> GetCountry(string ip)
        {
            var hash = ip.GetHashCode();
            var history = IPHashAndCountry.FirstOrDefault(t => t.hash == hash);
            if (history != null)
            {
                return history;
            }

            var jsonString = await $"http://ip-api.com/json/{ip}?time={TimeInfo.Instance.ClientNow()}".GetStringAsync();
            if (jsonString == null || jsonString[0] != '{')
            {
                return null;
            }
            var retJson = JObject.Parse(jsonString);
            if (retJson["status"].ToString() == "success")
            {
                var sCountry = retJson["country"].ToString();
                var sCity = retJson["city"].ToString();
                var sIsp = retJson["isp"].ToString();
                Log.Info($"ip:{ip} country:{sCountry} isp:{sIsp} city:{sCity}");
                var data = PushHistoryCountry(hash, sCountry, sCity);
                return data;
            }else
            {
                if (retJson.ContainsKey("message"))
                {
                    var message = retJson["message"].ToString();
                    if (message == "private range" || ip == "127.0.0.1")
                    {
                        Log.Info($"ip:{ip} country:private");
                        var data = PushHistoryCountry(hash, "private", "ALL");
                        return data;
                    }
                }
            }
            return null;
        }
    }
}
