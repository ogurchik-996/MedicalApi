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
    public DateOnly IssueDate { get; set; }
    public DateOnly ValidUntil { get; set; }
}
