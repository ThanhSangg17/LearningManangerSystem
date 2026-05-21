using System;
using System.Collections.Generic;

namespace PRN232.LearningManagerSystem.Repositories.Models;

public partial class Semester
{
    public int SemesterId { get; set; }

    public string SemesterName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
