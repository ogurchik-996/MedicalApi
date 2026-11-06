using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedicalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    public static List<Appointment> Appointments = new List<Appointment>();

    // Счётчик только растёт: Id удалённых записей не достаются новым
    private static int _nextId = 1;

    // GET /api/appointments?patientId=1&doctorName=Иванова&date=2026-11-10, все фильтры необязательные.
    // Записи отсортированы по времени, так удобнее смотреть расписание
    [HttpGet]
    public List<Appointment> GetAll(int? patientId, string? doctorName, DateTime? date)
    {
        IEnumerable<Appointment> result = Appointments;
        if (patientId != null)
        {
            result = result.Where(a => a.PatientId == patientId);
        }
        if (!string.IsNullOrWhiteSpace(doctorName))
        {
            result = result.Where(a => a.DoctorName.ToLower().Contains(doctorName.Trim().ToLower()));
        }
        if (date != null)
        {
            result = result.Where(a => a.StartTime.Date == date.Value.Date);
        }
        return result.OrderBy(a => a.StartTime).ToList();
    }

    [HttpGet("{id}")]
    [ProducesResponseType<Appointment>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Appointment> GetById(int id)
    {
        Appointment? appointment = Appointments.FirstOrDefault(a => a.Id == id);
        if (appointment == null)
        {
            return NotFound();
        }
        return appointment;
    }

    [HttpPost]
    [ProducesResponseType<Appointment>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public ActionResult<Appointment> Create(Appointment appointment)
    {
        string? error = Validate(appointment, 0);
        if (error != null)
        {
            return BadRequest(error);
        }
        appointment.Id = _nextId++;
        Appointments.Add(appointment);
        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
    }

    [HttpPut("{id}")]
    [ProducesResponseType<Appointment>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Appointment> Update(int id, Appointment appointment)
    {
        Appointment? existing = Appointments.FirstOrDefault(a => a.Id == id);
        if (existing == null)
        {
            return NotFound();
        }
        string? error = Validate(appointment, id);
        if (error != null)
        {
            return BadRequest(error);
        }
        existing.PatientId = appointment.PatientId;
        existing.DoctorName = appointment.DoctorName;
        existing.Specialty = appointment.Specialty;
        existing.StartTime = appointment.StartTime;
        existing.Cabinet = appointment.Cabinet;
        existing.Complaint = appointment.Complaint;
        existing.IsCompleted = appointment.IsCompleted;
        existing.SpravkaId = appointment.SpravkaId;
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Delete(int id)
    {
        Appointment? appointment = Appointments.FirstOrDefault(a => a.Id == id);
        if (appointment == null)
        {
            return NotFound();
        }
        Appointments.Remove(appointment);
        return NoContent();
    }

    // id: запись, которую редактируем (0 при создании), чтобы она не конфликтовала сама с собой
    private string? Validate(Appointment appointment, int id)
    {
        Patient? patient = PatientsController.Patients.FirstOrDefault(p => p.Id == appointment.PatientId);
        if (patient == null)
        {
            return $"Пациент с номером {appointment.PatientId} не найден. Сначала зарегистрируйте пациента в картотеке";
        }
        if (string.IsNullOrWhiteSpace(appointment.DoctorName))
        {
            return "Укажите ФИО врача";
        }
        if (appointment.DoctorName.Length > 100)
        {
            return "ФИО врача должно быть не длиннее 100 символов";
        }
        if (string.IsNullOrWhiteSpace(appointment.Specialty))
        {
            return "Укажите специальность врача, например «терапевт»";
        }
        if (appointment.Specialty.Length > 50)
        {
            return "Специальность должна быть не длиннее 50 символов";
        }
        if (appointment.StartTime == default)
        {
            return "Укажите дату и время приёма в формате ГГГГ-ММ-ДДTЧЧ:ММ, например 2026-12-01T10:30";
        }
        if (appointment.StartTime.DayOfWeek == DayOfWeek.Sunday)
        {
            return "В воскресенье поликлиника не работает, выберите другой день";
        }
        if (appointment.StartTime.Hour < 8 || appointment.StartTime.Hour >= 20)
        {
            return "Приём ведётся с 08:00 до 20:00, выберите другое время";
        }
        if (appointment.StartTime.Minute % 15 != 0 || appointment.StartTime.Second != 0)
        {
            return "Приём начинается каждые 15 минут: 10:00, 10:15, 10:30, 10:45";
        }
        if (!appointment.IsCompleted && appointment.StartTime < DateTime.Now)
        {
            return "Нельзя записать на прошедшее время, выберите время в будущем";
        }
        if (appointment.IsCompleted && appointment.StartTime > DateTime.Now)
        {
            return "Приём ещё не начался, его нельзя отметить завершённым";
        }
        if (appointment.Cabinet < 1 || appointment.Cabinet > 999)
        {
            return "Номер кабинета должен быть от 1 до 999";
        }
        if (appointment.Complaint.Length > 500)
        {
            return "Жалобы должны быть не длиннее 500 символов";
        }
        if (appointment.SpravkaId != null)
        {
            Spravka? spravka = SpravkiController.Spravki.FirstOrDefault(s => s.Id == appointment.SpravkaId);
            if (spravka == null)
            {
                return $"Справка с номером {appointment.SpravkaId} не найдена";
            }
            if (spravka.PatientId != appointment.PatientId)
            {
                return $"Справка с номером {appointment.SpravkaId} выписана другому пациенту";
            }
        }
        string doctor = appointment.DoctorName.Trim().ToLower();
        if (Appointments.Any(a => a.Id != id && a.StartTime == appointment.StartTime && a.DoctorName.Trim().ToLower() == doctor))
        {
            return $"У врача {appointment.DoctorName} на {appointment.StartTime:dd.MM.yyyy HH:mm} уже есть запись, выберите другое время";
        }
        if (Appointments.Any(a => a.Id != id && a.StartTime == appointment.StartTime && a.PatientId == appointment.PatientId))
        {
            return $"Пациент уже записан на {appointment.StartTime:dd.MM.yyyy HH:mm} к другому врачу";
        }
        return null;
    }
}
