using System;
using System.Windows;
using System.Windows.Controls;
using HashPasswords;
using ModulesPR3.Services;
using ModulesPR5.Models;
using ModulesPR5;
using System.Linq;

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
                string login = txtbLogin.Text.Trim();
                string password = Hash.HashPassword(txtbPassword.Password.Trim());

                var db = Helper.GetContext();

                var auth = db.Auth.FirstOrDefault(x => x.login == login && x.password == password);
                if (auth != null)
                {
                    var user = db.Applicants.FirstOrDefault(x => x.auth_id == auth.id);
                    var role = db.Roles.FirstOrDefault(x => x.id == auth.role_id);
                    if (auth != null)
                    {
                        MessageBox.Show("Вы вошли под: " + role.Title.ToString());
                        LoadPage(role.Title.ToString());
                    }
                    else
                    {
                         MessageBox.Show("Вы ввели логин или пароль неверно!");
                         GenerateCaptcha();
                    }
                }
                else
                {
                    MessageBox.Show("Введите данные заново!");
                    GenerateCaptcha();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show(exception.Message);
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

        private void GenerateCaptcha()
        {
            tbCaptcha.Visibility = Visibility.Visible;
            txtbCaptcha.Visibility = Visibility.Visible;
            
            string captchaText = CaptchaGenerator.GenerateCaptchaText(6);
            tbCaptcha.Text = captchaText;
            tbCaptcha.TextDecorations = TextDecorations.Strikethrough;
        }
        
        private void LoadPage(string _role)
        {
            switch (_role)
            {
                case "Applicant":
                    NavigationService.Navigate(new Clients());
                    break;
            }
        }
    }
}