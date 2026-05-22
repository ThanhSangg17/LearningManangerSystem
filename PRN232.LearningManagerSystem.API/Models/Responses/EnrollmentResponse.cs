namespace PRN232.LearningManagerSystem.API.Models.Responses;

public class EnrollmentResponse
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrollDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public StudentSummaryResponse? Student { get; set; }
    public CourseSummaryResponse? Course { get; set; }
}
