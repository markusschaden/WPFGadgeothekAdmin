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

        public List<GadgetViewModel> GadgetViewModels { get; set; }

        public String ServiceUrl { get; set; }

        public MainWindow()
        {
            ServiceUrl = "http://localhost:8080";
            libraryAdminService = new LibraryAdminService(ServiceUrl);
            Gadgets = libraryAdminService.GetAllGadgets();
            Customers = libraryAdminService.GetAllCustomers();
            Loans = libraryAdminService.GetAllLoans();
            GadgetViewModels = new List<GadgetViewModel>();
            Gadgets.ForEach( g =>
            {
                List<Loan> loans = Loans.FindAll(l => l.GadgetId == g.InventoryNumber);
                GadgetViewModels.Add(new GadgetViewModel() {Gadget = g, Loans = loans});
            });


            InitializeComponent();

            DataContext = this;
        }

        
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            GadgetView gadgetView = new GadgetView();
            //GadgetView.Show();
            
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
    }
}
