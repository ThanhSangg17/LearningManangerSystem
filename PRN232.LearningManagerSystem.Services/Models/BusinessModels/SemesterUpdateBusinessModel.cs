namespace PRN232.LearningManagerSystem.Services.Models.BusinessModels;

public class SemesterUpdateBusinessModel
{
    public string SemesterName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
