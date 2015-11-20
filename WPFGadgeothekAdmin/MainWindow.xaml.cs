using ch.hsr.wpf.gadgeothek.service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ch.hsr.wpf.gadgeothek.domain;
using WPFGadgeothekAdmin.viewmodel;

namespace WPFGadgeothekAdmin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LibraryAdminService _libraryAdminService;
        public List<Gadget> Gadgets { get; set; }
        public List<Customer> Customers { get; set; }
        public ObservableCollection<Gadget> AvailableGadgets { get; set; }
        public List<Loan> Loans { get; set; }
        public List<Reservation> Reservations { get; set; }
        public ObservableCollection<GadgetViewModel> GadgetViewModels { get; set; }
        public ObservableCollection<CustomerViewModel> CustomerViewModels { get; set; }
        public String ServiceUrl { get; set; }
        private bool connected = false;

        public MainWindow()
        {
            GadgetViewModels = new ObservableCollection<GadgetViewModel>();
            CustomerViewModels = new ObservableCollection<CustomerViewModel>();
            AvailableGadgets = new ObservableCollection<Gadget>();
            DataContext = this;
            InitializeComponent();
            Connect();
        }

        public void Connect()
        {
            if (connected) return;
            try
            {
                ServiceUrl = "http://localhost:8080";
                _libraryAdminService = new LibraryAdminService(ServiceUrl);
                connected = true;
                LoadData();
                RefreshComponents();
                StatusBarInfo.Content = $"Connected to {ServiceUrl}";
                StatusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x07, 0x78, 0xB5));
                
            }
            catch (Exception e)
            {
                connected = false;
                StatusBarInfo.Content = $"Can't connect to {ServiceUrl} - Click here to reconnect";
                StatusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x62, 0x1F));

            }
        }

        private void RefreshComponents()
        {
            MainGadgetView.UpdateLayout();
        }

        public void LoadData()
        {
            Gadgets = _libraryAdminService.GetAllGadgets();
            Customers = _libraryAdminService.GetAllCustomers();
            Loans = _libraryAdminService.GetAllLoans();
            Reservations = _libraryAdminService.GetAllReservations();

            Gadgets.ForEach(g =>
            {
                List<Loan> loans = Loans.FindAll(l => l.GadgetId == g.InventoryNumber);
                GadgetViewModels.Add(new GadgetViewModel() { Gadget = g, Loans = loans });
            });

            Customers.ForEach(c => {
                List<Loan> loans = Loans.FindAll(l => l.CustomerId == c.Studentnumber);
                CustomerViewModels.Add(new CustomerViewModel() { Customer = c, Loans = loans });
            });

        }

        void ShowHideDetails(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility =
                      row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
        }

        private void StatusBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Connect();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CustomerViewModel selectedCustomer = (CustomerViewModel) CustomerViewModelList.SelectedItem;
            LoanList.ItemsSource = Loans.FindAll(l => l.CustomerId == selectedCustomer.Customer.Studentnumber);
            LoanList.UpdateLayout();

            ReservationList.ItemsSource = Reservations.FindAll(r => r.CustomerId == selectedCustomer.Customer.Studentnumber);
            ReservationList.UpdateLayout();

            AvailableGadgets.Clear();
            Gadgets.ForEach(g => AvailableGadgets.Add(g));
            
            Loans.FindAll(l => l.IsLent).ForEach(g => AvailableGadgets.Remove(g.Gadget));
        }
    }
}
