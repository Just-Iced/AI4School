using System.Text;
using AI4SchoolAgent.Models;
using Microsoft.EntityFrameworkCore;

namespace AI4SchoolAgent.Tools;

public class StudentsAgentTools {
    private readonly DbContext _dbContext;

    public StudentsAgentTools(DbContext dbContext) {
        _dbContext = dbContext;
    }

    public async Task<string> GetStudentDetailsByIdAsync(int studentId) {
        // Query the database for student information

        var student = await _dbContext.Set<Student>().FindAsync(studentId);

        return student != null

            ? $"Student {studentId}: {student.FirstName} {student.LastName}, School: {student.School}, Gender: {student.Gender}, Status: {student.Status ?? "not recorded"}"

            : "Student not found.";
    }

    public async Task<string> GetStudentsByGenderAsync(string gender) {
        // Query the database for student information by gender

        var students = await _dbContext.Set<Student>().Where(s => s.Gender == gender).ToListAsync();

        if (students.Count == 0)
            return "No students found.";

        var result = new StringBuilder();

        foreach (var student in students) {
            result.AppendLine($"Student {student.StudentId}: {student.FirstName} {student.LastName}, School: {student.School}, Gender: {student.Gender}, Status: {student.Status ?? "not recorded"}");
        }
        return result.ToString();
    }

    public async Task<string> GetStudentsBySchoolAsync(string school) {
        // Query the database for student information by school
        var students = await _dbContext.Set<Student>().Where(s => s.School == school).ToListAsync();

        if (students.Count == 0)
            return "No students found.";

        var result = new StringBuilder();

        foreach (var student in students) {
            result.AppendLine($"Student {student.StudentId}: {student.FirstName} {student.LastName}, School: {student.School}, Gender: {student.Gender}, Status: {student.Status ?? "not recorded"}");
        }
        return result.ToString();
    }

    public async Task<string> GetStudentsByAgeAsync() {
        // Query the database for student information by school

        var students = await _dbContext.Set<Student>().ToListAsync();

        if (students.Count == 0)
            return "No students found.";

        var result = new StringBuilder();

        foreach (var student in students) {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var age = student.DateOfBirth is { } dateOfBirth
                ? today.Year - dateOfBirth.Year - (today < dateOfBirth.AddYears(today.Year - dateOfBirth.Year) ? 1 : 0)
                : (int?)null;

            result.AppendLine($"Student {student.StudentId}: {student.FirstName} {student.LastName}, School: {student.School}, Gender: {student.Gender}, Born: {student.DateOfBirth?.ToString("yyyy-MM-dd") ?? "not recorded"}, Age: {age?.ToString() ?? "not recorded"}");
        }
        return result.ToString();
    }

    public async Task<string> GetAllStudentsAsync() {
        // Query the database for all students
        var students = await _dbContext.Set<Student>().ToListAsync();

        if (students.Count == 0)
            return "No students found.";

        var result = new StringBuilder();

        foreach (var student in students) {
            result.AppendLine($"Student {student.StudentId}: {student.FirstName} {student.LastName}, School: {student.School}, Gender: {student.Gender}, Status: {student.Status ?? "not recorded"}");
        }
        return result.ToString();
    }

    public async Task<string> ProcessWithdrawalAsync(int studentId, string reason) {
        // Business logic to process the withdrawal
        var student = await _dbContext.Set<Student>().FindAsync(studentId);

        if (student == null)
            return "Student not found.";

        // Assuming we have a property to track return status for the student
        student.Status = "Withdrawal Initiated";

        await _dbContext.SaveChangesAsync();

        return $"Withdrawal initiated for student {studentId}.";
    }
}

