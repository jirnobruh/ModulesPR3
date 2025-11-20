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
        private int failedAttempts = 0;
        private string currentCaptcha = string.Empty;

        public Auth()
        {
            InitializeComponent();
            // Капча скрыта по умолчанию в XAML, но на всякий случай явно скрываем здесь
            HideCaptcha();
        }

        private void BtnLogIn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = txtbLogin.Text.Trim();
                string passwordHash = Hash.HashPassword(txtbPassword.Password.Trim());

                // Если капча уже показана, проверяем её ввод до обращения к БД
                if (IsCaptchaVisible())
                {
                    string entered = txtbCaptcha.Text?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(entered) || !string.Equals(entered, currentCaptcha, StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Код капчи введён неверно. Попробуйте снова.");
                        GenerateCaptcha(); // обновляем капчу при неверном вводе
                        failedAttempts++;
                        return;
                    }
                }

                var db = Helper.GetContext();

                var auth = db.Auth.FirstOrDefault(x => x.login == login && x.password == passwordHash);
                if (auth != null)
                {
                    var role = db.Roles.FirstOrDefault(x => x.id == auth.role_id);
                    MessageBox.Show("Вы вошли под: " + role.Title.ToString());
                    // Успешный вход — скрываем капчу и сбрасываем счётчик
                    HideCaptcha();
                    failedAttempts = 0;
                    LoadPage(role.Title.ToString());
                }
                else
                {
                    // Неверный логин/пароль
                    MessageBox.Show("Вы ввели логин или пароль неверно!");
                    failedAttempts++;
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
            // Генерация и показ капчи
            currentCaptcha = CaptchaGenerator.GenerateCaptchaText(6);
            tbCaptcha.Text = currentCaptcha;
            tbCaptcha.TextDecorations = TextDecorations.Strikethrough;
            ShowCaptcha();
        }

        private void ShowCaptcha()
        {
            CaptchaPanel.Visibility = Visibility.Visible;
            tbCaptcha.Visibility = Visibility.Visible;
            txtbCaptcha.Visibility = Visibility.Visible;
            txtbCaptcha.Text = string.Empty;
        }

        private void HideCaptcha()
        {
            CaptchaPanel.Visibility = Visibility.Collapsed;
            tbCaptcha.Visibility = Visibility.Collapsed;
            txtbCaptcha.Visibility = Visibility.Collapsed;
            currentCaptcha = string.Empty;
            txtbCaptcha.Text = string.Empty;
        }

        private bool IsCaptchaVisible()
        {
            return CaptchaPanel.Visibility == Visibility.Visible;
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