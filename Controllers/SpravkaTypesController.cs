using MedicalApi.Data;
using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpravkaTypesController : ControllerBase
{
    private readonly MedicalDbContext _db;

    public SpravkaTypesController(MedicalDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<List<SpravkaType>> GetAll()
    {
        return await _db.SpravkaTypes.OrderBy(t => t.Id).ToListAsync();
    }

    [HttpGet("{id}")]
    [ProducesResponseType<SpravkaType>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpravkaType>> GetById(int id)
    {
        SpravkaType? type = await _db.SpravkaTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (type == null)
        {
            return NotFound();
        }
        return type;
    }

    [HttpPost]
    [ProducesResponseType<SpravkaType>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SpravkaType>> Create(SpravkaType type)
    {
        string? error = await Validate(type, 0);
        if (error != null)
        {
            return BadRequest(error);
        }
        // Номер выдаёт база, Id из тела запроса игнорируем
        type.Id = 0;
        _db.SpravkaTypes.Add(type);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
    }

    [HttpPut("{id}")]
    [ProducesResponseType<SpravkaType>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpravkaType>> Update(int id, SpravkaType type)
    {
        SpravkaType? existing = await _db.SpravkaTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null)
        {
            return NotFound();
        }
        string? error = await Validate(type, id);
        if (error != null)
        {
            return BadRequest(error);
        }
        existing.Name = type.Name;
        existing.Price = type.Price;
        existing.ValidDays = type.ValidDays;
        existing.NeedsDoctorExam = type.NeedsDoctorExam;
        existing.ForChildren = type.ForChildren;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        SpravkaType? type = await _db.SpravkaTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (type == null)
        {
            return NotFound();
        }
        int used = await _db.Spravki.CountAsync(s => s.SpravkaTypeId == id);
        if (used > 0)
        {
            return BadRequest($"Нельзя удалить вид справки «{type.Name}»: по нему уже выписано справок: {used}");
        }
        _db.SpravkaTypes.Remove(type);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string?> Validate(SpravkaType type, int id)
    {
        if (string.IsNullOrWhiteSpace(type.Name))
        {
            return "Укажите название вида справки";
        }
        if (type.Name.Length > 100)
        {
            return "Название вида справки должно быть не длиннее 100 символов";
        }
        string name = type.Name.Trim().ToLower();
        if (await _db.SpravkaTypes.AnyAsync(t => t.Name.Trim().ToLower() == name && t.Id != id))
        {
            return $"Вид справки «{type.Name}» уже есть в каталоге";
        }
        if (type.Price <= 0)
        {
            return "Цена должна быть больше 0";
        }
        if (type.Price > 1000000)
        {
            return "Цена должна быть не больше 1 000 000 тенге";
        }
        if (type.ValidDays <= 0)
        {
            return "Срок действия справки должен быть больше 0 дней";
        }
        if (type.ValidDays > 3650)
        {
            return "Срок действия справки должен быть не больше 3650 дней (10 лет)";
        }
        return null;
    }
}
