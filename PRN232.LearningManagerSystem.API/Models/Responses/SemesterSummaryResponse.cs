namespace PRN232.LearningManagerSystem.API.Models.Responses;

public class SemesterSummaryResponse
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
