using ControlzEx.Standard;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ET
{
    public class WebServerController : WebApiController
    {
        private string ExtractParameter(string input, string paramName)
        {
            // 正则表达式提取指定参数的值
            var pattern = $@"[?&]{paramName}=([^?&]*)";
            var match = Regex.Match(input, pattern);
            // 如果找到匹配项，则返回参数值，否则返回 null
            return match.Success ? match.Groups[1].Value : null;
        }

        private string ReplaceParameterValue(string input, string paramName, string newValue)
        {
            // 正则表达式查找并替换指定参数的值
            var pattern = $@"([?&]){paramName}=([^?&]*)";
            var replacement = $"$1{paramName}={newValue}";  // 替换为新的值

            return Regex.Replace(input, pattern, replacement);
        }

        private JObject BuildSuccess(JObject json,string SteamID)
        {
            var ret = new JObject();
            var oldParameter = json["Parameter"].ToString();
            ret["code"] = 0;
            ret["ServerName"] = json["ServerName"];
            ret["Pad_1"] = json["Pad_1"];
            ret["SteamID"] = SteamID;
            ret["ClientType"] = json["ClientType"];
            ret["RemoteAddress"] = json["RemoteAddress"];

            ret["Parameter"] = ReplaceParameterValue(oldParameter, "ServerPassword", MainConfig.Instance.PalwordServerPassword);
            return ret;
        }

        private JObject BuildFail(JObject json, int code)
        {
            var ret = new JObject();
            ret["code"] = code;
            ret["Parameter"] = json["Parameter"];
            ret["RemoteAddress"] = json["RemoteAddress"];
            return ret;
        }

        [Route(HttpVerbs.Post, "/ClientInitGame")]
        public async Task<string> ClientInitGame()
        {
            using (var reader = new StreamReader(Request.InputStream, Encoding.UTF8))
            {
                var content = await reader.ReadToEndAsync();
                Log.Info(content);
                if ((content != null) && (content[0] == '{'))
                {
                    //{"ServerName":"0","MessageType":5,"RemoteAddress":"127.0.0.1:49942","SteamID":"076561197960287900","ClientType":"STEAM","Parameter":"?ServerPassword=123123?Name=Noob#initgame","Pad_1":29}
                    var json = JObject.Parse(content);

                    //json["Pad_1"] = 29;

                    var Parameter = json["Parameter"].ToString();
                    string serverPassword = ExtractParameter(Parameter, "ServerPassword");
                    string name = ExtractParameter(Parameter, "Name");

                    PasswordUserData p_data = null;
                    if (MainConfig.Instance.EnablePasswordUser)
                    {
                        if (serverPassword == null)
                        {
                            return BuildFail(json, -1).ToString();
                        }

                        p_data = MainConfig.Instance.PasswordUserList.FirstOrDefault(t => t.Enable && t.Password == serverPassword);
                        if (p_data == null)
                        {
                            return BuildFail(json, -2).ToString();
                        }

                        if (p_data.EnableExpireTime)
                        {
                            if (p_data.ExpireTime < DateTime.Now)
                            {
                                return BuildFail(json, -3).ToString();
                            }
                        }
                    }

                    var ok = false;
                    if (MainConfig.Instance.EnableCountryCheck)
                    {
                        var ip_data = json["RemoteAddress"].ToString().Split(':');
                        if (ip_data.Length != 2 || ip_data[0].Length == 0)
                        {
                            return BuildFail(json, -5).ToString();
                        }

                        var jsonString = await $"http://ip-api.com/json/{ip_data[0]}?time=111{TimeInfo.Instance.ClientNow()}".GetStringAsync();
                        if (jsonString[0] != '{')
                        {
                            return BuildFail(json, -6).ToString();
                        }
                        var retJson = JObject.Parse(jsonString);
                        if (retJson["status"].ToString() == "success")
                        {
                            //當前IP:61.227.133.140 區域:Taiwan isp:Chunghwa Telecom Co., Ltd. 
                            var sCountry = retJson["country"].ToString();
                            var sIsp = retJson["isp"].ToString();
                            Log.Info($"{json["SteamID"]} ip:{ip_data[0]} country:{sCountry} isp:{sIsp}");

                            var c_data = MainConfig.Instance.CountryCheckList.FirstOrDefault(t => t.Enable && t.Country == sCountry);
                            if (MainConfig.Instance.CountryCheckType == ECountryCheckType.区域允许)
                            {
                                if (c_data != null)
                                {
                                    ok = true;
                                }
                            }else
                            {
                                //不允许模式
                                if (c_data == null)
                                {
                                    ok = true;
                                }
                            }
                        }else
                        {
                            return BuildFail(json, -4).ToString();
                        }
                    }else
                    {
                        ok = true;
                    }
                    if (ok)
                    {
                        if (p_data != null)
                        {
                            var ret = BuildSuccess(json, p_data.SteamID);
                            return ret.ToString();
                        }
                        else
                        {
                            var ret = BuildSuccess(json, json["SteamID"].ToString());
                            return ret.ToString();
                        }
                    }
                    else
                    {
                        return BuildFail(json, -7).ToString();
                    }
                }
            }
            return "{\"code\":-99}";
        }

    }



    public static class TextResponseSerializer
    {
        public static async Task Text(IHttpContext context, object data)
        {
            if (data is null)
            {
                return;
            }

            var bufferResponse = false;

            var isBinaryResponse = data is byte[];

            if (!context.TryDetermineCompression(context.Response.ContentType, out var preferCompression))
            {
                preferCompression = true;
            }

            if (isBinaryResponse)
            {
                var responseBytes = (byte[])data;
                using var stream = context.OpenResponseStream(bufferResponse, preferCompression);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
            }
            else
            {
                var responseString = data is string stringData ? stringData : data.ToString() ?? string.Empty;
                using var text = context.OpenResponseText(context.Response.ContentEncoding, bufferResponse, preferCompression);
                await text.WriteAsync(responseString).ConfigureAwait(false);
            }
        }
    }


    [Singleton]
    public class HttpServer : Singleton<HttpServer>, ISingletonAwake
    {
        public WebServer Server;
        public void Awake()
        {
            Swan.Logging.Logger.NoLogging();
            Server = new WebServer(o => o
            .WithUrlPrefix(MainConfig.Instance.HttpServerBind)
            .WithMode(HttpListenerMode.EmbedIO))
            .WithLocalSessionManager()
            .WithWebApi("/api", TextResponseSerializer.Text, m => m
            .WithController<WebServerController>()
            );
            Server.RunAsync();
            Log.Info("WebServer 啟動成功");
        }
    }
}
