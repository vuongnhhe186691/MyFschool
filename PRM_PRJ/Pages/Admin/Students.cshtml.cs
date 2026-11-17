using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;

namespace PRM_PRJ.Pages.Admin
{
    public class StudentsModel : AdminPageModelBase
    {
        private readonly AppDbContext _context;

        public StudentsModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? ClassId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty]
        public List<Guid> SelectedStudentIds { get; set; } = new();

        [BindProperty]
        public int TargetClassId { get; set; }

        public List<Class> Classes { get; set; } = new();
        public List<ManagementStudentItemResponse> Students { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostAssignClassAsync()
        {
            if (SelectedStudentIds == null || SelectedStudentIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 học sinh để chuyển lớp.";
                return RedirectToPage(new { ClassId, Search });
            }

            var targetClass = await _context.Classes.FirstOrDefaultAsync(x => x.Id == TargetClassId);
            if (targetClass == null)
            {
                TempData["ErrorMessage"] = "Lớp đích không tồn tại.";
                return RedirectToPage(new { ClassId, Search });
            }

            var students = await _context.Students
                .Where(x => SelectedStudentIds.Contains(x.Id))
                .ToListAsync();

            foreach (var student in students)
            {
                student.ClassId = TargetClassId;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã chuyển {students.Count} học sinh sang lớp {targetClass.Name}.";
            return RedirectToPage(new { ClassId, Search, PageNumber });
        }

        public async Task<IActionResult> OnGetDownloadTemplateAsync()
        {
            var sampleStudent = await _context.Students.OrderBy(x => x.StudentCode).Select(x => x.StudentCode).FirstOrDefaultAsync() ?? "10A1-001";
            var sampleClass = await _context.Classes.OrderBy(x => x.Name).Select(x => x.Name).Skip(1).FirstOrDefaultAsync() ?? "10A2";

            var rows = new List<string[]>
            {
                new[] { "StudentCode", "TargetClass" },
                new[] { sampleStudent, sampleClass }
            };

            var bytes = ExcelHelper.BuildXlsx("StudentTransfer", rows);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Student_Transfer_Template.xlsx");
        }

        public async Task<IActionResult> OnPostImportAssignAsync(IFormFile? excelFile)
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
                rows[0][0].Equals("StudentCode", StringComparison.OrdinalIgnoreCase))
            {
                startIndex = 1;
            }

            if (startIndex >= rows.Count)
            {
                TempData["ErrorMessage"] = "File Excel chỉ có dòng tiêu đề, chưa có dữ liệu.";
                return RedirectToPage();
            }

            var classes = await _context.Classes.ToListAsync();
            var updatedCount = 0;
            var errors = new List<string>();

            for (int i = startIndex; i < rows.Count; i++)
            {
                var cols = rows[i];
                var rowNo = i + 1;
                if (cols.Count < 2)
                {
                    errors.Add($"Dòng {rowNo}: Cần đủ 2 cột (StudentCode, TargetClass).");
                    continue;
                }

                var code = cols[0].Trim();
                var targetToken = cols[1].Trim();

                Class? targetClass = int.TryParse(targetToken, out var parsedClassId)
                    ? classes.FirstOrDefault(c => c.Id == parsedClassId)
                    : classes.FirstOrDefault(c => string.Equals(c.Name, targetToken, StringComparison.OrdinalIgnoreCase));

                if (targetClass == null)
                {
                    errors.Add($"Dòng {rowNo}: Lớp đích '{targetToken}' không tồn tại.");
                    continue;
                }

                var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentCode == code);
                if (student == null)
                {
                    errors.Add($"Dòng {rowNo}: Không tìm thấy học sinh '{code}'.");
                    continue;
                }

                student.ClassId = targetClass.Id;
                updatedCount++;
            }

            if (errors.Count > 0)
            {
                TempData["ErrorMessage"] = string.Join(" | ", errors.Take(5));
                return RedirectToPage();
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Import chuyển lớp thành công {updatedCount} học sinh từ Excel.";
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            Classes = await _context.Classes.OrderBy(x => x.Name).ToListAsync();

            var query = _context.Students
                .Include(x => x.Class)
                .Include(x => x.StudentParents)
                .AsQueryable();

            if (ClassId.HasValue)
            {
                query = query.Where(x => x.ClassId == ClassId.Value);
            }

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim().ToLower();
                query = query.Where(x =>
                    x.StudentCode.ToLower().Contains(term) ||
                    x.FullName.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();
            ComputePagination(totalCount, 12);

            Students = await query
                .OrderBy(x => x.Class.Name)
                .ThenBy(x => x.StudentCode)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
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
                    ParentIds = x.StudentParents.Select(sp => sp.ParentUserId.ToString()).ToList(),
                    AvatarUrl = x.AvatarUrl,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }
    }
}
