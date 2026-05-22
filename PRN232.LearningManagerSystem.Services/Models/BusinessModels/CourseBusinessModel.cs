namespace PRN232.LearningManagerSystem.Services.Models.BusinessModels;

/// <summary>
/// Business Model for Course. Sits between the Entity (from repository) and the Response DTO.
/// Contains computed/enriched fields derived from related entities.
/// </summary>
public class CourseBusinessModel
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public int SubjectId { get; set; }

    // Denormalized from related entities
    public string SemesterName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Credit { get; set; }

    // Computed fields (Business Logic)

    /// <summary>Number of enrollments in this course.</summary>
    public int EnrollmentCount { get; set; }

    /// <summary>Human-readable display name: "{SubjectCode} - {CourseName}"</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Expanded list of enrollments (populated when ?expand=enrollments).</summary>
    public List<EnrollmentBusinessModel>? Enrollments { get; set; }
}
