namespace PRN232.LearningManagerSystem.API.Models.Responses;

public class SemesterResponse
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Business Model derived fields
    public int CourseCount { get; set; }
    public bool IsCurrent { get; set; }

    // Expand: list of courses (populated when ?expand=courses)
    public List<CourseSummaryResponse>? Courses { get; set; }
}
