using System;
using System.Windows;

namespace ModulesPR3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FrmMain_OnInitialized(object sender, EventArgs e)
        {
            try
            {
                frmMain.Content = new Pages.Btns();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Frame init error:", exception);
                throw;
            }
        }

        private void BtnBack_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                frmMain.GoBack();
            }
            catch (Exception exception)
            {
                Console.WriteLine("btnBack error: ", exception);
                throw;
            }
        }

        private void FrmMain_OnContentRendered(object sender, EventArgs e)
        {
            if (frmMain.CanGoBack)
            {
                btnBack.Visibility = Visibility.Visible;
            }
            else
            {
                btnBack.Visibility = Visibility.Hidden;
            }
        }
    }
}