namespace PRN232.LearningManagerSystem.Services.Models.Responses;

public class CourseSummaryResponse
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public int SubjectId { get; set; }
    public SemesterSummaryResponse? Semester { get; set; }
    public SubjectSummaryResponse? Subject { get; set; }
}
