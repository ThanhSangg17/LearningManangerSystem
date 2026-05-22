namespace PRN232.LearningManagerSystem.Services.Models.BusinessModels;

public class SubjectCreateBusinessModel
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Credit { get; set; }
}
