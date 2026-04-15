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

        /// <summary>
        /// Инициализирует таймер блокировки и подписывает его на обработчик тиков.
        /// </summary>
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

        /// <summary>
        /// Запускает режим временной блокировки формы входа после нескольких неудачных попыток авторизации.
        /// </summary>
        private void StartLockout()
        {
            SetInputsEnabled(false);

            lockoutSecondsRemaining = LOCKOUT_SECONDS;
            UpdateLockTimerText();
            tbLockTimer.Visibility = Visibility.Visible;

            lockoutTimer.Start();
        }

        /// <summary>
        /// Останавливает блокировку формы входа и повторно включает поля ввода.
        /// </summary>
        private void StopLockout()
        {
            lockoutTimer.Stop();
            tbLockTimer.Visibility = Visibility.Collapsed;
            SetInputsEnabled(true);
        }

        /// <summary>
        /// Обновляет текст таймера, отображающий оставшееся время до разблокировки.
        /// </summary>
        private void UpdateLockTimerText()
        {
            tbLockTimer.Text = $"До разблокировки: {lockoutSecondsRemaining} с.";
        }

        /// <summary>
        /// Включает или отключает элементы ввода на форме авторизации.
        /// </summary>
        /// <param name="enabled">Флаг доступности элементов: true — включить, false — отключить.</param>
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

                var now = DateTime.Now;
                if (!Session.IsWithinWorkHours(now))
                {
                    MessageBox.Show("Доступ к системе запрещён: сейчас не рабочее время (10:00 - 19:00).", "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

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
                    var staff = db.AgencyStaff.FirstOrDefault(s => s.auth_id == auth.id);
                    UserSession userSession;
                    if (applicant != null)
                    {
                        userSession = new UserSession
                        {
                            Id = applicant.id,
                            LastName = applicant.last_name ?? string.Empty,
                            FirstName = applicant.first_name ?? string.Empty,
                            MiddleName = applicant.middle_name ?? string.Empty,
                            Role = role.Title ?? string.Empty
                        };
                    }
                    else if (staff != null)
                    {
                        userSession = new UserSession
                        {
                            Id = staff.id,
                            LastName = staff.full_name ?? string.Empty,
                            FirstName = string.Empty,
                            MiddleName = string.Empty,
                            Role = role.Title ?? string.Empty
                        };
                    }
                    else
                    {
                        MessageBox.Show("Пользователь не найден ни среди соискателей, ни среди сотрудников агентства.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
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

                    LoadPage(role.Title);
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

        /// <summary>
        /// Проверяет, нужно ли запускать блокировку после очередной неудачной попытки входа.
        /// </summary>
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

                NavigationService.Navigate(new Pages.Guests());
            }
            catch (Exception exception)
            {
                Console.WriteLine("btnAuthGuest error:", exception);
                throw;
            }
        }

        /// <summary>
        /// Генерирует новый текст капчи и отображает блок капчи на форме.
        /// </summary>
        private void GenerateCaptcha()
        {
            currentCaptcha = CaptchaGenerator.GenerateCaptchaText(6);
            tbCaptcha.Text = currentCaptcha;
            tbCaptcha.TextDecorations = TextDecorations.Strikethrough;
            ShowCaptcha();
        }

        /// <summary>
        /// Показывает элементы интерфейса, связанные с вводом капчи, и очищает поле ввода.
        /// </summary>
        private void ShowCaptcha()
        {
            CaptchaPanel.Visibility = Visibility.Visible;
            tbCaptcha.Visibility = Visibility.Visible;
            txtbCaptcha.Visibility = Visibility.Visible;
            txtbCaptcha.Text = string.Empty;
        }

        /// <summary>
        /// Скрывает элементы капчи и сбрасывает текущее значение капчи.
        /// </summary>
        private void HideCaptcha()
        {
            CaptchaPanel.Visibility = Visibility.Collapsed;
            tbCaptcha.Visibility = Visibility.Collapsed;
            txtbCaptcha.Visibility = Visibility.Collapsed;
            currentCaptcha = string.Empty;
            txtbCaptcha.Text = string.Empty;
        }

        /// <summary>
        /// Определяет, отображается ли в текущий момент блок капчи.
        /// </summary>
        /// <returns>true, если капча отображается; иначе false.</returns>
        private bool IsCaptchaVisible()
        {
            return CaptchaPanel.Visibility == Visibility.Visible;
        }

        /// <summary>
        /// Выполняет переход на страницу, соответствующую роли пользователя.
        /// </summary>
        /// <param name="_role">Название роли пользователя, определяющее целевую страницу.</param>
        private void LoadPage(string _role)
        {
            switch (_role)
            {
                case "Applicant":
                    NavigationService.Navigate(new Clients());
                    break;
                case "AgentStaff":
                    NavigationService.Navigate(new AgentStaff());
                    break;
                case "Admin":
                    NavigationService.Navigate(new AdminPanel());
                    break;
            }
        }
    }
}
