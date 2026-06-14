using AutoServiceApp.Models;

namespace AutoServiceApp.Services;

public class PartService
{
    private readonly List<Part> _parts;
    private readonly Action _saveAll;

    public PartService(List<Part> parts, Action saveAll)
    {
        _parts = parts;
        _saveAll = saveAll;
    }

    public Part AddPart(string name, string article, decimal price, int stock)
    {
        var p = new Part { Name = name, Article = article, Price = price, Stock = stock };
        _parts.Add(p);
        _saveAll();
        return p;
    }

    public void UpdatePart(Part part, string name, string article, decimal price, int stock)
    {
        part.Name = name;
        part.Article = article;
        part.Price = price;
        part.Stock = stock;
        _saveAll();
    }

    public void DeletePart(Part p)
    {
        _parts.Remove(p);
        _saveAll();
    }
}
