using MedicalApi.Data;
using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly MedicalDbContext _db;

    public AppointmentsController(MedicalDbContext db)
    {
        _db = db;
    }

    // GET /api/appointments?patientId=1&doctorName=Иванова&date=2026-11-10, все фильтры необязательные.
    // Записи отсортированы по времени, так удобнее смотреть расписание
    [HttpGet]
    public async Task<List<Appointment>> GetAll(int? patientId, string? doctorName, DateOnly? date)
    {
        IQueryable<Appointment> query = _db.Appointments;
        if (patientId != null)
        {
            query = query.Where(a => a.PatientId == patientId);
        }
        if (!string.IsNullOrWhiteSpace(doctorName))
        {
            query = query.Where(a => EF.Functions.ILike(a.DoctorName, $"%{doctorName.Trim()}%"));
        }
        if (date != null)
        {
            DateTime dayStart = date.Value.ToDateTime(TimeOnly.MinValue);
            DateTime dayEnd = dayStart.AddDays(1);
            query = query.Where(a => a.StartTime >= dayStart && a.StartTime < dayEnd);
        }
        return await query.OrderBy(a => a.StartTime).ToListAsync();
    }

    [HttpGet("{id}")]
    [ProducesResponseType<Appointment>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Appointment>> GetById(int id)
    {
        Appointment? appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
        {
            return NotFound();
        }
        return appointment;
    }

    [HttpPost]
    [ProducesResponseType<Appointment>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Appointment>> Create(Appointment appointment)
    {
        string? error = await Validate(appointment, 0);
        if (error != null)
        {
            return BadRequest(error);
        }
        // Номер выдаёт база, Id из тела запроса игнорируем
        appointment.Id = 0;
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
    }

    [HttpPut("{id}")]
    [ProducesResponseType<Appointment>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Appointment>> Update(int id, Appointment appointment)
    {
        Appointment? existing = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (existing == null)
        {
            return NotFound();
        }
        string? error = await Validate(appointment, id);
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
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        Appointment? appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
        {
            return NotFound();
        }
        _db.Appointments.Remove(appointment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // id: запись, которую редактируем (0 при создании), чтобы она не конфликтовала сама с собой
    private async Task<string?> Validate(Appointment appointment, int id)
    {
        if (!await _db.Patients.AnyAsync(p => p.Id == appointment.PatientId))
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
            Spravka? spravka = await _db.Spravki.FirstOrDefaultAsync(s => s.Id == appointment.SpravkaId);
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
        if (await _db.Appointments.AnyAsync(a => a.Id != id && a.StartTime == appointment.StartTime && a.DoctorName.Trim().ToLower() == doctor))
        {
            return $"У врача {appointment.DoctorName} на {appointment.StartTime:dd.MM.yyyy HH:mm} уже есть запись, выберите другое время";
        }
        if (await _db.Appointments.AnyAsync(a => a.Id != id && a.StartTime == appointment.StartTime && a.PatientId == appointment.PatientId))
        {
            return $"Пациент уже записан на {appointment.StartTime:dd.MM.yyyy HH:mm} к другому врачу";
        }
        return null;
    }
}
