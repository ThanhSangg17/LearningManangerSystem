namespace PRN232.LearningManagerSystem.Services.Models.BusinessModels;

/// <summary>
/// Business Model for Semester. Computed fields: CourseCount, IsCurrent.
/// </summary>
public class SemesterBusinessModel
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Computed fields (Business Logic)

    /// <summary>Number of courses belonging to this semester.</summary>
    public int CourseCount { get; set; }

    /// <summary>True when today's date falls within [StartDate, EndDate] (inclusive).</summary>
    public bool IsCurrent
    {
        get
        {
            var now = DateTime.Now;
            return now >= StartDate && now <= EndDate;
        }
    }

    /// <summary>Expanded list of courses (populated when ?expand=courses).</summary>
    public List<CourseBusinessModel>? Courses { get; set; }
}
