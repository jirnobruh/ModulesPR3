using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ModulesPR5;
using ModulesPR5.Models; // твой EF контекст

namespace ModulesPR3.Pages
{
    public partial class AdminPanel : Page
    {
        public AdminPanel()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                LoadStaff();
                txtSearch.TextChanged += txtSearch_TextChanged; // подписка только после загрузки
            };
        }

        private void LoadStaff()
        {
            var db = Helper.GetContext();
            var staffList = db.AgencyStaff.ToList();
            lvStaff.ItemsSource = staffList;

            // Заполняем фильтр должностей
            cmbFilter.ItemsSource = staffList.Select(s => s.position).Distinct().ToList();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var db = Helper.GetContext();
            if (db == null || db.AgencyStaff == null) return;
            if (lvStaff == null) return; // защита

            var search = txtSearch.Text?.ToLower() ?? string.Empty;
            lvStaff.ItemsSource = db.AgencyStaff
                .Where(s => s.full_name.ToLower().Contains(search))
                .ToList();

        }

        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var db = Helper.GetContext();
            var selected = cmbFilter.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selected))
                lvStaff.ItemsSource = db.AgencyStaff.Where(s => s.position == selected).ToList();
            else
                LoadStaff();
        }

        private void lvStaff_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lvStaff.SelectedItem is AgencyStaff staff)
            {
                NavigationService.Navigate(new AddEditStaffPage(staff));
            }
        }

        private void btnAddStaff_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddEditStaffPage(null));
        }
    }
}