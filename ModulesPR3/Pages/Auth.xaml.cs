using System;
using System.Windows;
using System.Windows.Controls;

namespace ModulesPR3.Pages
{
    public partial class Auth : Page
    {
        public Auth()
        {
            InitializeComponent();
        }

        private void BtnLogIn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new Clients());
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        private void BtnLogInGuest_OnClick(object sender, RoutedEventArgs e)
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
    }
}