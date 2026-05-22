namespace PRN232.LearningManagerSystem.API.Models.Requests;

public class UpdateStudentRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
}
