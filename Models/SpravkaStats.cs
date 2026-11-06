namespace MedicalApi.Models;

public class SpravkaStats
{
    public int TotalCount { get; set; }
    public decimal TotalSum { get; set; }
    public List<SpravkaTypeStats> ByType { get; set; } = new List<SpravkaTypeStats>();
}

public class SpravkaTypeStats
{
    public int SpravkaTypeId { get; set; }
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public decimal Sum { get; set; }
}
