using ControlzEx.Standard;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Prospect.Unreal.Serialization;
using Renci.SshNet.Messages;
using Swan;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using UIConsole.App;
using Windows.ApplicationModel.Contacts;

namespace ET
{
    public class WebServerController : WebApiController
    {
        private string ExtractParameter(string input, string paramName)
        {
            //var pattern = $@"[?&]{paramName}=([^?&]*)";
            var pattern = $@"[?&]{paramName}=([^?&#]*)";
            var match = Regex.Match(input, pattern);
            // 如果找到匹配项，则返回参数值，否则返回 null
            return match.Success ? match.Groups[1].Value : null;
        }

        private string ReplaceParameterValue(string input, string paramName, string newValue)
        {
            // 正则表达式查找并替换指定参数的值
            var pattern = $@"([?&]){paramName}=([^?&#]*)";
            var replacement = $"$1{paramName}={newValue}";  // 替换为新的值

            return Regex.Replace(input, pattern, replacement);
        }

        private JObject BuildFail(string RemoteAddress,int code)
        {
            var json = new JObject();
            json["code"] = code;
            json["RemoteAddress"] = RemoteAddress;

            return json;
        }

        private JObject BuildSuccess(string RemoteAddress, FBitReader fbr,string newSteamID)
        {
            var json = new JObject();
            fbr.Pos = 8;

            var ServerName = fbr.ReadString();
            var Parameter = fbr.ReadString();
            var fbr_SteamID = fbr.ReadUniqueNetId();
            var ClientType = fbr.ReadString();

            json["code"] = 0;
            json["RemoteAddress"] = RemoteAddress;

            FBitWriter fbw = new FBitWriter();
            fbw.SetAllowResize(true);

            fbw.WriteByte(5);
            fbw.WriteString(ServerName);
            var newParameter = ReplaceParameterValue(Parameter, "ServerPassword", MainConfig.Instance.PalwordServerPassword);
            fbw.WriteString(newParameter,true);
            fbw.WriteUniqueNetId(newSteamID,29);
            fbw.WriteString(ClientType);

            var BitHex = fbw.GetData().ToHex(0, (int)fbw.GetNumBytes());
            json["BitHex"] = BitHex;

            return json;
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
                    var json = JObject.Parse(content);
                    var RemoteAddress = json["RemoteAddress"].ToString();
                    var hexBuffer = json["BitHex"].ToString().FormatBytes();
                    FBitReader fbr = new FBitReader(hexBuffer);
                    fbr.Pos = 8;
                    var ServerName = fbr.ReadString();
                    var Parameter = fbr.ReadString();
                    var SteamID = fbr.ReadUniqueNetId();
                    var ClientType = fbr.ReadString();
                    var ipCountry = "";

                    if (MainConfig.Instance.EnableCountryCheck)
                    {
                        var ip_data = RemoteAddress.Split(':');
                        if (ip_data.Length != 2 || ip_data[0].Length == 0)
                        {
                            return BuildFail(RemoteAddress, -1).ToString();
                        }

                        var ok = false;
                        ipCountry = await IPCountryPool.Instance.GetCountry(ip_data[0]);
                        var c_data = MainConfig.Instance.CountryCheckList.FirstOrDefault(t => t.Enable && t.Country == ipCountry);
                        if (c_data != null)
                        {
                            if (c_data.CountryCheckType == ECountryCheckType.区域允许)
                            {
                                ok = true;
                            }
                        }
                        else
                        {
                            //其他区域禁止
                            if (MainConfig.Instance.OtherCountryCheckType == ECountryCheckType.区域允许)
                            {
                                ok = true;
                            }
                        }

                        if (!ok)
                        {
                            Log.Info($"用户区域被禁止:{RemoteAddress} [{ipCountry}] [{SteamID}]");
                            return BuildFail(RemoteAddress, -2).ToString();
                        }
                    }
                    PasswordUserData p_data = null;
                    if (MainConfig.Instance.EnablePasswordUser)
                    {
                        string userPassword = ExtractParameter(Parameter, "ServerPassword");
                        if (userPassword == null)
                        {
                            Log.Info($"用户未输入服务器密码:{RemoteAddress}");
                            return BuildFail(RemoteAddress, -3).ToString();
                        }
                        p_data = MainConfig.Instance.PasswordUserList.FirstOrDefault(t => t.Enable && t.Password == userPassword);
                        if (p_data == null)
                        {
                            if (MainConfig.Instance.AutoUserCreatePassword)
                            {
                                if (userPassword.Length < MainConfig.Instance.AutoUserCreatePasswordLenth)
                                {
                                    Log.Info($"密码长度不足不予创建:{SteamID} {RemoteAddress} [{userPassword}]");
                                    return BuildFail(RemoteAddress, -4).ToString();
                                }
                                p_data = MainConfig.Instance.PasswordUserList.FirstOrDefault(t=>t.SteamID == SteamID);
                                if (p_data != null)
                                {
                                    Log.Info($"用户SteamID已存在不予创建:{SteamID} {RemoteAddress} [{userPassword}]");
                                    return BuildFail(RemoteAddress, -4).ToString();
                                }

                                Log.Info($"自动创建用户:{SteamID} {RemoteAddress} [{userPassword}]");
                                await MainWindow.Ptr.Dispatcher.BeginInvoke(() =>
                                {
                                    p_data = new PasswordUserData() { Enable = true, Password = userPassword, SteamID = SteamID, Remark = $"AutoCraete {DateTime.Now} {RemoteAddress}" };
                                    MainConfig.Instance.PasswordUserList.Add(p_data);
                                });
                            }
                            else
                            {
                                Log.Info($"用户输入密码不存在:{RemoteAddress} [{userPassword}]");
                                return BuildFail(RemoteAddress, -4).ToString();
                            }
                        }

                        if (p_data.EnableExpireTime)
                        {
                            if (p_data.ExpireTime < DateTime.Now)
                            {
                                Log.Info($"用户输入密码已过期:{RemoteAddress} [{userPassword}] {p_data.ExpireTime}");
                                return BuildFail(RemoteAddress, -5).ToString();
                            }
                        }
                    }

                    
                    if (p_data != null)
                    {
                        Log.Info($"登录成功:{RemoteAddress} [{p_data.SteamID}] {ipCountry}");
                        return BuildSuccess(RemoteAddress, fbr, p_data.SteamID).ToString();
                    }
                    else
                    {
                        Log.Info($"登录成功:{RemoteAddress} [{SteamID}] {ipCountry}");
                        return BuildSuccess(RemoteAddress, fbr, SteamID).ToString();
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
