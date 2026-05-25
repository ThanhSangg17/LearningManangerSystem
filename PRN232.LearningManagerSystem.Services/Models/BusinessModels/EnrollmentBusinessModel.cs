namespace PRN232.LearningManagerSystem.Services.Models.BusinessModels;

/// <summary>
/// Business Model for Enrollment. Computed field: IsActive.
/// </summary>
public class EnrollmentBusinessModel
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrollDate { get; set; }
    public string Status { get; set; } = string.Empty;

    // Denormalized from related entities
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;

    public StudentBusinessModel? Student { get; set; }

    // Computed field (Business Logic)

    /// <summary>True when Status equals "Active" (case-insensitive).</summary>
    public bool IsActive => string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);
}
