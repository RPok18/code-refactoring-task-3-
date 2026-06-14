using AutoServiceApp.Models;

namespace AutoServiceApp.Services;

public class MechanicService
{
    private readonly List<Mechanic> _mechanics;
    private readonly List<RepairOrder> _orders;
    private readonly Action _saveAll;

    public MechanicService(List<Mechanic> mechanics, List<RepairOrder> orders, Action saveAll)
    {
        _mechanics = mechanics;
        _orders = orders;
        _saveAll = saveAll;
    }

    public Mechanic AddMechanic(string name, string specialization, decimal hourRate)
    {
        var m = new Mechanic { Name = name, Specialization = specialization, HourRate = hourRate };
        _mechanics.Add(m);
        _saveAll();
        return m;
    }

    public void UpdateMechanic(Mechanic m, string name, string specialization, decimal hourRate)
    {
        m.Name = name;
        m.Specialization = specialization;
        m.HourRate = hourRate;
        _saveAll();
    }

    public void DeleteMechanic(Mechanic m)
    {
        _mechanics.Remove(m);
        foreach (var order in _orders.Where(o => o.AssignedMechanicId == m.Id))
        {
            order.AssignedMechanicId = "";
            order.AssignedMechanic = null;
        }
        _saveAll();
    }
}
