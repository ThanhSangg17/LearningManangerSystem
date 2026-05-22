namespace PRN232.LearningManagerSystem.Services.Models.BusinessModels;

public class CourseUpdateBusinessModel
{
    public string CourseName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public int SubjectId { get; set; }
}
