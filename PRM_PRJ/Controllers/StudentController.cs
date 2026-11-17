using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Request;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;
using PRM_PRJ.Enums;

namespace PRM_PRJ.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/student")]
    public class StudentController : ControllerBase
    {
        private readonly AppDbContext _context;
       

        public StudentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("grades")]
        public async Task<IActionResult> GetGrades()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
                return NotFound(new { message = "Không tìm thấy student gắn với tài khoản hiện tại" });

            var grades = await _context.Scores
                .Include(x => x.Subject)
                .Include(x => x.Semester)
                    .ThenInclude(x => x.SchoolYear)
                .Where(x => x.StudentId == student.Id)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new GradeItemResponse
                {
                    GradeId = x.Id,
                    StudentCode = student.StudentCode,
                    StudentFullName = student.FullName,
                    AcademicYear = x.Semester.SchoolYear.Name,
                    Semester = x.Semester.Name,
                    SubjectName = x.Subject.Name,
                    AssessmentType = x.Type.ToString(),
                    Score = x.Value ?? 0,
                    Grade = x.Grade,
                    CreatedAt = x.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Ok(grades);
        }

        [HttpGet("weekly-timetable")]
        public async Task<IActionResult> GetWeeklyTimetable([FromQuery] string? weekStartDate)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
                return NotFound(new { message = "Không tìm thấy student gắn với tài khoản hiện tại" });

            var query = _context.Timetables
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .Include(x => x.Class)
                .Where(x => x.ClassId == student.ClassId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(weekStartDate))
            {
                if (!DateTime.TryParse(weekStartDate, out var parsedWeek))
                    return BadRequest(new { message = "weekStartDate không hợp lệ. Dùng yyyy-MM-dd" });

                var normalizedWeek = parsedWeek.Date;
                query = query.Where(x => x.WeekStartDate.Date == normalizedWeek);
            }

            var rawItems = await query
                .OrderBy(x => x.WeekStartDate)
                .ThenBy(x => x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .Select(x => new
                {
                    x.Id,
                    SubjectName = x.Subject.Name,
                    TeacherName = x.Teacher.FullName,
                    x.DayOfWeek,
                    x.StartTime,
                    x.EndTime,
                    ClassName = x.Class.Name,
                    x.WeekStartDate
                })
                .ToListAsync();

            var items = rawItems.Select(x =>
            {
                var hasWeek = x.WeekStartDate > new DateTime(2000, 1, 1);
                var weekStr = hasWeek ? x.WeekStartDate.ToString("yyyy-MM-dd") : string.Empty;
                var lessonDateStr = hasWeek
                    ? x.WeekStartDate.Date.AddDays(ToWeekdayOffset(x.DayOfWeek)).ToString("yyyy-MM-dd")
                    : string.Empty;

                return new TimetableItemResponse
                {
                    Id = x.Id,
                    SubjectName = x.SubjectName,
                    TeacherName = x.TeacherName,
                    DayOfWeek = x.DayOfWeek.ToString(),
                    StartTime = x.StartTime.ToString(@"hh\:mm"),
                    EndTime = x.EndTime.ToString(@"hh\:mm"),
                    ClassName = x.ClassName,
                    WeekStartDate = weekStr,
                    LessonDate = lessonDateStr
                };
            }).ToList();

            return Ok(items);
        }

        private static int ToWeekdayOffset(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            DayOfWeek.Sunday => 6,
            _ => 0
        };

        [HttpGet("events")]
        public IActionResult GetEvents()
        {
            var result = new List<EventItemResponse>
            {
                new() { Id = 1, Title = "Họp phụ huynh", Description = "Họp phụ huynh đầu học kỳ", EventDate = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"), Location = "Phòng A101" },
                new() { Id = 2, Title = "Ngày hội thể thao", Description = "Thi đấu thể thao toàn trường", EventDate = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"), Location = "Sân trường" },
                new() { Id = 3, Title = "Thi giữa kỳ", Description = "Lịch thi giữa kỳ HK2", EventDate = DateTime.Now.AddDays(10).ToString("yyyy-MM-dd"), Location = "Theo phòng thi" },
            };

            return Ok(result);
        }

        [HttpGet("clubs")]
        public IActionResult GetClubs()
        {
            var result = new List<ClubItemResponse>
            {
                new() { Id = 1, Name = "CLB Bóng đá", Description = "Rèn luyện thể lực và kỹ năng đồng đội", Schedule = "Thứ 3 - 16:30" },
                new() { Id = 2, Name = "CLB Âm nhạc", Description = "Sinh hoạt guitar, hát và biểu diễn", Schedule = "Thứ 5 - 16:30" },
                new() { Id = 3, Name = "CLB STEM", Description = "Lập trình, robot và sáng tạo", Schedule = "Thứ 7 - 08:00" },
            };

            return Ok(result);
        }



        private async Task<Student?> GetCurrentStudentAsync()
        {
            var phoneNumber = User.Claims.FirstOrDefault(x => x.Type == "phoneNumber")?.Value;
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;

            var user = await _context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (user == null)
                return null;

            return await _context.Students
                .Include(x => x.Class)
                .Include(x => x.StudentParents)
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id ||
                    x.StudentParents.Any(sp => sp.ParentUserId == user.Id));
        }
    }
}