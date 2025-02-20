using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
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
using System.Windows.Shapes;

namespace ConsoleApp.Pages
{
    /// <summary>
    /// MessageComboBox.xaml 的交互逻辑
    /// </summary>
    public partial class MessageComboBox : CustomDialog
    {
        public Task<object> SelectedItem => tcs.Task;
        private readonly TaskCompletionSource<object> tcs;

        public MessageComboBox(MetroWindow parentWindow = null) : base(parentWindow)
        {
            InitializeComponent();
            tcs = new TaskCompletionSource<object>();
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            (this.OwningWindow ?? (MetroWindow)Application.Current.MainWindow).HideMetroDialogAsync(this);
            tcs.SetResult(null);
        }

        private void Ok_OnOK(object sender, RoutedEventArgs e)
        {
            (this.OwningWindow ?? (MetroWindow)Application.Current.MainWindow).HideMetroDialogAsync(this);
            if (cbxComboBox.SelectedItem != null)
            {
                tcs.SetResult(cbxComboBox.SelectedItem);
            }
            else
            {
                if ((cbxComboBox.Text != null)&&(cbxComboBox.Text.Length != 0))
                {
                    tcs.SetResult(cbxComboBox.Text);
                }else
                {
                    tcs.SetResult(null);
                }
            }
            
        }
    }
}
