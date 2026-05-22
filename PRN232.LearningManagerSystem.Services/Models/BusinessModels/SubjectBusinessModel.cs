namespace PRN232.LearningManagerSystem.Services.Models.BusinessModels;

/// <summary>
/// Business Model for Subject. Computed field: CourseCount.
/// </summary>
public class SubjectBusinessModel
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Credit { get; set; }

    // Computed field (Business Logic)

    /// <summary>Number of courses belonging to this subject.</summary>
    public int CourseCount { get; set; }

    /// <summary>Expanded list of courses (populated when ?expand=courses).</summary>
    public List<CourseBusinessModel>? Courses { get; set; }
}
