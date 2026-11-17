using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Request;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;

namespace PRM_PRJ.Pages.Admin
{
    public class TimetablesModel : AdminPageModelBase
    {
        private readonly AppDbContext _context;

        public TimetablesModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? ClassId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? WeekStartDate { get; set; }

        [BindProperty]
        public UpsertTimetableRequest Input { get; set; } = new();

        public List<Class> Classes { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<Teacher> Teachers { get; set; } = new();
        public List<string> AvailableWeeks { get; set; } = new();
        public List<ManagementTimetableItemResponse> Timetables { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!Enum.TryParse<DayOfWeek>(Input.DayOfWeek, true, out var dayOfWeek))
            {
                TempData["ErrorMessage"] = "Thứ trong tuần không hợp lệ.";
                return RedirectToPage();
            }

            if (!TimeSpan.TryParse(Input.StartTime, out var startTime) ||
                !TimeSpan.TryParse(Input.EndTime, out var endTime) ||
                startTime >= endTime)
            {
                TempData["ErrorMessage"] = "Giờ bắt đầu/kết thúc không hợp lệ (StartTime phải nhỏ hơn EndTime).";
                return RedirectToPage();
            }

            if (!DateTime.TryParse(Input.WeekStartDate, out var weekDate))
            {
                TempData["ErrorMessage"] = "WeekStartDate không hợp lệ.";
                return RedirectToPage();
            }

            var normalizedWeek = weekDate.Date;

            var duplicated = await _context.Timetables.AnyAsync(x =>
                x.ClassId == Input.ClassId &&
                x.WeekStartDate.Date == normalizedWeek &&
                x.DayOfWeek == dayOfWeek &&
                ((startTime >= x.StartTime && startTime < x.EndTime) ||
                 (endTime > x.StartTime && endTime <= x.EndTime) ||
                 (startTime <= x.StartTime && endTime >= x.EndTime)));

            if (duplicated)
            {
                TempData["ErrorMessage"] = "Lớp này đã có lịch trùng thời gian trong tuần được chọn.";
                return RedirectToPage();
            }

            _context.Timetables.Add(new Timetable
            {
                ClassId = Input.ClassId,
                SubjectId = Input.SubjectId,
                TeacherId = Input.TeacherId,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                WeekStartDate = normalizedWeek
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tạo lịch học mới thành công.";
            return RedirectToPage(new { ClassId = Input.ClassId });
        }

        public async Task<IActionResult> OnGetDownloadTemplateAsync()
        {
            var firstClass = await _context.Classes.OrderBy(x => x.Name).Select(x => x.Name).FirstOrDefaultAsync() ?? "10A1";
            var firstSubject = await _context.Subjects.OrderBy(x => x.Name).Select(x => x.Name).FirstOrDefaultAsync() ?? "Toán";
            var firstTeacher = await _context.Teachers.OrderBy(x => x.FullName).Select(x => x.FullName).FirstOrDefaultAsync() ?? "Nguyen Van An";
            var latestWeek = await _context.Timetables
                .Where(x => x.WeekStartDate > new DateTime(2000, 1, 1))
                .OrderByDescending(x => x.WeekStartDate)
                .Select(x => x.WeekStartDate)
                .FirstOrDefaultAsync();
            var weekValue = (latestWeek > new DateTime(2000, 1, 1) ? latestWeek : DateTime.Today).ToString("yyyy-MM-dd");

            var rows = new List<string[]>
            {
                new[] { "Class", "Subject", "Teacher", "DayOfWeek", "StartTime", "EndTime", "WeekStartDate" },
                new[] { firstClass, firstSubject, firstTeacher, "Monday", "10:20", "11:05", weekValue }
            };

            var bytes = ExcelHelper.BuildXlsx("Timetable", rows);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Timetable_Template.xlsx");
        }

        public async Task<IActionResult> OnPostImportAsync(IFormFile? excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file Excel (.xlsx hoặc .csv) để import.";
                return RedirectToPage();
            }

            List<List<string>> rows;
            try
            {
                using var stream = excelFile.OpenReadStream();
                rows = ExcelHelper.ReadRows(stream, excelFile.FileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Không đọc được file: {ex.Message}";
                return RedirectToPage();
            }

            if (rows.Count == 0)
            {
                TempData["ErrorMessage"] = "File Excel không có dữ liệu.";
                return RedirectToPage();
            }

            var startIndex = 0;
            if (rows[0].Count > 0 &&
                (rows[0][0].Equals("Class", StringComparison.OrdinalIgnoreCase) ||
                 rows[0][0].Equals("ClassId", StringComparison.OrdinalIgnoreCase)))
            {
                startIndex = 1;
            }

            if (startIndex >= rows.Count)
            {
                TempData["ErrorMessage"] = "File Excel chỉ có dòng tiêu đề, chưa có dữ liệu.";
                return RedirectToPage();
            }

            var classes = await _context.Classes.ToListAsync();
            var subjects = await _context.Subjects.ToListAsync();
            var teachers = await _context.Teachers.ToListAsync();
            var entities = new List<Timetable>();

            for (int i = startIndex; i < rows.Count; i++)
            {
                var cols = rows[i];
                var rowNo = i + 1;
                if (cols.Count < 7)
                {
                    TempData["ErrorMessage"] = $"Dòng {rowNo}: Cần đủ 7 cột (Class, Subject, Teacher, DayOfWeek, StartTime, EndTime, WeekStartDate).";
                    return RedirectToPage();
                }

                var classToken = cols[0].Trim();
                var subjectToken = cols[1].Trim();
                var teacherToken = cols[2].Trim();
                var dayToken = cols[3].Trim();
                var startToken = cols[4].Trim();
                var endToken = cols[5].Trim();
                var weekToken = cols[6].Trim();

                var cls = int.TryParse(classToken, out var cid)
                    ? classes.FirstOrDefault(x => x.Id == cid)
                    : classes.FirstOrDefault(x => x.Name.Equals(classToken, StringComparison.OrdinalIgnoreCase));
                if (cls == null)
                {
                    TempData["ErrorMessage"] = $"Dòng {rowNo}: Không tìm thấy lớp '{classToken}'.";
                    return RedirectToPage();
                }

                var subject = int.TryParse(subjectToken, out var sid)
                    ? subjects.FirstOrDefault(x => x.Id == sid)
                    : subjects.FirstOrDefault(x => x.Name.Equals(subjectToken, StringComparison.OrdinalIgnoreCase));
                if (subject == null)
                {
                    TempData["ErrorMessage"] = $"Dòng {rowNo}: Không tìm thấy môn học '{subjectToken}'.";
                    return RedirectToPage();
                }

                var teacher = Guid.TryParse(teacherToken, out var tid)
                    ? teachers.FirstOrDefault(x => x.Id == tid)
                    : teachers.FirstOrDefault(x => x.FullName.Equals(teacherToken, StringComparison.OrdinalIgnoreCase));
                if (teacher == null)
                {
                    TempData["ErrorMessage"] = $"Dòng {rowNo}: Không tìm thấy giáo viên '{teacherToken}'.";
                    return RedirectToPage();
                }

                if (!Enum.TryParse<DayOfWeek>(dayToken, true, out var dayOfWeek) ||
                    !ExcelHelper.TryParseExcelTime(startToken, out var startTime) ||
                    !ExcelHelper.TryParseExcelTime(endToken, out var endTime) ||
                    startTime >= endTime ||
                    !ExcelHelper.TryParseExcelDate(weekToken, out var normalizedWeek))
                {
                    TempData["ErrorMessage"] = $"Dòng {rowNo}: Thứ, giờ học hoặc ngày đầu tuần không hợp lệ.";
                    return RedirectToPage();
                }

                var duplicatedInDb = await _context.Timetables.AnyAsync(x =>
                    x.ClassId == cls.Id &&
                    x.WeekStartDate.Date == normalizedWeek &&
                    x.DayOfWeek == dayOfWeek &&
                    ((startTime >= x.StartTime && startTime < x.EndTime) ||
                     (endTime > x.StartTime && endTime <= x.EndTime) ||
                     (startTime <= x.StartTime && endTime >= x.EndTime)));

                if (duplicatedInDb)
                {
                    TempData["ErrorMessage"] = $"Dòng {rowNo}: Trùng lịch với dữ liệu đã có trong hệ thống.";
                    return RedirectToPage();
                }

                var duplicatedInFile = entities.Any(x =>
                    x.ClassId == cls.Id &&
                    x.WeekStartDate == normalizedWeek &&
                    x.DayOfWeek == dayOfWeek &&
                    ((startTime >= x.StartTime && startTime < x.EndTime) ||
                     (endTime > x.StartTime && endTime <= x.EndTime) ||
                     (startTime <= x.StartTime && endTime >= x.EndTime)));

                if (duplicatedInFile)
                {
                    TempData["ErrorMessage"] = $"Dòng {rowNo}: Bị trùng giờ với một dòng khác ngay trong file Excel.";
                    return RedirectToPage();
                }

                entities.Add(new Timetable
                {
                    ClassId = cls.Id,
                    SubjectId = subject.Id,
                    TeacherId = teacher.Id,
                    DayOfWeek = dayOfWeek,
                    StartTime = startTime,
                    EndTime = endTime,
                    WeekStartDate = normalizedWeek
                });
            }

            _context.Timetables.AddRange(entities);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Import thành công {entities.Count} tiết học từ Excel.";
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            Classes = await _context.Classes.OrderBy(x => x.Name).ToListAsync();
            Subjects = await _context.Subjects.OrderBy(x => x.Name).ToListAsync();
            Teachers = await _context.Teachers.OrderBy(x => x.FullName).ToListAsync();
            AvailableWeeks = (await _context.Timetables
                .Where(x => x.WeekStartDate > new DateTime(2000, 1, 1))
                .Select(x => x.WeekStartDate.Date)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync())
                .Select(d => d.ToString("yyyy-MM-dd"))
                .ToList();

            var query = _context.Timetables
                .Include(x => x.Class)
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .AsQueryable();

            if (ClassId.HasValue)
                query = query.Where(x => x.ClassId == ClassId.Value);

            if (!string.IsNullOrWhiteSpace(WeekStartDate) && DateTime.TryParse(WeekStartDate, out var parsedWeek))
            {
                var d = parsedWeek.Date;
                query = query.Where(x => x.WeekStartDate.Date == d);
            }

            var totalCount = await query.CountAsync();
            ComputePagination(totalCount, 15);

            Timetables = await query
                .OrderBy(x => x.Class.Name)
                .ThenBy(x => x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
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
        }
    }
}
