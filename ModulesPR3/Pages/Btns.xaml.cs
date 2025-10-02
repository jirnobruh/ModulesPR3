using System;
using System.Windows;
using System.Windows.Controls;

namespace ModulesPR3.Pages
{
    public partial class Btns : Page
    {
        public Btns()
        {
            InitializeComponent();
        }

        private void BtnAuthGuest_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new Pages.Clients());
            }
            catch (Exception exception)
            {
                Console.WriteLine("btnAuthGuest error:", exception);
                throw;
            }
        }

        private void BtnAuth_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new Auth());
            }
            catch (Exception exception)
            {
                Console.WriteLine("btnAuth error",exception);
                throw;
            }
        }
    }
}