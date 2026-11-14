using MedicalApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalApi.Data;

public class MedicalDbContext : DbContext
{
    public MedicalDbContext(DbContextOptions<MedicalDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<Spravka> Spravki { get; set; }
    public DbSet<SpravkaType> SpravkaTypes { get; set; }
    public DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // у справки один пациент, у пациента много справок, связь через PatientId
        modelBuilder.Entity<Spravka>()
            .HasOne<Patient>()
            .WithMany()
            .HasForeignKey(s => s.PatientId)
            // справки это медицинские документы: пациента со справками удалить нельзя
            .OnDelete(DeleteBehavior.Restrict);

        // у справки один вид, у вида много справок, связь через SpravkaTypeId
        modelBuilder.Entity<Spravka>()
            .HasOne<SpravkaType>()
            .WithMany()
            .HasForeignKey(s => s.SpravkaTypeId)
            // вид справки, по которому уже выписаны справки, удалить нельзя
            .OnDelete(DeleteBehavior.Restrict);

        // у записи на приём один пациент, у пациента много записей
        modelBuilder.Entity<Appointment>()
            .HasOne<Patient>()
            .WithMany()
            .HasForeignKey(a => a.PatientId);

        // запись может ссылаться на справку. Удалили справку: запись остаётся без ссылки
        modelBuilder.Entity<Appointment>()
            .HasOne<Spravka>()
            .WithMany()
            .HasForeignKey(a => a.SpravkaId)
            .OnDelete(DeleteBehavior.SetNull);

        // время приёма местное (по часам поликлиники), без часового пояса
        modelBuilder.Entity<Appointment>()
            .Property(a => a.StartTime)
            .HasColumnType("timestamp without time zone");
    }
}
