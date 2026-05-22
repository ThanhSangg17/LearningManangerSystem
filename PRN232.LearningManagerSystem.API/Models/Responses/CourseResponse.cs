namespace PRN232.LearningManagerSystem.API.Models.Responses;

public class CourseResponse
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public int SubjectId { get; set; }
    public SemesterSummaryResponse? Semester { get; set; }
    public SubjectSummaryResponse? Subject { get; set; }

    // Business Model derived fields
    public int EnrollmentCount { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    // Expand: list of enrollments (populated when ?expand=enrollments)
    public List<EnrollmentSummaryResponse>? Enrollments { get; set; }
}
