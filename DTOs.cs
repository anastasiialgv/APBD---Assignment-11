namespace DatabaseFirst;

    public class PatientResponseDto
    {
        public string Pesel { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public string Sex { get; set; } 
        public List<AdmissionDto> Admissions { get; set; }
        public List<BedAssignmentDto> BedAssignments { get; set; }
    }

    public class AdmissionDto
    {
        public int Id { get; set; }
        public DateTime AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public WardDto Ward { get; set; }
    }

    public class WardDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class BedAssignmentDto
    {
        public int Id { get; set; }
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public BedDto Bed { get; set; }
    }

    public class BedDto
    {
        public int Id { get; set; }
        public BedTypeDto BedType { get; set; }
        public RoomDto Room { get; set; }
    }

    public class BedTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class RoomDto
    {
        public string Id { get; set; }
        public bool HasTv { get; set; }
        public WardDto Ward { get; set; }
    }

    public class AssignBedRequestDto
    {
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public string BedType { get; set; }
        public string Ward { get; set; }
    }