using Microsoft.AspNetCore.Mvc;
using MedicalApi.Models;

namespace MedicalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpravkiController : ControllerBase
{
    public static List<Spravka> Spravki = new List<Spravka>();

    [HttpGet]
    public List<Spravka> GetAll()
    {
        return Spravki;
    }

    [HttpGet("{id}")]
    public ActionResult<Spravka> GetById(int id)
    {
        Spravka? spravka = Spravki.FirstOrDefault(s => s.Id == id);
        if (spravka == null)
        {
            return NotFound();
        }
        return spravka;
    }

    [HttpPost]
    public ActionResult<Spravka> Create(Spravka spravka)
    {
        string? error = Validate(spravka);
        if (error != null)
        {
            return BadRequest(error);
        }
        spravka.Id = Spravki.Count + 1;
        Spravki.Add(spravka);
        return CreatedAtAction(nameof(GetById), new { id = spravka.Id }, spravka);
    }

    [HttpPut("{id}")]
    public ActionResult<Spravka> Update(int id, Spravka spravka)
    {
        Spravka? existing = Spravki.FirstOrDefault(s => s.Id == id);
        if (existing == null)
        {
            return NotFound();
        }
        string? error = Validate(spravka);
        if (error != null)
        {
            return BadRequest(error);
        }
        existing.Name = spravka.Name;
        existing.PatientId = spravka.PatientId;
        existing.Price = spravka.Price;
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        Spravka? spravka = Spravki.FirstOrDefault(s => s.Id == id);
        if (spravka == null)
        {
            return NotFound();
        }
        Spravki.Remove(spravka);
        return NoContent();
    }

    private string? Validate(Spravka spravka)
    {
        if (string.IsNullOrWhiteSpace(spravka.Name))
        {
            return "Укажите название справки";
        }
        if (spravka.Name.Length > 100)
        {
            return "Название справки должно быть не длиннее 100 символов";
        }
        if (spravka.Price <= 0)
        {
            return "Цена должна быть больше 0";
        }
        if (spravka.Price > 1000000)
        {
            return "Цена должна быть не больше 1 000 000 тенге";
        }
        Patient? patient = PatientsController.Patients.FirstOrDefault(p => p.Id == spravka.PatientId);
        if (patient == null)
        {
            return $"Пациент с номером {spravka.PatientId} не найден. Сначала зарегистрируйте пациента в картотеке";
        }
        return null;
    }
}
