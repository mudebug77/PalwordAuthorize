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
    [Singleton]
    public class IPCountryPool : Singleton<IPCountryPool>, ISingletonAwake
    {
        private List<(int hash, string Country)> IPHashAndCountry;
        public void Awake()
        {
            IPHashAndCountry = new List<(int hash, string Country)>(200);
        }
        public void PushHistoryCountry(int iphash, string Country)
        {
            IPHashAndCountry.Add((iphash, Country));
            if (IPHashAndCountry.Count > 198)
            {
                IPHashAndCountry.RemoveAt(0);
            }
        }
        public async ETTask<string> GetCountry(string ip)
        {
            var hash = ip.GetHashCode();
            var history = IPHashAndCountry.FirstOrDefault(t => t.hash == hash);
            if (history.hash != 0)
            {
                return history.Country;
            }

            var jsonString = await $"http://ip-api.com/json/{ip}?time={TimeInfo.Instance.ClientNow()}".GetStringAsync();
            if (jsonString == null || jsonString[0] != '{')
            {
                return "";
            }
            var retJson = JObject.Parse(jsonString);
            if (retJson["status"].ToString() == "success")
            {
                var sCountry = retJson["country"].ToString();
                var sIsp = retJson["isp"].ToString();
                Log.Info($"ip:{ip} country:{sCountry} isp:{sIsp}");
                PushHistoryCountry(hash, sCountry);
                return sCountry;
            }else
            {
                var message = retJson["message"].ToString();
                if (message == "private range" || ip == "127.0.0.1")
                {
                    Log.Info($"ip:{ip} country:private");
                    PushHistoryCountry(hash, "private");
                    return "private";
                }
            }
            return "";
        }
    }
}
