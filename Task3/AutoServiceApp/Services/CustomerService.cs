using AutoServiceApp.Models;

namespace AutoServiceApp.Services;

public class CustomerService
{
    private readonly List<Customer> _customers;
    private readonly List<Car> _cars;
    private readonly List<RepairOrder> _orders;
    private readonly Action _saveAll;
    private readonly Action _relinkAll;

    public CustomerService(
        List<Customer> customers,
        List<Car> cars,
        List<RepairOrder> orders,
        Action saveAll,
        Action relinkAll)
    {
        _customers = customers;
        _cars = cars;
        _orders = orders;
        _saveAll = saveAll;
        _relinkAll = relinkAll;
    }

    public Customer AddCustomer(string name, string phone, string email, string address)
    {
        var c = new Customer { Name = name, Phone = phone, Email = email, Address = address };
        _customers.Add(c);
        _saveAll();
        return c;
    }

    public void UpdateCustomer(Customer customer, string name, string phone, string email, string address)
    {
        customer.Name = name;
        customer.Phone = phone;
        customer.Email = email;
        customer.Address = address;
        foreach (var order in _orders.Where(x => x.CustomerId == customer.Id))
            order.Customer = customer;
        _saveAll();
    }

    public void DeleteCustomer(Customer customer)
    {
        _customers.Remove(customer);
        foreach (var car in _cars.Where(x => x.CustomerId == customer.Id).ToList())
            _cars.Remove(car);
        foreach (var order in _orders.Where(x => x.CustomerId == customer.Id).ToList())
            _orders.Remove(order);
        _saveAll();
    }

    public Car AddCar(Customer? owner, string make, string model, int year, string vin, int mileage, string licensePlate)
    {
        var car = new Car
        {
            CustomerId = owner?.Id ?? "",
            Owner = owner,
            Make = make,
            Model = model,
            Year = year,
            Vin = vin,
            Mileage = mileage,
            LicensePlate = licensePlate
        };
        _cars.Add(car);
        if (owner != null)
            owner.Cars.Add(car);
        _saveAll();
        return car;
    }

    public void UpdateCar(Car car, Customer? owner, string make, string model, int year, string vin, int mileage, string licensePlate)
    {
        car.CustomerId = owner?.Id ?? "";
        car.Owner = owner;
        car.Make = make;
        car.Model = model;
        car.Year = year;
        car.Vin = vin;
        car.Mileage = mileage;
        car.LicensePlate = licensePlate;
        _relinkAll();
        _saveAll();
    }

    public void DeleteCar(Car car)
    {
        _cars.Remove(car);
        foreach (var c in _customers)
            c.Cars.RemoveAll(x => x.Id == car.Id);
        foreach (var order in _orders.Where(x => x.CarId == car.Id).ToList())
            _orders.Remove(order);
        _saveAll();
    }
}
