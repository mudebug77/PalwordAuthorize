using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ET
{

    public class PasswordUserData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public bool Enable { get; set; }
        public string Password { get; set; }
        public string SteamID { get; set; }
        public bool EnableExpireTime { get; set; }
        public DateTime ExpireTime { get; set; }
        public string Remark { get; set; }
    }


    public enum ECountryCheckType : int
    {
        区域允许,
        区域禁止,
    }
    public class CountryCheckData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public bool Enable { get; set; }
        public string Country { get; set; }
        public ECountryCheckType CountryCheckType { get; set; }
    }


    public class MainConfig : Singleton<MainConfig>, ISingletonAwake, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void Awake()
        {
            NewtonsoftJsonHelper.LoadJsonFile(this, "./Config/MainConfig.json");
        }

        public override void Dispose()
        {
            SavetConfig();
        }

        public void SavetConfig()
        {
            var savePath = "./Config/MainConfig.json";
            NewtonsoftJsonHelper.SaveJsonFile(this, savePath);
        }

        public string HttpServerBind { get; set; } = "http://*:10010";

        public string PalwordServerPassword { get; set; } = "88888888";
        public bool EnablePasswordUser { get; set; } = true;

        //[JsonProperty("PasswordUserList_v2")]
        public ObservableCollection<PasswordUserData> PasswordUserList { get; private set; } = new ObservableCollection<PasswordUserData>();


        public bool EnableCountryCheck { get; set; } = false;
        public ECountryCheckType OtherCountryCheckType { get; set; } = ECountryCheckType.区域禁止;
        public ObservableCollection<CountryCheckData> CountryCheckList { get; private set; } = new ObservableCollection<CountryCheckData>();


        public bool AutoUserCreatePassword { get; set; } = false;
        public int AutoUserCreatePasswordLenth { get; set; } = 8;
    }
}
