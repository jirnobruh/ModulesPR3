using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HashPasswords;
using ModulesPR3.Services;
using ModulesPR5;
using ModulesPR5.Models;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace ModulesPR3.Pages
{
    public partial class AddEditStaffPage : Page
    {
        private AgencyStaff staff;
        private ModulesPR5.Models.Auth auth;

        public AddEditStaffPage(AgencyStaff currentStaff)
        {
            InitializeComponent();
            var db = Helper.GetContext();

            staff = currentStaff ?? new AgencyStaff();

            if (currentStaff != null)
            {
                txtFullName.Text = staff.full_name;
                txtPosition.Text = staff.position;
                txtEmail.Text = staff.email;
                txtPhone.Text = staff.phone;

                auth = db.Auth.FirstOrDefault(a => a.id == staff.auth_id);
                if (auth != null)
                {
                    txtLogin.Text = auth.login;
                }
            }
            else
            {
                auth = new ModulesPR5.Models.Auth();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var db = Helper.GetContext();

            staff.full_name = txtFullName.Text;
            staff.position = txtPosition.Text;
            staff.email = txtEmail.Text;
            staff.phone = txtPhone.Text;

            // Работа с Auth
            auth.login = txtLogin.Text;
            if (!string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                auth.password = Hash.HashPassword(txtPassword.Password);
            }

            // Валидация моделей
            var staffErrors = ModelValidator.Validate(staff);
            var authErrors = ModelValidator.Validate(auth);

            // Проверка уникальности логина
            if (db.Auth.Any(a => a.login == auth.login && a.id != auth.id))
            {
                authErrors.Add(new ValidationResult("Логин уже используется", new[] { "login" }));
            }

            // Если есть ошибки — выводим
            if (staffErrors.Any() || authErrors.Any())
            {
                string msg = string.Join("\n", 
                    staffErrors.Select(ex => ex.ErrorMessage)
                        .Concat(authErrors.Select(ex => ex.ErrorMessage)));

                MessageBox.Show(msg, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Если новый сотрудник
            if (staff.id == 0)
            {
                // Назначаем роль "AgentStaff"
                var staffRole = db.Roles.FirstOrDefault(r => r.Title == "AgentStaff");
                if (staffRole == null)
                {
                    MessageBox.Show("Роль AgentStaff не найдена в таблице Roles!");
                    return;
                }
                auth.role_id = staffRole.id;

                db.Auth.Add(auth);
                db.SaveChanges(); // сохраним, чтобы получить auth.id

                staff.auth_id = auth.id;
                db.AgencyStaff.Add(staff);
            }

            try
            {
                db.SaveChanges();
                MessageBox.Show("Сотрудник сохранён!");
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var db = Helper.GetContext();
            if (staff.id == 0) return;

            if (MessageBox.Show("Удалить сотрудника?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                db.AgencyStaff.Remove(staff);
                if (auth != null)
                    db.Auth.Remove(auth);

                db.SaveChanges();
                MessageBox.Show("Сотрудник удалён!");
                NavigationService.GoBack();
            }
        }
    }
}