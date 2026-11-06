namespace MedicalApi.Models;

public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime BirthDate { get; set; }
    public string Iin { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsMale { get; set; }
    public int Uchastok { get; set; }
    public bool HasInsurance { get; set; }

    public int GetAge()
    {
        int age = DateTime.Today.Year - BirthDate.Year;
        if (BirthDate.Date > DateTime.Today.AddYears(-age))
        {
            age--;
        }
        return age;
    }
}
