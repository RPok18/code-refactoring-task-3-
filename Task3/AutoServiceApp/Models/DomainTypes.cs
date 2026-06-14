namespace AutoServiceApp.Models;

public enum OrderStatus
{
    New,
    Diagnostics,
    InProgress,
    WaitingForParts,
    Ready,
    Released
}

public enum PaymentMethod
{
    Cash,
    Card,
    Transfer
}

public enum NotificationType
{
    Sms,
    Email,
    Both
}