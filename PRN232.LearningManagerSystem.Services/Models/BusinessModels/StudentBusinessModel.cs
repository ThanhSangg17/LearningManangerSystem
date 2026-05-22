namespace PRN232.LearningManagerSystem.Services.Models.BusinessModels;

/// <summary>
/// Business Model for Student. Computed fields: Age, EnrollmentCount.
/// </summary>
public class StudentBusinessModel
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }

    // Computed fields (Business Logic)

    /// <summary>
    /// Age in full years (current year minus birth year, adjusted for whether the birthday
    /// has already occurred this calendar year).
    /// </summary>
    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age))
                age--;
            return age;
        }
    }

    /// <summary>Number of enrollments belonging to this student.</summary>
    public int EnrollmentCount { get; set; }

    /// <summary>Expanded list of enrollments (populated when ?expand=enrollments).</summary>
    public List<EnrollmentBusinessModel>? Enrollments { get; set; }
}
