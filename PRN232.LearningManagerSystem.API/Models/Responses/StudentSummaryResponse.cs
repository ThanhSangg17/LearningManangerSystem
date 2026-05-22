namespace PRN232.LearningManagerSystem.API.Models.Responses;

public class StudentSummaryResponse
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
