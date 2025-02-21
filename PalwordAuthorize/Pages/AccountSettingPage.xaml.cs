using ET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UIConsole.Pages;

namespace ConsoleApp.Pages
{
    /// <summary>
    /// AccountSettingPage.xaml 的交互逻辑
    /// </summary>
    public partial class AccountSettingPage : Page
    {
        public bool LoadingUi = false;
        public AccountSettingPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingUi = true;

            UI2ConfigHelper.ConfigToGrid(maingrid, MainConfig.Instance);

            uiPasswordUserList.ItemsSource = MainConfig.Instance.PasswordUserList;
            uiCountryCheckList.ItemsSource = MainConfig.Instance.CountryCheckList;

            LoadingUi = false;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {

        }
        private void ConfigChanged(object sender, EventArgs e)
        {
            if (LoadingUi) return;
            UI2ConfigHelper.HandleUiElementChangedEvent(sender, MainConfig.Instance);
        }

        private void mi_PasswordUserList_add_Click(object sender, RoutedEventArgs e)
        {
            MainConfig.Instance.PasswordUserList.Add(new PasswordUserData() { Enable = false, Password = "888888", SteamID = "00000000000000000",ExpireTime = DateTime.Now });
        }

        private void mi_PasswordUserList_del_Click(object sender, RoutedEventArgs e)
        {
            var select = uiPasswordUserList.SelectedItem as PasswordUserData;
            if (select == null) return;
            MainConfig.Instance.PasswordUserList.Remove(select);
        }

        private void mi_CountryCheckList_add_Click(object sender, RoutedEventArgs e)
        {
            MainConfig.Instance.CountryCheckList.Add(new CountryCheckData() { Enable = false,  Country= "private" });
        }

        private void mi_CountryCheckList_del_Click(object sender, RoutedEventArgs e)
        {
            var select = uiCountryCheckList.SelectedItem as CountryCheckData;
            if (select == null) return;
            MainConfig.Instance.CountryCheckList.Remove(select);
        }
    }
}
