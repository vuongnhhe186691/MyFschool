using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Request;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Enums;
using PRM_PRJ.Models;
using System.Globalization;
using System.Security.Claims;

namespace PRM_PRJ.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    [ApiController]
    [Route("api/academic-management")]
    public class AcademicManagementController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AcademicManagementController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("lookups")]
        public async Task<IActionResult> GetLookups()
        {
            Teacher? teacher = null;
            if (!User.IsInRole("Admin"))
            {
                teacher = await GetCurrentTeacherAsync();
                if (teacher == null)
                    return Forbid();
            }

            var studentsQuery = _context.Students
                .Include(x => x.Class)
                .AsQueryable();
            var subjectsQuery = _context.Subjects.AsQueryable();
            var classesQuery = _context.Classes.AsQueryable();
            var teachersQuery = _context.Teachers.AsQueryable();

            if (teacher != null)
            {
                var teacherId = teacher.Id;
                classesQuery = classesQuery.Where(c =>
                    c.HomeroomTeacherId == teacherId ||
                    _context.Timetables.Any(t => t.TeacherId == teacherId && t.ClassId == c.Id));

                studentsQuery = studentsQuery.Where(s =>
                    s.Class.HomeroomTeacherId == teacherId ||
                    _context.Timetables.Any(t => t.TeacherId == teacherId && t.ClassId == s.ClassId));

                subjectsQuery = subjectsQuery.Where(s =>
                    _context.Timetables.Any(t => t.TeacherId == teacherId && t.SubjectId == s.Id) ||
                    _context.TeacherSubjects.Any(ts => ts.TeacherId == teacherId && ts.SubjectId == s.Id));

                teachersQuery = teachersQuery.Where(t => t.Id == teacherId);
            }

            var response = new AcademicLookupResponse
            {
                Students = await studentsQuery
                    .OrderBy(x => x.Class.Name)
                    .ThenBy(x => x.StudentCode)
                    .Select(x => new LookupOptionResponse
                    {
                        Value = x.Id.ToString(),
                        Label = $"{x.Class.Name} - {x.StudentCode} - {x.FullName}"
                    })
                    .ToListAsync(),

                Subjects = await subjectsQuery
                    .OrderBy(x => x.Name)
                    .Select(x => new LookupOptionResponse
                    {
                        Value = x.Id.ToString(),
                        Label = x.Name
                    })
                    .ToListAsync(),

                Semesters = await _context.Semesters
                    .Include(x => x.SchoolYear)
                    .OrderBy(x => x.SchoolYear.Name)
                    .ThenBy(x => x.Name)
                    .Select(x => new LookupOptionResponse
                    {
                        Value = x.Id.ToString(),
                        Label = $"{x.SchoolYear.Name} - {x.Name}"
                    })
                    .ToListAsync(),

                Classes = await classesQuery
                    .OrderBy(x => x.Name)
                    .Select(x => new LookupOptionResponse
                    {
                        Value = x.Id.ToString(),
                        Label = x.Name
                    })
                    .ToListAsync(),

                Teachers = await teachersQuery
                    .OrderBy(x => x.FullName)
                    .Select(x => new LookupOptionResponse
                    {
                        Value = x.Id.ToString(),
                        Label = x.FullName
                    })
                    .ToListAsync()
            };

            return Ok(response);
        }

        [HttpGet("grades")]
        public async Task<IActionResult> GetGrades()
        {
            var query = _context.Scores
                .Include(x => x.Student)
                    .ThenInclude(s => s.Class)
                .Include(x => x.Subject)
                .Include(x => x.Semester)
                    .ThenInclude(x => x.SchoolYear)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var teacher = await GetCurrentTeacherAsync();
                if (teacher == null)
                    return Forbid();

                var teacherId = teacher.Id;
                query = query.Where(x => _context.Timetables.Any(t =>
                    t.TeacherId == teacherId &&
                    t.ClassId == x.Student.ClassId &&
                    t.SubjectId == x.SubjectId));
            }

            var items = await query
                .OrderBy(x => x.Student.Class.Name)
                .ThenBy(x => x.Student.StudentCode)
                .ThenBy(x => x.Subject.Name)
                .ThenBy(x => x.Type)
                .Select(x => new ManagementGradeItemResponse
                {
                    GradeId = x.Id,
                    StudentId = x.StudentId,
                    StudentCode = x.Student.StudentCode,
                    StudentFullName = x.Student.FullName,
                    ClassId = x.Student.ClassId,
                    ClassName = x.Student.Class.Name,
                    SubjectId = x.SubjectId,
                    SubjectName = x.Subject.Name,
                    SemesterId = x.SemesterId,
                    Semester = x.Semester.Name,
                    AcademicYear = x.Semester.SchoolYear.Name,
                    AssessmentType = x.Type.ToString(),
                    Score = x.Value ?? 0,
                    Grade = x.Grade,
                    CreatedAt = x.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("grades")]
        public IActionResult CreateGrade([FromBody] UpsertGradeRequest request)
        {
            return BadRequest(new
            {
                message = "Chức năng này đã bị khóa. Chỉ được sửa điểm đã có sẵn."
            });
        }

        [HttpPut("grades/{id:int}")]
        public async Task<IActionResult> UpdateGrade(int id, [FromBody] UpsertGradeRequest request)
        {
            var entity = await _context.Scores
                .Include(x => x.Student)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return NotFound(new { message = "Không tìm thấy bản ghi điểm" });
            }

            if (!User.IsInRole("Admin"))
            {
                var teacher = await GetCurrentTeacherAsync();
                if (teacher == null)
                    return Forbid();

                var teachesClassSubject = await _context.Timetables.AnyAsync(t =>
                    t.TeacherId == teacher.Id &&
                    t.ClassId == entity.Student.ClassId &&
                    t.SubjectId == entity.SubjectId);

                if (!teachesClassSubject || request.SubjectId != entity.SubjectId)
                {
                    return StatusCode(403, new
                    {
                        message = "Bạn chỉ được sửa điểm những môn và học sinh thuộc lớp mình trực tiếp giảng dạy"
                    });
                }
            }

            if (!Enum.TryParse<ScoreType>(request.AssessmentType, true, out var scoreType))
            {
                return BadRequest(new { message = "AssessmentType không hợp lệ. Dùng: Regular, Midterm, Final" });
            }

            var subjectExists = await _context.Subjects.AnyAsync(x => x.Id == request.SubjectId);
            var semesterExists = await _context.Semesters.AnyAsync(x => x.Id == request.SemesterId);

            if (!subjectExists || !semesterExists)
            {
                return BadRequest(new { message = "Subject hoặc Semester không tồn tại" });
            }

            var currentScore = entity.Value ?? 0;
            if (request.Score < currentScore)
            {
                return BadRequest(new
                {
                    message = $"Điểm mới ({request.Score}) không được nhỏ hơn điểm hiện tại ({currentScore})"
                });
            }

            if (request.StudentId != entity.StudentId)
            {
                return BadRequest(new { message = "Không được đổi học sinh của bản ghi điểm" });
            }

            entity.SubjectId = request.SubjectId;
            entity.SemesterId = request.SemesterId;
            entity.Type = scoreType;
            entity.Value = request.Score;
            entity.Grade = request.Grade;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật điểm thành công" });
        }

        [HttpDelete("grades/{id:int}")]
        public IActionResult DeleteGrade(int id)
        {
            return BadRequest(new
            {
                message = "Chức năng xóa điểm đã bị khóa."
            });
        }

        [HttpGet("timetables")]
        public async Task<IActionResult> GetTimetables([FromQuery] string? weekStartDate)
        {
            var query = _context.Timetables
                .Include(x => x.Class)
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var teacher = await GetCurrentTeacherAsync();
                if (teacher == null)
                    return Forbid();

                query = query.Where(x => x.TeacherId == teacher.Id);
            }

            if (!string.IsNullOrWhiteSpace(weekStartDate))
            {
                if (!DateTime.TryParse(weekStartDate, out var weekDate))
                {
                    return BadRequest(new { message = "weekStartDate không hợp lệ. Dùng yyyy-MM-dd" });
                }

                var normalizedWeek = weekDate.Date;
                query = query.Where(x => x.WeekStartDate.Date == normalizedWeek);
            }

            var items = await query
                .OrderBy(x => x.WeekStartDate)
                .ThenBy(x => x.Class.Name)
                .ThenBy(x => x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .Select(x => new ManagementTimetableItemResponse
                {
                    Id = x.Id,
                    ClassId = x.ClassId,
                    ClassName = x.Class.Name,
                    SubjectId = x.SubjectId,
                    SubjectName = x.Subject.Name,
                    TeacherId = x.TeacherId,
                    TeacherName = x.Teacher.FullName,
                    DayOfWeek = x.DayOfWeek.ToString(),
                    StartTime = x.StartTime.ToString(@"hh\:mm"),
                    EndTime = x.EndTime.ToString(@"hh\:mm"),
                    WeekStartDate = x.WeekStartDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Ok(items);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("timetables")]
        public async Task<IActionResult> CreateTimetable([FromBody] UpsertTimetableRequest request)
        {
            var validation = await ValidateTimetableRequest(request);
            if (validation is not null) return validation;

            var dayOfWeek = Enum.Parse<DayOfWeek>(request.DayOfWeek, true);
            var startTime = TimeSpan.Parse(request.StartTime);
            var endTime = TimeSpan.Parse(request.EndTime);
            var weekStartDateValue = DateTime.Parse(request.WeekStartDate).Date;

            var duplicated = await _context.Timetables.AnyAsync(x =>
                x.ClassId == request.ClassId &&
                x.WeekStartDate.Date == weekStartDateValue &&
                x.DayOfWeek == dayOfWeek &&
                (
                    (startTime >= x.StartTime && startTime < x.EndTime) ||
                    (endTime > x.StartTime && endTime <= x.EndTime) ||
                    (startTime <= x.StartTime && endTime >= x.EndTime)
                )
            );

            if (duplicated)
            {
                return BadRequest(new
                {
                    message = "Lớp này đã có lịch bị trùng thời gian trong tuần này"
                });
            }

            var entity = new Timetable
            {
                ClassId = request.ClassId,
                SubjectId = request.SubjectId,
                TeacherId = request.TeacherId,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                WeekStartDate = weekStartDateValue
            };

            _context.Timetables.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tạo lịch học thành công" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("timetables/import")]
        public async Task<IActionResult> ImportTimetables([FromBody] List<ImportTimetableRowRequest> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return BadRequest(new { message = "File import không có dữ liệu" });
            }

            var errors = new List<string>();
            var entities = new List<Timetable>();

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                if (!Enum.TryParse<DayOfWeek>(row.DayOfWeek, true, out var dayOfWeek))
                {
                    errors.Add($"Dòng {i + 1}: DayOfWeek không hợp lệ");
                    continue;
                }

                if (!TimeSpan.TryParse(row.StartTime, out var startTime) ||
                    !TimeSpan.TryParse(row.EndTime, out var endTime))
                {
                    errors.Add($"Dòng {i + 1}: StartTime hoặc EndTime không hợp lệ");
                    continue;
                }

                DateTime weekStartDate;
                var acceptedFormats = new[]
                {
                    "yyyy-MM-dd",
                    "M/d/yyyy",
                    "MM/dd/yyyy",
                    "d/M/yyyy",
                    "dd/MM/yyyy"
                };

                if (!DateTime.TryParseExact(
                        row.WeekStartDate?.Trim(),
                        acceptedFormats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out weekStartDate) &&
                    !DateTime.TryParse(row.WeekStartDate, out weekStartDate))
                {
                    errors.Add($"Dòng {i + 1}: WeekStartDate không hợp lệ");
                    continue;
                }

                weekStartDate = weekStartDate.Date;

                if (startTime >= endTime)
                {
                    errors.Add($"Dòng {i + 1}: StartTime phải nhỏ hơn EndTime");
                    continue;
                }

                var classExists = await _context.Classes.AnyAsync(x => x.Id == row.ClassId);
                var subjectExists = await _context.Subjects.AnyAsync(x => x.Id == row.SubjectId);
                var teacherExists = await _context.Teachers.AnyAsync(x => x.Id == row.TeacherId);

                if (!classExists || !subjectExists || !teacherExists)
                {
                    errors.Add($"Dòng {i + 1}: Class, Subject hoặc Teacher không tồn tại");
                    continue;
                }

                var duplicatedInDb = await _context.Timetables.AnyAsync(x =>
                    x.ClassId == row.ClassId &&
                    x.WeekStartDate.Date == weekStartDate &&
                    x.DayOfWeek == dayOfWeek &&
                    (
                        (startTime >= x.StartTime && startTime < x.EndTime) ||
                        (endTime > x.StartTime && endTime <= x.EndTime) ||
                        (startTime <= x.StartTime && endTime >= x.EndTime)
                    )
                );

                if (duplicatedInDb)
                {
                    errors.Add($"Dòng {i + 1}: Trùng lịch với dữ liệu đã có trong tuần này");
                    continue;
                }

                var duplicatedInFile = entities.Any(x =>
                    x.ClassId == row.ClassId &&
                    x.WeekStartDate.Date == weekStartDate &&
                    x.DayOfWeek == dayOfWeek &&
                    (
                        (startTime >= x.StartTime && startTime < x.EndTime) ||
                        (endTime > x.StartTime && endTime <= x.EndTime) ||
                        (startTime <= x.StartTime && endTime >= x.EndTime)
                    )
                );

                if (duplicatedInFile)
                {
                    errors.Add($"Dòng {i + 1}: Trùng lịch trong file import");
                    continue;
                }

                entities.Add(new Timetable
                {
                    ClassId = row.ClassId,
                    SubjectId = row.SubjectId,
                    TeacherId = row.TeacherId,
                    DayOfWeek = dayOfWeek,
                    StartTime = startTime,
                    EndTime = endTime,
                    WeekStartDate = weekStartDate
                });
            }

            if (errors.Count > 0)
            {
                return BadRequest(new
                {
                    message = "Import thất bại",
                    errors
                });
            }

            _context.Timetables.AddRange(entities);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Import thành công {entities.Count} lịch học"
            });
        }

        [HttpPut("timetables/{id:int}")]
        public IActionResult UpdateTimetable(int id, [FromBody] UpsertTimetableRequest request)
        {
            return BadRequest(new
            {
                message = "Chức năng sửa lịch học đã bị khóa. Chỉ được thêm mới hoặc import file."
            });
        }

        [HttpDelete("timetables/{id:int}")]
        public IActionResult DeleteTimetable(int id)
        {
            return BadRequest(new
            {
                message = "Chức năng xóa lịch học đã bị khóa."
            });
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetStudents([FromQuery] int? classId)
        {
            var query = _context.Students
                .Include(x => x.Class)
                .Include(x => x.StudentParents)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var teacher = await GetCurrentTeacherAsync();
                if (teacher == null)
                    return Forbid();

                var teacherId = teacher.Id;
                query = query.Where(x =>
                    x.Class.HomeroomTeacherId == teacherId ||
                    _context.Timetables.Any(t => t.TeacherId == teacherId && t.ClassId == x.ClassId));
            }

            if (classId.HasValue)
            {
                query = query.Where(x => x.ClassId == classId.Value);
            }

            var items = await query
                .OrderBy(x => x.Class.Name)
                .ThenBy(x => x.StudentCode)
                .Select(x => new ManagementStudentItemResponse
                {
                    Id = x.Id.ToString(),
                    FullName = x.FullName,
                    DateOfBirth = x.DateOfBirth.ToString("yyyy-MM-dd"),
                    Gender = x.Gender,
                    ClassId = x.ClassId,
                    ClassName = x.Class.Name,
                    StudentCode = x.StudentCode,
                    UserId = x.UserId.HasValue ? x.UserId.Value.ToString() : null,
                    ParentIds = x.StudentParents
                        .Select(sp => sp.ParentUserId.ToString())
                        .ToList(),
                    AvatarUrl = x.AvatarUrl,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            return Ok(items);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("students/assign-class")]
        public async Task<IActionResult> AssignStudentsToClass([FromBody] AssignStudentsToClassRequest request)
        {
            if (request.StudentIds == null || request.StudentIds.Count == 0)
            {
                return BadRequest(new { message = "Danh sách học sinh trống" });
            }

            var targetClass = await _context.Classes.FirstOrDefaultAsync(x => x.Id == request.TargetClassId);
            if (targetClass == null)
            {
                return BadRequest(new { message = "Lớp đích không tồn tại" });
            }

            var students = await _context.Students
                .Where(x => request.StudentIds.Contains(x.Id))
                .ToListAsync();

            if (students.Count != request.StudentIds.Count)
            {
                return BadRequest(new { message = "Có học sinh không tồn tại trong hệ thống" });
            }

            foreach (var student in students)
            {
                student.ClassId = request.TargetClassId;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Đã chuyển {students.Count} học sinh sang lớp {targetClass.Name}"
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("students/import-assign-class")]
        public async Task<IActionResult> ImportAssignStudentsToClass([FromBody] List<ImportAssignStudentRowRequest> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return BadRequest(new { message = "File import không có dữ liệu" });
            }

            var errors = new List<string>();
            var updatedCount = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                Student? student = null;

                if (row.TargetClassId <= 0)
                {
                    errors.Add($"Dòng {i + 1}: TargetClassId không hợp lệ");
                    continue;
                }

                var targetClassExists = await _context.Classes.AnyAsync(x => x.Id == row.TargetClassId);
                if (!targetClassExists)
                {
                    errors.Add($"Dòng {i + 1}: Lớp đích không tồn tại");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(row.StudentId))
                {
                    if (!Guid.TryParse(row.StudentId, out var parsedStudentId))
                    {
                        errors.Add($"Dòng {i + 1}: StudentId không hợp lệ");
                        continue;
                    }

                    student = await _context.Students.FirstOrDefaultAsync(x => x.Id == parsedStudentId);
                }
                else if (!string.IsNullOrWhiteSpace(row.StudentCode))
                {
                    var code = row.StudentCode.Trim();
                    student = await _context.Students.FirstOrDefaultAsync(x => x.StudentCode == code);
                }
                else
                {
                    errors.Add($"Dòng {i + 1}: Cần StudentId hoặc StudentCode");
                    continue;
                }

                if (student == null)
                {
                    errors.Add($"Dòng {i + 1}: Không tìm thấy học sinh");
                    continue;
                }

                student.ClassId = row.TargetClassId;
                updatedCount++;
            }

            if (errors.Count > 0)
            {
                return BadRequest(new
                {
                    message = "Import chuyển lớp thất bại",
                    errors
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Import chuyển lớp thành công {updatedCount} học sinh"
            });
        }

        private async Task<string?> ValidateStudentRequest(UpsertStudentRequest request, Guid? studentId)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return "FullName không được để trống";

            if (string.IsNullOrWhiteSpace(request.StudentCode))
                return "StudentCode không được để trống";

            if (!DateTime.TryParse(request.DateOfBirth, out _))
                return "DateOfBirth không hợp lệ. Dùng yyyy-MM-dd";

            if (!await _context.Classes.AnyAsync(x => x.Id == request.ClassId))
                return "Class không tồn tại";

            var duplicatedCode = await _context.Students.AnyAsync(x =>
                x.StudentCode == request.StudentCode.Trim() &&
                (!studentId.HasValue || x.Id != studentId.Value));

            if (duplicatedCode)
                return "StudentCode đã tồn tại";

            if (request.UserId.HasValue &&
                !await _context.Users.AnyAsync(x => x.Id == request.UserId.Value))
                return "UserId không tồn tại";

            if (request.ParentIds != null && request.ParentIds.Count > 0)
            {
                var distinctParentIds = request.ParentIds
                    .Where(x => x != Guid.Empty)
                    .Distinct()
                    .ToList();

                var existingParentIds = await _context.Users
                    .Where(x => distinctParentIds.Contains(x.Id))
                    .Select(x => x.Id)
                    .ToListAsync();

                var missingParentIds = distinctParentIds.Except(existingParentIds).ToList();
                if (missingParentIds.Any())
                    return "Có ParentId không tồn tại";

                var validParentUserIds = await _context.UserRoles
                    .Include(x => x.Role)
                    .Where(x => distinctParentIds.Contains(x.UserId) && x.Role.Name == "Parent")
                    .Select(x => x.UserId)
                    .Distinct()
                    .ToListAsync();

                var invalidParentIds = distinctParentIds.Except(validParentUserIds).ToList();
                if (invalidParentIds.Any())
                    return "Có user chưa được gán role Parent";
            }

            return null;
        }

        private async Task<IActionResult?> ValidateTimetableRequest(UpsertTimetableRequest request)
        {
            if (!Enum.TryParse<DayOfWeek>(request.DayOfWeek, true, out _))
            {
                return BadRequest(new { message = "DayOfWeek không hợp lệ" });
            }

            if (!TimeSpan.TryParse(request.StartTime, out var startTime) ||
                !TimeSpan.TryParse(request.EndTime, out var endTime))
            {
                return BadRequest(new { message = "StartTime hoặc EndTime không hợp lệ. Dùng HH:mm" });
            }

            DateTime parsedWeek;
            var acceptedFormats = new[]
            {
                "yyyy-MM-dd",
                "M/d/yyyy",
                "MM/dd/yyyy",
                "d/M/yyyy",
                "dd/MM/yyyy"
            };

            if (!DateTime.TryParseExact(
                    request.WeekStartDate?.Trim(),
                    acceptedFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsedWeek) &&
                !DateTime.TryParse(request.WeekStartDate, out parsedWeek))
            {
                return BadRequest(new { message = "WeekStartDate không hợp lệ" });
            }

            if (startTime >= endTime)
            {
                return BadRequest(new { message = "StartTime phải nhỏ hơn EndTime" });
            }

            var classExists = await _context.Classes.AnyAsync(x => x.Id == request.ClassId);
            var subjectExists = await _context.Subjects.AnyAsync(x => x.Id == request.SubjectId);
            var teacherExists = await _context.Teachers.AnyAsync(x => x.Id == request.TeacherId);

            if (!classExists || !subjectExists || !teacherExists)
            {
                return BadRequest(new { message = "Class, Subject hoặc Teacher không tồn tại" });
            }

            return null;
        }

        private async Task<Teacher?> GetCurrentTeacherAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return null;

            return await _context.Teachers.FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}