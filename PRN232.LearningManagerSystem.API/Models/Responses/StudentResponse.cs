namespace PRN232.LearningManagerSystem.API.Models.Responses;

public class StudentResponse
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }

    // Business Model derived fields
    public int Age { get; set; }
    public int EnrollmentCount { get; set; }

    // Expand: list of enrollments (populated when ?expand=enrollments)
    public List<EnrollmentSummaryResponse>? Enrollments { get; set; }
}
