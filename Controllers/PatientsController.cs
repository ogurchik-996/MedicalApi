using MedicalApi.Data;
using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly MedicalDbContext _db;

    public PatientsController(MedicalDbContext db)
    {
        _db = db;
    }

    // GET /api/patients?name=ай, поиск по части ФИО без учёта регистра, name необязательный
    [HttpGet]
    public async Task<List<Patient>> GetAll(string? name)
    {
        IQueryable<Patient> query = _db.Patients;
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{name.Trim()}%"));
        }
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    [HttpGet("{id}")]
    [ProducesResponseType<Patient>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Patient>> GetById(int id)
    {
        Patient? patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }
        return patient;
    }

    [HttpPost]
    [ProducesResponseType<Patient>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Patient>> Create(Patient patient)
    {
        string? error = await Validate(patient, 0);
        if (error != null)
        {
            return BadRequest(error);
        }
        // Номер выдаёт база. Id из тела запроса игнорируем, иначе можно занять чужой или будущий номер
        patient.Id = 0;
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpPut("{id}")]
    [ProducesResponseType<Patient>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Patient>> Update(int id, Patient patient)
    {
        Patient? existing = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (existing == null)
        {
            return NotFound();
        }
        string? error = await Validate(patient, id);
        if (error != null)
        {
            return BadRequest(error);
        }
        existing.Name = patient.Name;
        existing.BirthDate = patient.BirthDate;
        existing.Iin = patient.Iin;
        existing.Phone = patient.Phone;
        existing.Address = patient.Address;
        existing.IsMale = patient.IsMale;
        existing.Uchastok = patient.Uchastok;
        existing.HasInsurance = patient.HasInsurance;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        Patient? patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }
        // Справки это медицинские документы, их нельзя терять вместе с пациентом.
        // База тоже не даст удалить (ON DELETE RESTRICT), но тогда был бы 500 вместо понятного ответа
        int spravki = await _db.Spravki.CountAsync(s => s.PatientId == id);
        if (spravki > 0)
        {
            return BadRequest($"Нельзя удалить пациента {patient.Name}: у него выписано справок: {spravki}. Справки это медицинские документы");
        }
        // Записи на приём удаляются вместе с пациентом (ON DELETE CASCADE)
        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/spravki")]
    [ProducesResponseType<List<Spravka>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<Spravka>>> GetSpravki(int id)
    {
        if (!await _db.Patients.AnyAsync(p => p.Id == id))
        {
            return NotFound();
        }
        return await _db.Spravki.Where(s => s.PatientId == id).OrderBy(s => s.IssueDate).ToListAsync();
    }

    [HttpGet("{id}/appointments")]
    [ProducesResponseType<List<Appointment>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<Appointment>>> GetAppointments(int id)
    {
        if (!await _db.Patients.AnyAsync(p => p.Id == id))
        {
            return NotFound();
        }
        return await _db.Appointments.Where(a => a.PatientId == id).OrderBy(a => a.StartTime).ToListAsync();
    }

    // id: пациент, которого редактируем (0 при создании), чтобы ИИН не считался дублем самого себя
    private async Task<string?> Validate(Patient patient, int id)
    {
        if (string.IsNullOrWhiteSpace(patient.Name))
        {
            return "Укажите ФИО пациента";
        }
        if (patient.Name.Length > 100)
        {
            return "ФИО пациента должно быть не длиннее 100 символов";
        }
        if (patient.BirthDate == default)
        {
            return "Укажите дату рождения в формате ГГГГ-ММ-ДД, например 2000-05-15";
        }
        if (patient.BirthDate > DateOnly.FromDateTime(DateTime.Now))
        {
            return "Дата рождения не может быть в будущем";
        }
        if (patient.BirthDate.Year < 1900)
        {
            return "Дата рождения не может быть раньше 1900 года";
        }
        if (patient.Iin.Length != 12 || !patient.Iin.All(char.IsAsciiDigit))
        {
            return "ИИН должен состоять ровно из 12 цифр";
        }
        if (await _db.Patients.AnyAsync(p => p.Iin == patient.Iin && p.Id != id))
        {
            return "Пациент с таким ИИН уже есть в картотеке";
        }
        if (patient.Phone != "")
        {
            string digits = patient.Phone.StartsWith("+") ? patient.Phone.Substring(1) : patient.Phone;
            if (digits.Length < 10 || digits.Length > 15 || !digits.All(char.IsAsciiDigit))
            {
                return "Телефон должен содержать от 10 до 15 цифр, можно с + в начале, например +77011234567";
            }
        }
        if (patient.Address.Length > 200)
        {
            return "Адрес должен быть не длиннее 200 символов";
        }
        if (patient.Uchastok < 0)
        {
            return "Номер участка не может быть отрицательным";
        }
        return null;
    }
}
