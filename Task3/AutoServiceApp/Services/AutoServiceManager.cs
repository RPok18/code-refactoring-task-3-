using AutoServiceApp.Helpers;
using AutoServiceApp.Models;
using AutoServiceApp.Storage;

namespace AutoServiceApp.Services;

public class AutoServiceManager
{
    public List<Customer> Customers { get; } = new();
    public List<Car> Cars { get; } = new();
    public List<RepairOrder> Orders { get; } = new();
    public List<Part> Parts { get; } = new();
    public List<Mechanic> Mechanics { get; } = new();
    public List<string> Notifications { get; } = new();

    public JsonFileStore<Customer> CustomerStore { get; set; } = new();
    public JsonFileStore<Car> CarStore { get; set; } = new();
    public JsonFileStore<RepairOrder> OrderStore { get; set; } = new();
    public JsonFileStore<Part> PartStore { get; set; } = new();
    public JsonFileStore<Mechanic> MechanicStore { get; set; } = new();

    public SmsNotifier SmsNotifier { get; set; } = new();
    public EmailSender EmailSender { get; set; } = new();
    public NotificationFacade NotificationFacade { get; }
    public ReportService ReportService { get; set; } = new();
    public OrderStatusHelper StatusHelper { get; set; } = new();

    public CustomerService CustomerService { get; }
    public MechanicService MechanicService { get; }
    public PartService PartService { get; }
    public OrderService OrderService { get; }

    public AutoServiceManager()
    {
        NotificationFacade = new NotificationFacade(SmsNotifier, EmailSender);
        CustomerService = new CustomerService(Customers, Cars, Orders, SaveAll, RelinkEverything);
        MechanicService = new MechanicService(Mechanics, Orders, SaveAll);
        PartService = new PartService(Parts, SaveAll);
        OrderService = new OrderService(Orders, Parts, Mechanics, StatusHelper, NotificationFacade, ReportService, SaveAll);
    }

    public void Load()
    {
        LoadList(CustomerStore, "customers.json", Customers);
        LoadList(CarStore, "cars.json", Cars);
        LoadList(OrderStore, "orders.json", Orders);
        LoadList(PartStore, "parts.json", Parts);
        LoadList(MechanicStore, "mechanics.json", Mechanics);
        RelinkEverything();

        if (Customers.Count == 0 && Cars.Count == 0 && Mechanics.Count == 0)
            Seed();
    }

    public void SaveAll()
    {
        CustomerStore.Save("customers.json", Customers);
        CarStore.Save("cars.json", Cars);
        OrderStore.Save("orders.json", Orders);
        PartStore.Save("parts.json", Parts);
        MechanicStore.Save("mechanics.json", Mechanics);
    }

    public void RelinkEverything()
    {
        foreach (var customer in Customers)
            customer.Cars = Cars.Where(x => x.CustomerId == customer.Id).ToList();

        foreach (var car in Cars)
            car.Owner = Customers.FirstOrDefault(x => x.Id == car.CustomerId);

        foreach (var order in Orders)
        {
            order.Customer = Customers.FirstOrDefault(x => x.Id == order.CustomerId);
            order.Car = Cars.FirstOrDefault(x => x.Id == order.CarId);
            order.AssignedMechanic = Mechanics.FirstOrDefault(x => x.Id == order.AssignedMechanicId);
        }

        foreach (var mechanic in Mechanics)
            mechanic.AssignedOrderIds = Orders.Where(x => x.AssignedMechanicId == mechanic.Id).Select(x => x.Id).ToList();
    }

    private void Seed()
    {
        var c1 = CustomerService.AddCustomer("John Parker", "+1 555 100-20-30", "john@example.com", "12 Market Street");
        var c2 = CustomerService.AddCustomer("Anna Stone", "+1 555 555-44-33", "anna@example.com", "45 Lake Avenue");
        var car1 = CustomerService.AddCar(c1, "Toyota", "Camry", 2018, "JTNB11HK303000001", 87000, "ABC123");
        CustomerService.AddCar(c2, "Kia", "Rio", 2021, "Z94CB41ABMR000002", 43000, "MOR777");
        var m1 = MechanicService.AddMechanic("Sam Miller", "engine", 1200);
        MechanicService.AddMechanic("Owen Lane", "electrical", 1500);
        PartService.AddPart("Oil filter", "OF-100", 650, 12);
        PartService.AddPart("Brake pads", "BR-500", 3200, 5);
        var order = OrderService.CreateOrder(c1, car1, "Knock on startup, diagnostics required", m1, OrderStatus.Diagnostics, PaymentMethod.Card);
        OrderService.AddWorkToOrder(order, "Computer diagnostics", 1.5, 2500);
        SaveAll();
    }

    private static void LoadList<T>(JsonFileStore<T> store, string fileName, List<T> destination)
    {
        destination.Clear();
        destination.AddRange(store.Load(fileName));
    }
}
