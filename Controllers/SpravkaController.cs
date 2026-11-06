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
        SpravkaType type = SpravkaTypesController.Types.First(t => t.Id == spravka.SpravkaTypeId);
        spravka.Id = Spravki.Count + 1;
        spravka.Name = type.Name;
        spravka.Price = type.Price;
        spravka.IssueDate = DateTime.Today;
        spravka.ValidUntil = spravka.IssueDate.AddDays(type.ValidDays);
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
        existing.PatientId = spravka.PatientId;
        // Если вид справки не меняли, оставляем цену, по которой её выписали,
        // даже если с тех пор цена в каталоге изменилась
        if (existing.SpravkaTypeId != spravka.SpravkaTypeId)
        {
            SpravkaType type = SpravkaTypesController.Types.First(t => t.Id == spravka.SpravkaTypeId);
            existing.SpravkaTypeId = type.Id;
            existing.Name = type.Name;
            existing.Price = type.Price;
            existing.ValidUntil = existing.IssueDate.AddDays(type.ValidDays);
        }
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
        Patient? patient = PatientsController.Patients.FirstOrDefault(p => p.Id == spravka.PatientId);
        if (patient == null)
        {
            return $"Пациент с номером {spravka.PatientId} не найден. Сначала зарегистрируйте пациента в картотеке";
        }
        SpravkaType? type = SpravkaTypesController.Types.FirstOrDefault(t => t.Id == spravka.SpravkaTypeId);
        if (type == null)
        {
            return $"Вид справки с номером {spravka.SpravkaTypeId} не найден. Выберите вид справки из каталога";
        }
        if (type.ForChildren && patient.GetAge() >= 18)
        {
            return $"«{type.Name}» выдаётся только детям до 18 лет, а пациенту уже исполнилось 18";
        }
        return null;
    }
}
