using MedicalApi.Data;
using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly MedicalDbContext _db;

    public StatsController(MedicalDbContext db)
    {
        _db = db;
    }

    // GET /api/stats: сводка для главврача. Каждое число считает база (COUNT, SUM), таблицы целиком не загружаются
    [HttpGet]
    public async Task<ClinicStats> Get()
    {
        return new ClinicStats
        {
            PatientsCount = await _db.Patients.CountAsync(),
            SpravkiCount = await _db.Spravki.CountAsync(),
            SpravkiSum = await _db.Spravki.SumAsync(s => s.Price),
            AppointmentsCount = await _db.Appointments.CountAsync(),
        };
    }
}
