namespace MedicalApi.Models;

public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateOnly BirthDate { get; set; }
    public string Iin { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsMale { get; set; }
    public int Uchastok { get; set; }
    public bool HasInsurance { get; set; }

    public int GetAge()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        int age = today.Year - BirthDate.Year;
        if (BirthDate > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }
}
