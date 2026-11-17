using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;

namespace PRM_PRJ.Pages.Admin
{
    public class GradesModel : AdminPageModelBase
    {
        private readonly AppDbContext _context;

        public GradesModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? ClassId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SubjectId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SemesterId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public List<Class> Classes { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<Semester> Semesters { get; set; } = new();
        public List<ManagementGradeItemResponse> Grades { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostUpdateAsync(int gradeId, double newScore, string? newGrade)
        {
            var entity = await _context.Scores.FirstOrDefaultAsync(x => x.Id == gradeId);
            if (entity == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bản ghi điểm.";
                return RedirectToPage(new { ClassId, SubjectId, SemesterId, Search });
            }

            if (newScore < 0 || newScore > 10)
            {
                TempData["ErrorMessage"] = "Điểm phải nằm trong khoảng 0 đến 10.";
                return RedirectToPage(new { ClassId, SubjectId, SemesterId, Search });
            }

            var currentScore = entity.Value ?? 0;
            if (newScore < currentScore)
            {
                TempData["ErrorMessage"] = $"Điểm mới ({newScore}) không được nhỏ hơn điểm hiện tại ({currentScore}).";
                return RedirectToPage(new { ClassId, SubjectId, SemesterId, Search });
            }

            entity.Value = Math.Round(newScore, 1);
            entity.Grade = string.IsNullOrWhiteSpace(newGrade) ? ToGrade(entity.Value.Value) : newGrade.Trim();

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật điểm thành công.";
            return RedirectToPage(new { ClassId, SubjectId, SemesterId, Search, PageNumber });
        }

        private async Task LoadDataAsync()
        {
            Classes = await _context.Classes.OrderBy(x => x.Name).ToListAsync();
            Subjects = await _context.Subjects.OrderBy(x => x.Name).ToListAsync();
            Semesters = await _context.Semesters.Include(x => x.SchoolYear).OrderBy(x => x.SchoolYear.Name).ThenBy(x => x.Name).ToListAsync();

            var query = _context.Scores
                .Include(x => x.Student).ThenInclude(s => s.Class)
                .Include(x => x.Subject)
                .Include(x => x.Semester).ThenInclude(s => s.SchoolYear)
                .AsQueryable();

            if (ClassId.HasValue)
                query = query.Where(x => x.Student.ClassId == ClassId.Value);

            if (SubjectId.HasValue)
                query = query.Where(x => x.SubjectId == SubjectId.Value);

            if (SemesterId.HasValue)
                query = query.Where(x => x.SemesterId == SemesterId.Value);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Student.StudentCode.ToLower().Contains(term) ||
                    x.Student.FullName.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();
            ComputePagination(totalCount, 15);

            Grades = await query
                .OrderBy(x => x.Student.Class.Name)
                .ThenBy(x => x.Student.StudentCode)
                .ThenBy(x => x.Subject.Name)
                .ThenBy(x => x.Type)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
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
        }

        private static string ToGrade(double score) => score switch
        {
            >= 8.0 => "Gioi",
            >= 6.5 => "Kha",
            >= 5.0 => "Trung binh",
            _ => "Yeu"
        };
    }
}
