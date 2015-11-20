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
        private LibraryAdminService libraryAdminService;
        public List<Gadget> Gadgets { get; set; }
        public List<Customer> Customers { get; set; }
        public List<Gadget> AvailableGadgets { get; set; }
        public List<Loan> Loans { get; set; }
        public ObservableCollection<GadgetViewModel> GadgetViewModels { get; set; }
        public String ServiceUrl { get; set; }
        private bool connected = false;

        public MainWindow()
        {
            GadgetViewModels = new ObservableCollection<GadgetViewModel>();
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
                libraryAdminService = new LibraryAdminService(ServiceUrl);
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
            Gadgets = libraryAdminService.GetAllGadgets();
            Customers = libraryAdminService.GetAllCustomers();
            Loans = libraryAdminService.GetAllLoans();
            
            Gadgets.ForEach(g =>
            {
                List<Loan> loans = Loans.FindAll(l => l.GadgetId == g.InventoryNumber);
                GadgetViewModels.Add(new GadgetViewModel() { Gadget = g, Loans = loans });
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

    }
}
