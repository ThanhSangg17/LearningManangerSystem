namespace PRN232.LearningManagerSystem.API.Models.Responses;

public class SubjectResponse
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Credit { get; set; }

    // Business Model derived field
    public int CourseCount { get; set; }

    // Expand: list of courses (populated when ?expand=courses)
    public List<CourseSummaryResponse>? Courses { get; set; }
}
