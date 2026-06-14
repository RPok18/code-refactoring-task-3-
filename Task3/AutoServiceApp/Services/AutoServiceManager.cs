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

    public Customer AddCustomer(string name, string phone, string email, string address)
        => CustomerService.AddCustomer(name, phone, email, address);

    public void UpdateCustomer(Customer customer, string name, string phone, string email, string address)
        => CustomerService.UpdateCustomer(customer, name, phone, email, address);

    public void DeleteCustomer(Customer customer)
        => CustomerService.DeleteCustomer(customer);

    public Car AddCar(Customer? owner, string make, string model, int year, string vin, int mileage, string licensePlate)
        => CustomerService.AddCar(owner, make, model, year, vin, mileage, licensePlate);

    public void UpdateCar(Car car, Customer? owner, string make, string model, int year, string vin, int mileage, string licensePlate)
        => CustomerService.UpdateCar(car, owner, make, model, year, vin, mileage, licensePlate);

    public void DeleteCar(Car car)
        => CustomerService.DeleteCar(car);

    public Mechanic AddMechanic(string name, string specialization, decimal hourRate)
        => MechanicService.AddMechanic(name, specialization, hourRate);

    public void UpdateMechanic(Mechanic mechanic, string name, string specialization, decimal hourRate)
        => MechanicService.UpdateMechanic(mechanic, name, specialization, hourRate);

    public void DeleteMechanic(Mechanic mechanic)
        => MechanicService.DeleteMechanic(mechanic);

    public Part AddPart(string name, string article, decimal price, int stock)
        => PartService.AddPart(name, article, price, stock);

    public void UpdatePart(Part part, string name, string article, decimal price, int stock)
        => PartService.UpdatePart(part, name, article, price, stock);

    public void DeletePart(Part part)
        => PartService.DeletePart(part);

    public RepairOrder CreateOrder(Customer? customer, Car? car, string description, Mechanic? mechanic, OrderStatus status, PaymentMethod paymentMethod)
        => OrderService.CreateOrder(customer, car, description, mechanic, status, paymentMethod);

    public void UpdateOrder(RepairOrder order, Customer? customer, Car? car, string description, Mechanic? mechanic, OrderStatus status, decimal cost, PaymentMethod paymentMethod)
        => OrderService.UpdateOrder(order, customer, car, description, mechanic, status, cost, paymentMethod);

    public void ChangeOrderStatus(RepairOrder order, OrderStatus newStatus, NotificationType notificationType)
        => OrderService.ChangeOrderStatus(order, newStatus, notificationType);

    public void AddWorkToOrder(RepairOrder order, string name, double hours, decimal cost)
        => OrderService.AddWorkToOrder(order, name, hours, cost);

    public bool UsePartForOrder(RepairOrder order, Part part, int qty)
        => OrderService.UsePartForOrder(order, part, qty);

    public decimal CalculateOrderCost(RepairOrder order, bool final, PaymentMethod paymentMethod)
        => OrderService.CalculateOrderCost(order, final, paymentMethod);

    public string BuildOrderDetails(RepairOrder order)
        => OrderService.BuildOrderDetails(order);

    public string BuildReports(DateTime from, DateTime to)
        => OrderService.BuildReports(from, to);

    public List<RepairOrder> GetOrdersForMechanic(Mechanic mechanic)
        => OrderService.GetOrdersForMechanic(mechanic);

    public void NotifyAboutStatus(RepairOrder order, NotificationType type)
        => OrderService.NotifyAboutStatus(order, type);

    private void Seed()
    {
        var c1 = AddCustomer("John Parker", "+1 555 100-20-30", "john@example.com", "12 Market Street");
        var c2 = AddCustomer("Anna Stone", "+1 555 555-44-33", "anna@example.com", "45 Lake Avenue");
        var car1 = AddCar(c1, "Toyota", "Camry", 2018, "JTNB11HK303000001", 87000, "ABC123");
        AddCar(c2, "Kia", "Rio", 2021, "Z94CB41ABMR000002", 43000, "MOR777");
        var m1 = AddMechanic("Sam Miller", "engine", 1200);
        AddMechanic("Owen Lane", "electrical", 1500);
        AddPart("Oil filter", "OF-100", 650, 12);
        AddPart("Brake pads", "BR-500", 3200, 5);
        var order = CreateOrder(c1, car1, "Knock on startup, diagnostics required", m1, OrderStatus.Diagnostics, PaymentMethod.Card);
        AddWorkToOrder(order, "Computer diagnostics", 1.5, 2500);
        SaveAll();
    }

    private static void LoadList<T>(JsonFileStore<T> store, string fileName, List<T> destination)
    {
        destination.Clear();
        destination.AddRange(store.Load(fileName));
    }
}
