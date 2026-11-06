using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedicalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpravkaTypesController : ControllerBase
{
    public static List<SpravkaType> Types = new List<SpravkaType>
    {
        new SpravkaType { Id = 1, Name = "Справка 083", Price = 100, ValidDays = 365, NeedsDoctorExam = true, ForChildren = false },
        new SpravkaType { Id = 2, Name = "Санитарная книжка", Price = 750, ValidDays = 365, NeedsDoctorExam = true, ForChildren = false },
        new SpravkaType { Id = 3, Name = "Справка в бассейн", Price = 300, ValidDays = 90, NeedsDoctorExam = true, ForChildren = false },
        new SpravkaType { Id = 4, Name = "Справка 086 для поступления", Price = 500, ValidDays = 180, NeedsDoctorExam = true, ForChildren = false },
        new SpravkaType { Id = 5, Name = "Справка в детский лагерь", Price = 200, ValidDays = 3, NeedsDoctorExam = true, ForChildren = true },
        new SpravkaType { Id = 6, Name = "Справка в спортивную секцию", Price = 250, ValidDays = 365, NeedsDoctorExam = false, ForChildren = true },
    };

    [HttpGet]
    public List<SpravkaType> GetAll()
    {
        return Types;
    }

    [HttpGet("{id}")]
    public ActionResult<SpravkaType> GetById(int id)
    {
        SpravkaType? type = Types.FirstOrDefault(t => t.Id == id);
        if (type == null)
        {
            return NotFound();
        }
        return type;
    }

    [HttpPost]
    public ActionResult<SpravkaType> Create(SpravkaType type)
    {
        string? error = Validate(type, 0);
        if (error != null)
        {
            return BadRequest(error);
        }
        type.Id = Types.Count + 1;
        Types.Add(type);
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
    }

    [HttpPut("{id}")]
    public ActionResult<SpravkaType> Update(int id, SpravkaType type)
    {
        SpravkaType? existing = Types.FirstOrDefault(t => t.Id == id);
        if (existing == null)
        {
            return NotFound();
        }
        string? error = Validate(type, id);
        if (error != null)
        {
            return BadRequest(error);
        }
        existing.Name = type.Name;
        existing.Price = type.Price;
        existing.ValidDays = type.ValidDays;
        existing.NeedsDoctorExam = type.NeedsDoctorExam;
        existing.ForChildren = type.ForChildren;
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        SpravkaType? type = Types.FirstOrDefault(t => t.Id == id);
        if (type == null)
        {
            return NotFound();
        }
        int used = SpravkiController.Spravki.Count(s => s.SpravkaTypeId == id);
        if (used > 0)
        {
            return BadRequest($"Нельзя удалить вид справки «{type.Name}»: по нему уже выписано справок: {used}");
        }
        Types.Remove(type);
        return NoContent();
    }

    private string? Validate(SpravkaType type, int id)
    {
        if (string.IsNullOrWhiteSpace(type.Name))
        {
            return "Укажите название вида справки";
        }
        if (type.Name.Length > 100)
        {
            return "Название вида справки должно быть не длиннее 100 символов";
        }
        if (Types.Any(t => t.Name.Trim().ToLower() == type.Name.Trim().ToLower() && t.Id != id))
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
