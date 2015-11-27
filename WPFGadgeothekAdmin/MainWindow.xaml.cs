using ch.hsr.wpf.gadgeothek.service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
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
using ch.hsr.wpf.gadgeothek;
using ch.hsr.wpf.gadgeothek.domain;
using ch.hsr.wpf.gadgeothek.websocket;
using MahApps.Metro.Controls;
using WPFGadgeothekAdmin.viewmodel;

namespace WPFGadgeothekAdmin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private LibraryAdminService _libraryAdminService;
        public List<Gadget> Gadgets { get; set; }
        public List<Customer> Customers { get; set; }
        public List<Loan> Loans { get; set; }
        public List<Reservation> Reservations { get; set; }


        public ObservableCollection<Gadget> AvailableGadgets { get; set; }
        public ObservableCollection<Gadget> ReservableGadgets { get; set; }
        public ObservableCollection<GadgetViewModel> GadgetViewModels { get; set; }
        public ObservableCollection<CustomerViewModel> CustomerViewModels { get; set; }

        public ObservableCollection<Reservation> ReservedGadgets { get; set; }
        public ObservableCollection<Loan> LoanedGadgets { get; set; }
       
        public int MAX_LOANS { get; set; }
        public int MAX_RESERVATIONS { get; set; }


        public Gadget SelectedAvailableGadget { get; set; }
        public Gadget SelectedReservationGadget { get; set; }

        public String ServiceUrl { get; set; }
        private bool _connected = false;
        //private CustomerViewModel _selectedCustomer;
        private String _selectedCustomerId;

        public MainWindow()
        {
            MAX_LOANS = 3;
            MAX_RESERVATIONS = 3;
            GadgetViewModels = new ObservableCollection<GadgetViewModel>();
            CustomerViewModels = new ObservableCollection<CustomerViewModel>();
            AvailableGadgets = new ObservableCollection<Gadget>();
            ReservableGadgets = new ObservableCollection<Gadget>();
            ReservedGadgets = new ObservableCollection<Reservation>();
            LoanedGadgets = new ObservableCollection<Loan>();


            DataContext = this;
            InitializeComponent();
            ShowLoadingScreen();
            //Connect();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        public void Connect()
        {
            if (_connected) return;
            try
            {
                ServiceUrl = "http://localhost:8080";
                _libraryAdminService = new LibraryAdminService(ServiceUrl);
                _connected = true;
                LoadData();
                RefreshComponents();
                StatusBarInfo.Content = $"Connected to {ServiceUrl}";
                StatusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x07, 0x78, 0xB5));

                HideLoadingScreen();

                var client = new WebSocketClient(ServiceUrl);
                client.NotificationReceived += (o, e) =>
                {
                    Debug.WriteLine("WebSocket::Notification: " + e.Notification.Target + " > " + e.Notification.Type);

                    // demonstrate how these updates could be further used
                    if (e.Notification.Target == typeof(Gadget).Name.ToLower())
                    {
                        var gadget = e.Notification.DataAs<Gadget>();                       
                        var oldGadget = Gadgets.Find(g => g.InventoryNumber == gadget.InventoryNumber);
                        Gadgets.Remove(oldGadget);
                        Gadgets.Add(gadget);

                    } else if (e.Notification.Target == typeof(Reservation).Name.ToLower())
                    {
                        var reservation = e.Notification.DataAs<Reservation>();
                        var oldReservation = Reservations.Find(r => r.Id == reservation.Id);
                        Reservations.Remove(oldReservation);

                        reservation.Customer = Customers.Find(c => c.Studentnumber == reservation.CustomerId);
                        reservation.Gadget = Gadgets.Find(c => c.InventoryNumber == reservation.GadgetId);
                        Reservations.Add(reservation);

                    } else if (e.Notification.Target == typeof(Loan).Name.ToLower())
                    {
                        var loan = e.Notification.DataAs<Loan>();
                        var oldLoans = Loans.Find(l => l.Id == loan.Id);
                        Loans.Remove(oldLoans);

                        loan.Customer = Customers.Find(c => c.Studentnumber == loan.CustomerId);
                        loan.Gadget = Gadgets.Find(c => c.InventoryNumber == loan.GadgetId);
                        Loans.Add(loan);
                    }

                    LoadModels();
                    UpdateView();

                    HideLoadingScreen();
                };

                // spawn a new background thread in which the websocket client listens to notifications from the server
                try
                {
                    var bgTask = client.ListenAsync();
                }
                catch (WebSocketException e)
                {
                    ShowErrorScreen();
                }
            }
            catch (Exception e)
            {
                _connected = false;
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

            LoadModels();
        }

        public void LoadModels()
        {
            GadgetViewModels.Clear();
            Gadgets.ForEach(g =>
            {
                List<Loan> loans = Loans.FindAll(l => l.GadgetId == g.InventoryNumber);
                GadgetViewModels.Add(new GadgetViewModel() { Gadget = g, Loans = loans });
            });

            CustomerViewModels.Clear();
            Customers.ForEach(c => {
                List<Loan> loans = Loans.FindAll(l => l.CustomerId == c.Studentnumber);
                CustomerViewModels.Add(new CustomerViewModel() { Customer = c, Loans = loans });
            });

        }
      
        private void StatusBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Connect();
        }

        private void CustomerViewModelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CustomerViewModel selectedCustomer = (CustomerViewModel) CustomerViewModelList.SelectedItem;
            if (selectedCustomer == null) return; 
            _selectedCustomerId = selectedCustomer.Customer.Studentnumber;

            UpdateView();
            
        }

        private void UpdateView()
        {
            if (_selectedCustomerId == null)
            {
                Debug.Print("CustomerID null");
                return;
            }

            //LoanedGadgets = new ObservableCollection<Loan>(Loans.FindAll(l => l.CustomerId == _selectedCustomer.Customer.Studentnumber && l.WasReturned == false).ToList());
            LoanList.ItemsSource =
                Loans.FindAll(l => l.CustomerId == _selectedCustomerId && l.WasReturned == false);
            LoanList.UpdateLayout();

            //ReservedGadgets = new ObservableCollection<Reservation>(Reservations.FindAll(r => r.CustomerId == _selectedCustomer.Customer.Studentnumber && r.Finished == false).ToList());
            ReservationList.ItemsSource =
                Reservations.FindAll(
                    r => r.CustomerId == _selectedCustomerId && r.Finished == false);
            ReservationList.UpdateLayout();

            AvailableGadgets.Clear();
            Gadgets.ForEach(g => AvailableGadgets.Add(g));

            Loans.FindAll(l => l.IsLent).ForEach(g => AvailableGadgets.Remove(g.Gadget));

            ReservableGadgets.Clear();
            Gadgets.ForEach(g => ReservableGadgets.Add(g));
            AvailableGadgets.ToList().ForEach(g => ReservableGadgets.Remove(g));
            Loans.FindAll(l => l.CustomerId == _selectedCustomerId && l.WasReturned == false).ForEach(l => ReservableGadgets.Remove(l.Gadget));
            Reservations.FindAll(r => r.CustomerId == _selectedCustomerId && r.Finished == false).ForEach(r => ReservableGadgets.Remove(r.Gadget));

        }

        private void AddReservation(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomerId != null && SelectedReservationGadget != null)
            {
                ShowLoadingScreen();
                _libraryAdminService.AddReservation(new Reservation()
                {
                    Id = GUIDGenerator.getGUID().ToString(),
                    CustomerId = _selectedCustomerId,
                    GadgetId = SelectedReservationGadget.InventoryNumber,
                    ReservationDate = DateTime.Now
                });
            }
            
        }

        private void AddLoan(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomerId != null && SelectedAvailableGadget != null)
            {
                ShowLoadingScreen();

                _libraryAdminService.AddLoan(new Loan()
                {
                    Id = GUIDGenerator.getGUID().ToString(),
                    CustomerId = _selectedCustomerId,
                    GadgetId = SelectedAvailableGadget.InventoryNumber,
                    PickupDate = DateTime.Now
                });
            }
            
        }

        private void TakingBackButton_Click(object sender, RoutedEventArgs e)
        {
            Loan selectedLoan = LoanList.SelectedItem as Loan;
            if (selectedLoan == null) return;

            ShowLoadingScreen();
            selectedLoan.ReturnDate = DateTime.Now;
            _libraryAdminService.UpdateLoan(selectedLoan);

        }

        private void ShowLoadingScreen()
        {
            Overlay.Visibility = Visibility.Visible;
        }

        private void ShowErrorScreen()
        {
            Overlay.Visibility = Visibility.Visible;
            OverlayError.Text = "Lost Connection";
        }

        private void HideLoadingScreen()
        {
            Overlay.Visibility = Visibility.Collapsed;
        }
    }
}
