namespace PRN232.LearningManagerSystem.Services.Models.BusinessModels;

public class EnrollmentUpdateBusinessModel
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrollDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
