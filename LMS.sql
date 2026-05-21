CREATE DATABASE PRN232_LMS;
GO

USE PRN232_LMS;
GO

-- =====================================================
-- TABLE: Semester
-- =====================================================
CREATE TABLE Semester
(
    SemesterId INT IDENTITY(1,1) PRIMARY KEY,

    SemesterName NVARCHAR(100) NOT NULL,

    StartDate DATETIME NOT NULL,

    EndDate DATETIME NOT NULL,

    CONSTRAINT CK_Semester_Date
        CHECK (EndDate > StartDate)
);
GO

-- =====================================================
-- TABLE: Course
-- =====================================================
CREATE TABLE Course
(
    CourseId INT IDENTITY(1,1) PRIMARY KEY,

    CourseName NVARCHAR(100) NOT NULL,

    SemesterId INT NOT NULL,

    CONSTRAINT FK_Course_Semester
        FOREIGN KEY (SemesterId)
        REFERENCES Semester(SemesterId)
);
GO

-- =====================================================
-- TABLE: Subject
-- =====================================================
CREATE TABLE Subject
(
    SubjectId INT IDENTITY(1,1) PRIMARY KEY,

    SubjectCode VARCHAR(20) NOT NULL UNIQUE,

    SubjectName NVARCHAR(100) NOT NULL,

    Credit INT NOT NULL,

    CONSTRAINT CK_Subject_Credit
        CHECK (Credit > 0)
);
GO

-- =====================================================
-- TABLE: CourseSubject
-- MANY-TO-MANY: Course <-> Subject
-- =====================================================
CREATE TABLE CourseSubject
(
    CourseId INT NOT NULL,

    SubjectId INT NOT NULL,

    PRIMARY KEY (CourseId, SubjectId),

    CONSTRAINT FK_CourseSubject_Course
        FOREIGN KEY (CourseId)
        REFERENCES Course(CourseId),

    CONSTRAINT FK_CourseSubject_Subject
        FOREIGN KEY (SubjectId)
        REFERENCES Subject(SubjectId)
);
GO

-- =====================================================
-- TABLE: Student
-- =====================================================
CREATE TABLE Student
(
    StudentId INT IDENTITY(1,1) PRIMARY KEY,

    FullName NVARCHAR(100) NOT NULL,

    Email VARCHAR(100) NOT NULL UNIQUE,

    DateOfBirth DATETIME NOT NULL
);
GO

-- =====================================================
-- TABLE: Enrollment
-- =====================================================
CREATE TABLE Enrollment
(
    EnrollmentId INT IDENTITY(1,1) PRIMARY KEY,

    StudentId INT NOT NULL,

    CourseId INT NOT NULL,

    EnrollDate DATETIME NOT NULL DEFAULT GETDATE(),

    Status VARCHAR(20) NOT NULL,

    CONSTRAINT FK_Enrollment_Student
        FOREIGN KEY (StudentId)
        REFERENCES Student(StudentId),

    CONSTRAINT FK_Enrollment_Course
        FOREIGN KEY (CourseId)
        REFERENCES Course(CourseId),

    -- Một student không được enroll cùng course nhiều lần
    CONSTRAINT UQ_Enrollment
        UNIQUE (StudentId, CourseId),

    -- Chỉ cho phép status hợp lệ
    CONSTRAINT CK_Enrollment_Status
        CHECK (Status IN ('Active', 'Completed', 'Dropped', 'Waiting'))
);
GO