using System.Text;
using AutoServiceApp.Helpers;
using AutoServiceApp.Models;

namespace AutoServiceApp.Services;

public class OrderService
{
    private readonly List<RepairOrder> _orders;
    private readonly List<Part> _parts;
    private readonly List<Mechanic> _mechanics;
    private readonly OrderStatusHelper _statusHelper;
    private readonly NotificationFacade _notificationFacade;
    private readonly ReportService _reportService;
    private readonly Action _saveAll;

    public OrderService(
        List<RepairOrder> orders,
        List<Part> parts,
        List<Mechanic> mechanics,
        OrderStatusHelper statusHelper,
        NotificationFacade notificationFacade,
        ReportService reportService,
        Action saveAll)
    {
        _orders = orders;
        _parts = parts;
        _mechanics = mechanics;
        _statusHelper = statusHelper;
        _notificationFacade = notificationFacade;
        _reportService = reportService;
        _saveAll = saveAll;
    }

    public RepairOrder CreateOrder(Customer? customer, Car? car, string description, Mechanic? mechanic, OrderStatus status, PaymentMethod paymentMethod)
    {
        var order = new RepairOrder
        {
            OrderNumber = "RO-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
            CustomerId = customer?.Id ?? string.Empty,
            CarId = car?.Id ?? string.Empty,
            Customer = customer,
            Car = car,
            ProblemDescription = description,
            AssignedMechanicId = mechanic?.Id ?? string.Empty,
            AssignedMechanic = mechanic,
            Status = status,
            PaymentMethod = paymentMethod,
            Cost = 0
        };

        order.StatusHistory.Add($"{DateTime.Now:g}: order created with status {status}");
        _orders.Add(order);

        if (mechanic != null)
            mechanic.AssignedOrderIds.Add(order.Id);

        _saveAll();
        return order;
    }

    public void UpdateOrder(RepairOrder order, Customer? customer, Car? car, string description, Mechanic? mechanic, OrderStatus status, decimal cost, PaymentMethod paymentMethod)
    {
        order.CustomerId = customer?.Id ?? string.Empty;
        order.CarId = car?.Id ?? string.Empty;
        order.Customer = customer;
        order.Car = car;
        order.ProblemDescription = description;
        order.AssignedMechanicId = mechanic?.Id ?? string.Empty;
        order.AssignedMechanic = mechanic;
        order.PaymentMethod = paymentMethod;
        order.Cost = cost;

        if (order.Status != status)
            ChangeOrderStatus(order, status, NotificationType.Both);

        _saveAll();
    }

    public void ChangeOrderStatus(RepairOrder order, OrderStatus newStatus, NotificationType notificationType)
    {
        _statusHelper.MarkStatus(order, newStatus);

        if (newStatus == OrderStatus.Ready)
            order.Cost = CalculateOrderCost(order, true, order.PaymentMethod);

        if (order.AssignedMechanic != null && !order.AssignedMechanic.AssignedOrderIds.Contains(order.Id))
            order.AssignedMechanic.AssignedOrderIds.Add(order.Id);

        NotifyAboutStatus(order, notificationType);
        _saveAll();
    }

    public void AddWorkToOrder(RepairOrder order, string name, double hours, decimal cost)
    {
        var work = new RepairWork { Name = name, Hours = hours, Cost = cost };
        order.Works.Add(work);
        order.Cost = CalculateOrderCost(order, false, order.PaymentMethod);
        _saveAll();
    }

    public bool UsePartForOrder(RepairOrder order, Part part, int qty)
    {
        if (part.Stock < qty)
            return false;

        part.Stock -= qty;
        for (var i = 0; i < qty; i++)
            order.UsedPartIds.Add(part.Id);

        order.Cost += part.Price * qty * 1.50m;
        order.StatusHistory.Add($"{DateTime.Now:g}: part used {part.Name} x{qty}");
        _saveAll();
        return true;
    }

    public decimal CalculateOrderCost(RepairOrder order, bool final, PaymentMethod paymentMethod)
    {
        var works = order.Works.Sum(x => x.Cost + (decimal)x.Hours * (order.AssignedMechanic?.HourRate ?? 0));
        var parts = order.UsedPartIds.Select(id => _parts.FirstOrDefault(p => p.Id == id)).Where(p => p != null).Sum(p => p!.Price * 1.20m);
        var result = works + parts;

        if (paymentMethod == PaymentMethod.Card)
            result += result * 0.05m;

        if (order.Customer != null && order.Customer.Cars.Count > 2)
            result -= result * 0.10m;

        if (final && order.Status == OrderStatus.Ready)
            result += 500;

        var tempDiscount = result > 10000 ? result * 0.15m : 0;
        return result - tempDiscount;
    }

    public string BuildOrderDetails(RepairOrder order)
    {
        var sb = new StringBuilder();
        sb.AppendLine(order.ToString());
        sb.AppendLine(order.ProblemDescription);
        sb.AppendLine("Works:");

        foreach (var work in order.Works)
            sb.AppendLine(" - " + work);

        sb.AppendLine("History:");
        foreach (var h in order.StatusHistory)
            sb.AppendLine(" - " + h);

        if (order.Customer?.Cars.Count > 0)
            sb.AppendLine("First car owner phone: " + order.Customer.Cars[0].Owner?.Phone);

        return sb.ToString();
    }

    public string BuildReports(DateTime from, DateTime to)
    {
        return _reportService.BuildRevenueReport(_orders, from, to) + "\n"
            + _reportService.BuildPopularWorks(_orders) + "\n\n"
            + _reportService.BuildMechanicsLoad(_mechanics, _orders) + "\n"
            + _reportService.BuildPartsStock(_parts);
    }

    public List<RepairOrder> GetOrdersForMechanic(Mechanic mechanic)
    {
        var result = new List<RepairOrder>();
        foreach (var id in mechanic.AssignedOrderIds)
        {
            var order = _orders.FirstOrDefault(x => x.Id == id);
            if (order != null)
                result.Add(order);
        }
        return result;
    }

    public void NotifyAboutStatus(RepairOrder order, NotificationType type)
    {
        var phone = order.Customer?.Phone ?? string.Empty;
        var email = order.Customer?.Email ?? string.Empty;
        var text = $"Order {order.OrderNumber}: new status {order.Status}";

        if (type == NotificationType.Sms)
            _notificationFacade.Notify("sms", phone, email, "Order status", text);
        else if (type == NotificationType.Email)
            _notificationFacade.Notify("email", phone, email, "Order status", text);
        else
            _notificationFacade.Notify("both", phone, email, "Order status", text);
    }
}
