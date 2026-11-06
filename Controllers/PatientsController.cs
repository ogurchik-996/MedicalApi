using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedicalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    public static List<Patient> Patients = new List<Patient>();

    [HttpGet]
    public List<Patient> GetAll()
    {
        return Patients;
    }

    [HttpGet("{id}")]
    public ActionResult<Patient> GetById(int id)
    {
        Patient? patient = Patients.FirstOrDefault(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }
        return patient;
    }

    [HttpPost]
    public ActionResult<Patient> Create(Patient patient)
    {
        string? error = Validate(patient, 0);
        if (error != null)
        {
            return BadRequest(error);
        }
        patient.Id = Patients.Count + 1;
        Patients.Add(patient);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpPut("{id}")]
    public ActionResult<Patient> Update(int id, Patient patient)
    {
        Patient? existing = Patients.FirstOrDefault(p => p.Id == id);
        if (existing == null)
        {
            return NotFound();
        }
        string? error = Validate(patient, id);
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
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        Patient? patient = Patients.FirstOrDefault(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }
        Patients.Remove(patient);
        return NoContent();
    }

    [HttpGet("{id}/spravki")]
    public ActionResult<List<Spravka>> GetSpravki(int id)
    {
        Patient? patient = Patients.FirstOrDefault(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }
        return SpravkiController.Spravki.Where(s => s.PatientId == id).ToList();
    }

    // id: пациент, которого редактируем (0 при создании), чтобы ИИН не считался дублем самого себя
    private string? Validate(Patient patient, int id)
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
        if (patient.BirthDate > DateTime.Now)
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
        if (Patients.Any(p => p.Iin == patient.Iin && p.Id != id))
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
