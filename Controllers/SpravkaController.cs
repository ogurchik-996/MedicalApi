using Microsoft.AspNetCore.Mvc;
using MedicalApi.Models;

namespace MedicalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpravkiController : ControllerBase
{
    public static List<Spravka> Spravki = new List<Spravka>();

    // GET /api/spravki?patientId=3&spravkaTypeId=1, оба фильтра необязательные
    [HttpGet]
    public List<Spravka> GetAll(int? patientId, int? spravkaTypeId)
    {
        IEnumerable<Spravka> result = Spravki;
        if (patientId != null)
        {
            result = result.Where(s => s.PatientId == patientId);
        }
        if (spravkaTypeId != null)
        {
            result = result.Where(s => s.SpravkaTypeId == spravkaTypeId);
        }
        return result.ToList();
    }

    // GET /api/spravki/stats?from=2026-11-01&to=2026-11-30, даты необязательные
    [HttpGet("stats")]
    public ActionResult<SpravkaStats> GetStats(DateTime? from, DateTime? to)
    {
        if (from != null && to != null && from > to)
        {
            return BadRequest("Дата «с» должна быть не позже даты «по»");
        }
        IEnumerable<Spravka> selected = Spravki;
        if (from != null)
        {
            selected = selected.Where(s => s.IssueDate >= from.Value.Date);
        }
        if (to != null)
        {
            selected = selected.Where(s => s.IssueDate <= to.Value.Date);
        }
        List<Spravka> list = selected.ToList();
        return new SpravkaStats
        {
            TotalCount = list.Count,
            TotalSum = list.Sum(s => s.Price),
            ByType = list
                .GroupBy(s => s.SpravkaTypeId)
                .Select(g => new SpravkaTypeStats
                {
                    SpravkaTypeId = g.Key,
                    Name = g.First().Name,
                    Count = g.Count(),
                    Sum = g.Sum(s => s.Price),
                })
                .OrderByDescending(t => t.Sum)
                .ToList(),
        };
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
        // Записи на приём остаются, только теряют ссылку на удалённую справку
        foreach (Appointment appointment in AppointmentsController.Appointments.Where(a => a.SpravkaId == id))
        {
            appointment.SpravkaId = null;
        }
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
