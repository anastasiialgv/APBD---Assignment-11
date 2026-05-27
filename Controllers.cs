using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DatabaseFirst.Models;
namespace DatabaseFirst

{
    [ApiController]
    [Route("api/[controller]")] 
    public class PatientsController : ControllerBase
    {
        private readonly HospitalDbContext _context;

        public PatientsController(HospitalDbContext context)
        {
            _context = context;
        }

        // GET: /api/patients?search=...
        [HttpGet]
        public async Task<IActionResult> GetPatients([FromQuery] string? search)
        {
            var query = _context.Patients
                .Include(p => p.Admissions).ThenInclude(a => a.Ward)
                .Include(p => p.BedAssignments).ThenInclude(ba => ba.Bed).ThenInclude(b => b.BedType)
                .Include(p => p.BedAssignments).ThenInclude(ba => ba.Bed).ThenInclude(b => b.Room).ThenInclude(r => r.Ward)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lowerSearch = search.ToLower();
                query = query.Where(p => p.FirstName.ToLower().Contains(lowerSearch) 
                                      || p.LastName.ToLower().Contains(lowerSearch)); 
            }
            var patients = await query.ToListAsync();
            var result = patients.Select(p => new PatientResponseDto
            {
                Pesel = p.Pesel,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Age = p.Age,
                Sex = p.Sex ? "Male" : "Female", 
                Admissions = p.Admissions.Select(a => new AdmissionDto
                {
                    Id = a.Id,
                    AdmissionDate = a.AdmissionDate,
                    DischargeDate = a.DischargeDate,
                    Ward = new WardDto
                    {
                        Id = a.Ward.Id,
                        Name = a.Ward.Name,
                        Description = a.Ward.Description
                    }
                }).ToList(),
                BedAssignments = p.BedAssignments.Select(ba => new BedAssignmentDto
                {
                    Id = ba.Id,
                    From = ba.From,
                    To = ba.To,
                    Bed = new BedDto
                    {
                        Id = ba.Bed.Id,
                        BedType = new BedTypeDto
                        {
                            Id = ba.Bed.BedType.Id,
                            Name = ba.Bed.BedType.Name,
                            Description = ba.Bed.BedType.Description
                        },
                        Room = new RoomDto
                        {
                            Id = ba.Bed.RoomId,
                            HasTv = ba.Bed.Room.HasTv,
                            Ward = new WardDto
                            {
                                Id = ba.Bed.Room.Ward.Id,
                                Name = ba.Bed.Room.Ward.Name,
                                Description = ba.Bed.Room.Ward.Description
                            }
                        }
                    }
                }).ToList()
            }).ToList();

            return Ok(result);
        }
        
        // POST: /api/patients/{pesel}/bedassignments
    [HttpPost("{pesel}/bedassignments")]
    public async Task<IActionResult> AssignBed(string pesel, [FromBody] AssignBedRequestDto request)
    {
        var patientExists = await _context.Patients.AnyAsync(p => p.Pesel == pesel);
        if (!patientExists)
        {
            return NotFound($"Pacjent o numerze PESEL {pesel} nie istnieje w bazie danych."); 
        }

        DateTime reqFrom = request.From;
        DateTime reqTo = request.To ?? DateTime.MaxValue;

        var matchingBeds = await _context.Beds
            .Include(b => b.BedType)
            .Include(b => b.Room).ThenInclude(r => r.Ward)
            .Where(b => b.BedType.Name == request.BedType && b.Room.Ward.Name == request.Ward)
            .ToListAsync();

        if (!matchingBeds.Any())
        {
            return NotFound($"Nie znaleziono łóżek typu '{request.BedType}' na oddziale '{request.Ward}'."); 
        }

        Bed? availableBed = null;

        foreach (var bed in matchingBeds)
        {
            var assignments = await _context.BedAssignments
                .Where(ba => ba.BedId == bed.Id)
                .ToListAsync();
            
            bool isOccupied = assignments.Any(ba =>
            {
                DateTime existFrom = ba.From;
                DateTime existTo = ba.To ?? DateTime.MaxValue;

                return reqFrom < existTo && reqTo > existFrom;
            });

            if (!isOccupied)
            {
                availableBed = bed;
                break; 
            }
        }

        if (availableBed == null)
        {
            return NotFound($"Wszystkie łóżka typu '{request.BedType}' na oddziale '{request.Ward}' są zajęte w wybranym okresie czasu."); 
        }

        var newAssignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId = availableBed.Id,
            From = request.From,
            To = request.To
        };

        _context.BedAssignments.Add(newAssignment);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "OK", AssignmentId = newAssignment.Id });
        }
    }
}