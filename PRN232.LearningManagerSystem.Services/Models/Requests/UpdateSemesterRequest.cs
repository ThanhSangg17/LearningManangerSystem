namespace PRN232.LearningManagerSystem.Services.Models.Requests;

public class UpdateSemesterRequest
{
    public string SemesterName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
