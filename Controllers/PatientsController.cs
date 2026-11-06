using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedicalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private static List<Patient> _patients = new List<Patient>();

    [HttpGet]
    public List<Patient> GetAll()
    {
        return _patients;
    }

    [HttpGet("{id}")]
    public ActionResult<Patient> GetById(int id)
    {
        Patient? patient = _patients.FirstOrDefault(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }
        return patient;
    }

    [HttpPost]
    public ActionResult<Patient> Create(Patient patient)
    {
        patient.Id = _patients.Count + 1;
        _patients.Add(patient);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpPut("{id}")]
    public ActionResult<Patient> Update(int id, Patient patient)
    {
        Patient? existing = _patients.FirstOrDefault(p => p.Id == id);
        if (existing == null)
        {
            return NotFound();
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
        Patient? patient = _patients.FirstOrDefault(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }
        _patients.Remove(patient);
        return NoContent();
    }
}
