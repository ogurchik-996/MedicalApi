namespace MedicalApi.Models;

public class Appointment
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string DoctorName { get; set; } = "";
    public string Specialty { get; set; } = "";
    public DateTime StartTime { get; set; }
    public int Cabinet { get; set; }
    public string Complaint { get; set; } = "";
    public bool IsCompleted { get; set; }
    // Справка, для которой нужен осмотр врача. Может не быть
    public int? SpravkaId { get; set; }
}
