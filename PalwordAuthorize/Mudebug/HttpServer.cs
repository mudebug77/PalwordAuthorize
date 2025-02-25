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

                    var ip_data = RemoteAddress.Split(':');
                    if (ip_data.Length != 2 || ip_data[0].Length == 0)
                    {
                        Log.Info($"解析IP数据错误:{RemoteAddress}");
                        return BuildFail(RemoteAddress, -1).ToString();
                    }

                    //{"Parameter":"?ServerPassword=1111111111?Name=Noob#initgame","MessageType":5,"ServerName":"0","Pad_1":29,"ClientType":"STEAM","RemoteAddress":"127.0.0.1:62077","SteamID":"076561197960287900"} 
                    FBitReader fbr = new FBitReader(hexBuffer);
                    fbr.Pos = 8;
                    var ServerName = fbr.ReadString();
                    var Parameter = fbr.ReadString();
                    var SteamID = fbr.ReadUniqueNetId();
                    var ClientType = fbr.ReadString();
                    string userPassword = ExtractParameter(Parameter, "ServerPassword");
                    string userName = ExtractParameter(Parameter, "Name");
                    IPData ipData = null;


                    var blacklist = MainConfig.Instance.BlackList.FirstOrDefault(t => t.Enable && (t.Text == SteamID || t.Text == ip_data[0] || t.Text == userName));
                    if (blacklist != null)
                    {
                        Log.Info($"该用户是黑名单:{SteamID} [{userName}] {ip_data[0]}");
                        return BuildFail(RemoteAddress, -2).ToString();
                    }

                    if (MainConfig.Instance.EnableCountryCheck)
                    {
                        ipData = await IPCountryPool.Instance.GetCountry(ip_data[0]);
                        if (ipData == null)
                        {
                            Log.Info($"获取IP信息失败:{ip_data[0]} {SteamID} [{userName}]");
                            return BuildFail(RemoteAddress, -2).ToString();
                        }
                        if (!IPCheck(ipData))
                        {
                            Log.Info($"用户IP被禁止:{ip_data[0]} {SteamID} [{userName}] [{ipData?.Country}][{ipData?.City}]");
                            return BuildFail(RemoteAddress, -2).ToString();
                        }
                    }
                    PasswordUserData p_data = null;
                    if (MainConfig.Instance.EnablePasswordUser)
                    {
                        if (userPassword == null)
                        {
                            Log.Info($"用户未输入服务器密码:{ip_data[0]} {SteamID} [{userName}] [{ipData?.Country}][{ipData?.City}]");
                            return BuildFail(RemoteAddress, -3).ToString();
                        }
                        p_data = MainConfig.Instance.PasswordUserList.FirstOrDefault(t => t.Enable && t.Password == userPassword);
                        if (p_data == null)
                        {
                            if (MainConfig.Instance.AutoUserCreatePassword)
                            {
                                if ((userPassword.Length < MainConfig.Instance.AutoUserCreatePasswordLenth)&&(userPassword.Length > 24))
                                {
                                    Log.Info($"密码长度不符不予创建:{SteamID} {ip_data[0]} [{userName}] [{userPassword}]");
                                    return BuildFail(RemoteAddress, -4).ToString();
                                }
                                p_data = MainConfig.Instance.PasswordUserList.FirstOrDefault(t=>t.SteamID == SteamID);
                                if (p_data != null)
                                {
                                    Log.Info($"用户SteamID已存在不予创建:{SteamID} {ip_data[0]} [{userName}] [{userPassword}]");
                                    return BuildFail(RemoteAddress, -4).ToString();
                                }

                                Log.Info($"自动创建用户:{SteamID} {ip_data[0]} [{userName}] [{userPassword}]");
                                await MainWindow.Ptr.Dispatcher.BeginInvoke(() =>
                                {
                                    p_data = new PasswordUserData() { Enable = true, Password = userPassword, SteamID = SteamID, Remark = $"AutoCraete {DateTime.Now} {RemoteAddress}" };
                                    MainConfig.Instance.PasswordUserList.Add(p_data);
                                });
                            }
                            else
                            {
                                Log.Info($"用户输入密码不存在:{ip_data[0]} [{userName}] [{userPassword}]");
                                return BuildFail(RemoteAddress, -4).ToString();
                            }
                        }

                        if (p_data.EnableExpireTime)
                        {
                            if (p_data.ExpireTime < DateTime.Now)
                            {
                                Log.Info($"用户输入密码已过期:{p_data.SteamID} [{userName}][{userPassword}] {p_data.ExpireTime} {ip_data[0]}");
                                return BuildFail(RemoteAddress, -5).ToString();
                            }
                        }
                    }

                    
                    if (p_data != null)
                    {
                        Log.Info($"登录成功:[{p_data.SteamID}] [{userName}] [{ipData?.Country}][{ipData?.City}] {ip_data[0]}");
                        return BuildSuccess(RemoteAddress, fbr, p_data.SteamID).ToString();
                    }
                    else
                    {
                        Log.Info($"登录成功:[{SteamID}] [{userName}] [{ipData?.Country}][{ipData?.City}] {ip_data[0]}");
                        return BuildSuccess(RemoteAddress, fbr, SteamID).ToString();
                    }
                }
            }
            return "{\"code\":-99}";
        }


        private bool IPCheck(IPData ipData)
        {
            var c_data = MainConfig.Instance.CountryCheckList.FirstOrDefault(t => t.Enable && t.Country == ipData.Country && t.City == ipData.City);
            if (c_data != null)
            {
                if (c_data.CountryCheckType == ECountryCheckType.区域允许)
                {
                    return true;
                }
                return false;
            }
            c_data = MainConfig.Instance.CountryCheckList.FirstOrDefault(t => t.Enable && t.Country == ipData.Country && t.City == "ALL");
            if (c_data != null)
            {
                if (c_data.CountryCheckType == ECountryCheckType.区域允许)
                {
                    return true;
                }
                return false;
            }
            if (MainConfig.Instance.OtherCountryCheckType == ECountryCheckType.区域允许)
            {
                return true;
            }
            return false;
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
