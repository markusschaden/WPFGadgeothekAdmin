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
using ch.hsr.wpf.gadgeothek;
using ch.hsr.wpf.gadgeothek.domain;
using ch.hsr.wpf.gadgeothek.websocket;
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
        private CustomerViewModel _selectedCustomer;

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
                };

                // spawn a new background thread in which the websocket client listens to notifications from the server
                var bgTask = client.ListenAsync();
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
            CustomerViewModels.ToList().RemoveAll(t => true);
            Customers.ForEach(c => {
                List<Loan> loans = Loans.FindAll(l => l.CustomerId == c.Studentnumber);
                CustomerViewModels.Add(new CustomerViewModel() { Customer = c, Loans = loans });
            });

        }
      
        private void StatusBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Connect();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCustomer = (CustomerViewModel) CustomerViewModelList.SelectedItem;

            UpdateView();
            
            Storyboard fading = this.Resources["ColumnAnimation"] as Storyboard;
            if (fading != null)
            {
                //fading.Begin();   
            }
        }

        private void UpdateView()
        {
            if (_selectedCustomer == null) return;

            //LoanedGadgets = new ObservableCollection<Loan>(Loans.FindAll(l => l.CustomerId == _selectedCustomer.Customer.Studentnumber && l.WasReturned == false).ToList());
            LoanList.ItemsSource =
                Loans.FindAll(l => l.CustomerId == _selectedCustomer.Customer.Studentnumber && l.WasReturned == false);
            LoanList.UpdateLayout();

            //ReservedGadgets = new ObservableCollection<Reservation>(Reservations.FindAll(r => r.CustomerId == _selectedCustomer.Customer.Studentnumber && r.Finished == false).ToList());
            ReservationList.ItemsSource =
                Reservations.FindAll(
                    r => r.CustomerId == _selectedCustomer.Customer.Studentnumber && r.Finished == false);
            ReservationList.UpdateLayout();

            AvailableGadgets.Clear();
            Gadgets.ForEach(g => AvailableGadgets.Add(g));

            Loans.FindAll(l => l.IsLent).ForEach(g => AvailableGadgets.Remove(g.Gadget));

            ReservableGadgets.Clear();
            Gadgets.ForEach(g => ReservableGadgets.Add(g));
            AvailableGadgets.ToList().ForEach(g => ReservableGadgets.Remove(g));
            Loans.FindAll(l => l.CustomerId == _selectedCustomer.Customer.Studentnumber && l.WasReturned == false).ForEach(l => ReservableGadgets.Remove(l.Gadget));
            Reservations.FindAll(r => r.CustomerId == _selectedCustomer.Customer.Studentnumber && r.Finished == false).ForEach(r => ReservableGadgets.Remove(r.Gadget));

        }

        private void AddReservation(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomer != null && SelectedReservationGadget != null)
            {
                _libraryAdminService.AddReservation(new Reservation()
                {
                    Id = GUIDGenerator.getGUID().ToString(),
                    CustomerId = _selectedCustomer.Customer.Studentnumber,
                    GadgetId = SelectedReservationGadget.InventoryNumber,
                    ReservationDate = DateTime.Now
                });
            }
        }

        private void AddLoan(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomer != null && SelectedAvailableGadget != null)
            {
                _libraryAdminService.AddLoan(new Loan()
                {
                    Id = GUIDGenerator.getGUID().ToString(),
                    CustomerId = _selectedCustomer.Customer.Studentnumber,
                    GadgetId = SelectedAvailableGadget.InventoryNumber,
                    PickupDate = DateTime.Now
                });
            }
        }
    }
}
