using MedicalApi.Data;
using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpravkiController : ControllerBase
{
    private readonly MedicalDbContext _db;

    public SpravkiController(MedicalDbContext db)
    {
        _db = db;
    }

    // GET /api/spravki?patientId=1&spravkaTypeId=2&minPrice=200&maxPrice=500&sort=price, все параметры необязательные.
    // sort: date (по умолчанию), date_desc, price, price_desc
    [HttpGet]
    [ProducesResponseType<List<Spravka>>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<Spravka>>> GetAll(int? patientId, int? spravkaTypeId, decimal? minPrice, decimal? maxPrice, string? sort)
    {
        // Пока не вызван ToListAsync, это только запрос: условия соберутся в один WHERE
        IQueryable<Spravka> query = _db.Spravki;
        if (patientId != null)
        {
            query = query.Where(s => s.PatientId == patientId);
        }
        if (spravkaTypeId != null)
        {
            query = query.Where(s => s.SpravkaTypeId == spravkaTypeId);
        }
        if (minPrice != null)
        {
            query = query.Where(s => s.Price >= minPrice);
        }
        if (maxPrice != null)
        {
            query = query.Where(s => s.Price <= maxPrice);
        }
        switch (sort?.ToLower())
        {
            case null:
            case "date":
                query = query.OrderBy(s => s.IssueDate).ThenBy(s => s.Id);
                break;
            case "date_desc":
                query = query.OrderByDescending(s => s.IssueDate).ThenByDescending(s => s.Id);
                break;
            case "price":
                query = query.OrderBy(s => s.Price).ThenBy(s => s.Id);
                break;
            case "price_desc":
                query = query.OrderByDescending(s => s.Price).ThenBy(s => s.Id);
                break;
            default:
                return BadRequest("Сортировка может быть: date, date_desc, price, price_desc");
        }
        return await query.ToListAsync();
    }

    // GET /api/spravki/stats?from=2026-11-01&to=2026-11-30, даты необязательные
    [HttpGet("stats")]
    [ProducesResponseType<SpravkaStats>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SpravkaStats>> GetStats(DateOnly? from, DateOnly? to)
    {
        if (from != null && to != null && from > to)
        {
            return BadRequest("Дата «с» должна быть не позже даты «по»");
        }
        IQueryable<Spravka> selected = _db.Spravki;
        if (from != null)
        {
            selected = selected.Where(s => s.IssueDate >= from);
        }
        if (to != null)
        {
            selected = selected.Where(s => s.IssueDate <= to);
        }
        // Считает база: COUNT, SUM и GROUP BY уходят в SQL, в C# приходят только итоги
        List<SpravkaTypeStats> byType = await selected
            .GroupBy(s => s.SpravkaTypeId)
            .Select(g => new SpravkaTypeStats
            {
                SpravkaTypeId = g.Key,
                Count = g.Count(),
                Sum = g.Sum(s => s.Price),
            })
            .OrderByDescending(t => t.Sum)
            .ToListAsync();
        Dictionary<int, string> names = await _db.SpravkaTypes.ToDictionaryAsync(t => t.Id, t => t.Name);
        foreach (SpravkaTypeStats t in byType)
        {
            t.Name = names.GetValueOrDefault(t.SpravkaTypeId, "");
        }
        return new SpravkaStats
        {
            TotalCount = byType.Sum(t => t.Count),
            TotalSum = byType.Sum(t => t.Sum),
            ByType = byType,
        };
    }

    [HttpGet("{id}")]
    [ProducesResponseType<Spravka>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Spravka>> GetById(int id)
    {
        Spravka? spravka = await _db.Spravki.FirstOrDefaultAsync(s => s.Id == id);
        if (spravka == null)
        {
            return NotFound();
        }
        return spravka;
    }

    [HttpPost]
    [ProducesResponseType<Spravka>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Spravka>> Create(Spravka spravka)
    {
        string? error = await Validate(spravka);
        if (error != null)
        {
            return BadRequest(error);
        }
        SpravkaType type = await _db.SpravkaTypes.FirstAsync(t => t.Id == spravka.SpravkaTypeId);
        // Номер выдаёт база, Id из тела запроса игнорируем
        spravka.Id = 0;
        spravka.Name = type.Name;
        spravka.Price = type.Price;
        spravka.IssueDate = DateOnly.FromDateTime(DateTime.Today);
        spravka.ValidUntil = spravka.IssueDate.AddDays(type.ValidDays);
        _db.Spravki.Add(spravka);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = spravka.Id }, spravka);
    }

    [HttpPut("{id}")]
    [ProducesResponseType<Spravka>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Spravka>> Update(int id, Spravka spravka)
    {
        Spravka? existing = await _db.Spravki.FirstOrDefaultAsync(s => s.Id == id);
        if (existing == null)
        {
            return NotFound();
        }
        string? error = await Validate(spravka);
        if (error != null)
        {
            return BadRequest(error);
        }
        existing.PatientId = spravka.PatientId;
        // Если вид справки не меняли, оставляем цену, по которой её выписали,
        // даже если с тех пор цена в каталоге изменилась
        if (existing.SpravkaTypeId != spravka.SpravkaTypeId)
        {
            SpravkaType type = await _db.SpravkaTypes.FirstAsync(t => t.Id == spravka.SpravkaTypeId);
            existing.SpravkaTypeId = type.Id;
            existing.Name = type.Name;
            existing.Price = type.Price;
            existing.ValidUntil = existing.IssueDate.AddDays(type.ValidDays);
        }
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        Spravka? spravka = await _db.Spravki.FirstOrDefaultAsync(s => s.Id == id);
        if (spravka == null)
        {
            return NotFound();
        }
        // Записи на приём остаются, только теряют ссылку на удалённую справку:
        // внешний ключ в базе настроен как ON DELETE SET NULL
        _db.Spravki.Remove(spravka);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string?> Validate(Spravka spravka)
    {
        Patient? patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == spravka.PatientId);
        if (patient == null)
        {
            return $"Пациент с номером {spravka.PatientId} не найден. Сначала зарегистрируйте пациента в картотеке";
        }
        SpravkaType? type = await _db.SpravkaTypes.FirstOrDefaultAsync(t => t.Id == spravka.SpravkaTypeId);
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
