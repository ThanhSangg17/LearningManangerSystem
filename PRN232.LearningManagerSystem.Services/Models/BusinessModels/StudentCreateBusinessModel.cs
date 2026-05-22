namespace PRN232.LearningManagerSystem.Services.Models.BusinessModels;

public class StudentCreateBusinessModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
}
