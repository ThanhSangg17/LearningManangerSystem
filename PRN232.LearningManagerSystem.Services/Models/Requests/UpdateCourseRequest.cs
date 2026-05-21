namespace PRN232.LearningManagerSystem.Services.Models.Requests;

public class UpdateCourseRequest
{
    public string CourseName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public int SubjectId { get; set; }
}
