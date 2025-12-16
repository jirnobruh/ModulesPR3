using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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

        private DispatcherTimer lockoutTimer;
        private int lockoutSecondsRemaining = 0;
        private const int LOCKOUT_SECONDS = 10;
        private const int LOCKOUT_THRESHOLD = 3;

        public Auth()
        {
            InitializeComponent();
            HideCaptcha();
            InitializeLockoutTimer();
        }

        private void InitializeLockoutTimer()
        {
            lockoutTimer = new DispatcherTimer();
            lockoutTimer.Interval = TimeSpan.FromSeconds(1);
            lockoutTimer.Tick += LockoutTimer_Tick;
        }

        private void LockoutTimer_Tick(object sender, EventArgs e)
        {
            lockoutSecondsRemaining--;
            if (lockoutSecondsRemaining <= 0)
            {
                StopLockout();
            }
            else
            {
                UpdateLockTimerText();
            }
        }

        private void StartLockout()
        {
            SetInputsEnabled(false);

            lockoutSecondsRemaining = LOCKOUT_SECONDS;
            UpdateLockTimerText();
            tbLockTimer.Visibility = Visibility.Visible;

            lockoutTimer.Start();
        }

        private void StopLockout()
        {
            lockoutTimer.Stop();
            tbLockTimer.Visibility = Visibility.Collapsed;
            SetInputsEnabled(true);
        }

        private void UpdateLockTimerText()
        {
            tbLockTimer.Text = $"До разблокировки: {lockoutSecondsRemaining} с.";
        }

        private void SetInputsEnabled(bool enabled)
        {
            txtbLogin.IsEnabled = enabled;
            txtbPassword.IsEnabled = enabled;
            btnLogIn.IsEnabled = enabled;
            btnLogInGuest.IsEnabled = enabled;
            txtbCaptcha.IsEnabled = enabled;
        }

        private void BtnLogIn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!btnLogIn.IsEnabled) return;

                string login = txtbLogin.Text.Trim();
                string passwordHash = Hash.HashPassword(txtbPassword.Password.Trim());

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(passwordHash))
                {
                    MessageBox.Show("Введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка рабочего времени до авторизации
                var now = DateTime.Now;
                if (!Session.IsWithinWorkHours(now))
                {
                    MessageBox.Show("Доступ к системе запрещён: сейчас не рабочее время (10:00 - 19:00).", "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Если капча показана, проверяем её ввод до обращения к БД
                if (IsCaptchaVisible())
                {
                    string entered = txtbCaptcha.Text?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(entered) || !string.Equals(entered, currentCaptcha, StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Код капчи введён неверно. Попробуйте снова.");
                        GenerateCaptcha();
                        failedAttempts++;
                        CheckLockoutAfterFailure();
                        return;
                    }
                }

                var db = Helper.GetContext();

                var auth = db.Auth.FirstOrDefault(x => x.login == login && x.password == passwordHash);
                if (auth != null)
                {
                    var role = db.Roles.FirstOrDefault(x => x.id == auth.role_id);

                    var applicant = db.Applicants.FirstOrDefault(a => a.auth_id == auth.id);

                    UserSession userSession;
                    if (applicant != null)
                    {
                        userSession = new UserSession
                        {
                            Id = applicant.id,
                            LastName = applicant.last_name ?? string.Empty,
                            FirstName = applicant.first_name ?? string.Empty,
                            MiddleName = applicant.middle_name ?? string.Empty,
                            Role = role?.Title ?? string.Empty
                        };
                    }
                    else
                    {
                        applicant = db.Applicants.FirstOrDefault(a => a.email == auth.login);
                        if (applicant != null)
                        {
                            userSession = new UserSession
                            {
                                Id = applicant.id,
                                LastName = applicant.last_name ?? string.Empty,
                                FirstName = applicant.first_name ?? string.Empty,
                                MiddleName = applicant.middle_name ?? string.Empty,
                                Role = role?.Title ?? string.Empty
                            };
                        }
                        else
                        {
                            userSession = new UserSession
                            {
                                Id = auth.id,
                                LastName = role?.Title ?? "Пользователь",
                                FirstName = string.Empty,
                                MiddleName = string.Empty,
                                Role = role?.Title ?? string.Empty
                            };
                        }
                    }

                    Session.CurrentUser = userSession;

                    HideCaptcha();
                    failedAttempts = 0;

                    // Показать приветствие
                    var greeting = Session.GetGreeting(now);
                    if (!string.IsNullOrEmpty(greeting))
                    {
                        MessageBox.Show(greeting, "Приветствие", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    LoadPage(role?.Title?.ToString());
                }
                else
                {
                    MessageBox.Show("Вы ввели логин или пароль неверно!");
                    failedAttempts++;
                    GenerateCaptcha();
                    CheckLockoutAfterFailure();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show(exception.Message);
            }
        }

        private void CheckLockoutAfterFailure()
        {
            if (failedAttempts >= LOCKOUT_THRESHOLD)
            {
                StartLockout();
            }
        }

        private void BtnLogInGuest_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!btnLogInGuest.IsEnabled) return;

                Session.CurrentUser = new UserSession
                {
                    Id = 0,
                    LastName = "Гость",
                    FirstName = string.Empty,
                    MiddleName = string.Empty,
                    Role = "Guest"
                };

                var greeting = Session.GetGreeting(DateTime.Now);
                if (!string.IsNullOrEmpty(greeting))
                {
                    MessageBox.Show(greeting, "Приветствие", MessageBoxButton.OK, MessageBoxImage.Information);
                }

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