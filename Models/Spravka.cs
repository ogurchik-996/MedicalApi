namespace MedicalApi.Models;

public class Spravka
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int SpravkaTypeId { get; set; }
    // Название и цену сервер берёт из каталога видов справок
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    // Даты сервер ставит сам
    public DateTime IssueDate { get; set; }
    public DateTime ValidUntil { get; set; }
}
