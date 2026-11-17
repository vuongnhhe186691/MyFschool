import 'package:flutter/material.dart';
import '../core/app_theme.dart';
import '../models/models.dart';
import '../services/api_service.dart';

class _ClassGradeGroup {
  final int classId;
  final String className;
  final List<String> subjectNames;
  final int recordCount;

  _ClassGradeGroup({
    required this.classId,
    required this.className,
    required this.subjectNames,
    required this.recordCount,
  });
}

class TeacherGradesScreen extends StatefulWidget {
  const TeacherGradesScreen({super.key});

  @override
  State<TeacherGradesScreen> createState() => _TeacherGradesScreenState();
}

class _TeacherGradesScreenState extends State<TeacherGradesScreen> {
  AcademicLookups? _lookups;
  List<ManagementGradeItem> _grades = [];
  bool _loading = true;
  String? _error;

  int? _selectedClassId;
  String _selectedClassName = '';
  int? _selectedSubjectId;
  int? _selectedSemesterId;
  String _search = '';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final results = await Future.wait([
        ApiService.instance.getLookups(),
        ApiService.instance.getManagementGrades(),
      ]);
      if (!mounted) return;
      setState(() {
        _lookups = results[0] as AcademicLookups;
        _grades = results[1] as List<ManagementGradeItem>;
        _loading = false;
      });
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString();
          _loading = false;
        });
      }
    }
  }

  List<_ClassGradeGroup> _buildClassGroups() {
    final byClass = <int, List<ManagementGradeItem>>{};
    for (final g in _grades) {
      byClass.putIfAbsent(g.classId, () => []).add(g);
    }

    final groups = byClass.entries.map((entry) {
      final list = entry.value;
      final subjects = list.map((e) => e.subjectName).toSet().toList()..sort();
      return _ClassGradeGroup(
        classId: entry.key,
        className: list.first.className,
        subjectNames: subjects,
        recordCount: list.length,
      );
    }).toList();

    groups.sort((a, b) => a.className.compareTo(b.className));
    return groups;
  }

  Future<void> _openEditDialog(ManagementGradeItem item) async {
    final scoreCtrl = TextEditingController(text: item.score.toString());
    final gradeCtrl = TextEditingController(text: item.grade ?? '');
    var saving = false;

    await showDialog<void>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          title: Text('Sửa điểm: ${item.studentFullName}'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                '${item.className} • ${item.subjectName} • ${item.assessmentType}',
                style: const TextStyle(fontSize: 12.5, color: AppColors.textMuted),
              ),
              const SizedBox(height: 6),
              Text(
                'Lưu ý: Điểm mới phải >= ${item.score}',
                style: const TextStyle(
                  fontSize: 12,
                  color: AppColors.orange,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: 14),
              TextField(
                controller: scoreCtrl,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: AppTheme.modernInput(
                  label: 'Điểm số (0 - 10)',
                  hint: 'Nhập điểm mới',
                  prefixIcon: Icons.grade_rounded,
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: gradeCtrl,
                decoration: AppTheme.modernInput(
                  label: 'Xếp loại',
                  hint: 'Gioi / Kha / Dat',
                  prefixIcon: Icons.workspace_premium_rounded,
                ),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: saving ? null : () => Navigator.of(ctx).pop(),
              child: const Text('Hủy'),
            ),
            ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.orange,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              onPressed: saving
                  ? null
                  : () async {
                      final parsed = double.tryParse(scoreCtrl.text.trim());
                      if (parsed == null || parsed < 0 || parsed > 10) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(
                            content: Text('Điểm không hợp lệ (0 - 10)'),
                            backgroundColor: AppColors.redFail,
                          ),
                        );
                        return;
                      }
                      setDialogState(() => saving = true);
                      try {
                        final msg = await ApiService.instance.updateGrade(
                          gradeId: item.gradeId,
                          studentId: item.studentId,
                          subjectId: item.subjectId,
                          semesterId: item.semesterId,
                          assessmentType: item.assessmentType,
                          score: parsed,
                          grade: gradeCtrl.text.trim().isEmpty
                              ? null
                              : gradeCtrl.text.trim(),
                        );
                        if (!ctx.mounted) return;
                        Navigator.of(ctx).pop();
                        if (mounted) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(msg),
                              backgroundColor: AppColors.greenPass,
                            ),
                          );
                          _load();
                        }
                      } catch (e) {
                        setDialogState(() => saving = false);
                        if (mounted) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(e.toString()),
                              backgroundColor: AppColors.redFail,
                            ),
                          );
                        }
                      }
                    },
              child: Text(saving ? 'Đang lưu...' : 'Cập nhật'),
            ),
          ],
        ),
      ),
    );
    scoreCtrl.dispose();
    gradeCtrl.dispose();
  }

  void _backToClassList() {
    setState(() {
      _selectedClassId = null;
      _selectedClassName = '';
      _selectedSubjectId = null;
      _selectedSemesterId = null;
      _search = '';
    });
  }

  @override
  Widget build(BuildContext context) {
    final classGroups = _buildClassGroups();
    final classGrades = _selectedClassId == null
        ? <ManagementGradeItem>[]
        : _grades.where((g) => g.classId == _selectedClassId).toList();

    final taughtSubjectsInClass = <int, String>{};
    for (final g in classGrades) {
      taughtSubjectsInClass[g.subjectId] = g.subjectName;
    }

    final filtered = classGrades.where((g) {
      if (_selectedSubjectId != null && g.subjectId != _selectedSubjectId) return false;
      if (_selectedSemesterId != null && g.semesterId != _selectedSemesterId) return false;
      if (_search.isNotEmpty) {
        final q = _search.toLowerCase();
        if (!g.studentFullName.toLowerCase().contains(q) &&
            !g.studentCode.toLowerCase().contains(q)) {
          return false;
        }
      }
      return true;
    }).toList();

    return PopScope(
      canPop: _selectedClassId == null,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop && _selectedClassId != null) {
          _backToClassList();
        }
      },
      child: Scaffold(
        appBar: AppBar(
          leading: _selectedClassId != null
              ? IconButton(
                  icon: const Icon(Icons.arrow_back_rounded),
                  onPressed: _backToClassList,
                )
              : null,
          title: Text(
            _selectedClassId == null
                ? 'Chọn lớp quản lý điểm'
                : 'Điểm lớp $_selectedClassName',
          ),
        ),
        body: _loading
            ? const Center(child: CircularProgressIndicator(color: AppColors.orange))
            : _error != null
                ? Center(child: Text(_error!, style: const TextStyle(color: AppColors.redFail)))
                : _selectedClassId == null
                    ? _buildClassSelector(classGroups)
                    : Column(
                        children: [
                          Padding(
                            padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
                            child: Column(
                              children: [
                                TextField(
                                  onChanged: (v) => setState(() => _search = v.trim()),
                                  decoration: AppTheme.modernInput(
                                    hint: 'Tìm mã HS hoặc tên học sinh...',
                                    prefixIcon: Icons.search_rounded,
                                  ),
                                ),
                                const SizedBox(height: 10),
                                SingleChildScrollView(
                                  scrollDirection: Axis.horizontal,
                                  child: Row(
                                    children: [
                                      _buildDropdown(
                                        hint: 'Môn giảng dạy',
                                        value: _selectedSubjectId,
                                        options: taughtSubjectsInClass.entries
                                            .map((e) => LookupOption(
                                                  value: e.key.toString(),
                                                  label: e.value,
                                                ))
                                            .toList(),
                                        onChanged: (v) =>
                                            setState(() => _selectedSubjectId = v),
                                      ),
                                      const SizedBox(width: 8),
                                      _buildDropdown(
                                        hint: 'Tất cả học kỳ',
                                        value: _selectedSemesterId,
                                        options: _lookups?.semesters ?? [],
                                        onChanged: (v) =>
                                            setState(() => _selectedSemesterId = v),
                                      ),
                                    ],
                                  ),
                                ),
                              ],
                            ),
                          ),
                          Expanded(
                            child: filtered.isEmpty
                                ? const Center(
                                    child: Text(
                                      'Không có bản ghi điểm phù hợp',
                                      style: TextStyle(color: AppColors.textMuted),
                                    ),
                                  )
                                : ListView.builder(
                                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 20),
                                    itemCount: filtered.length,
                                    itemBuilder: (context, index) {
                                      final g = filtered[index];
                                      return FadeSlideIn(
                                        delay: Duration(
                                            milliseconds: 30 * (index.clamp(0, 10))),
                                        child: Container(
                                          margin: const EdgeInsets.only(bottom: 10),
                                          decoration: BoxDecoration(
                                            color: Colors.white,
                                            border:
                                                Border.all(color: AppColors.borderSubtle),
                                            borderRadius: BorderRadius.circular(18),
                                            boxShadow: AppColors.softShadow(),
                                          ),
                                          child: ListTile(
                                            contentPadding: const EdgeInsets.symmetric(
                                              horizontal: 16,
                                              vertical: 6,
                                            ),
                                            title: Text(
                                              '${g.studentCode} • ${g.studentFullName}',
                                              style: const TextStyle(
                                                fontWeight: FontWeight.w700,
                                                color: AppColors.textDark,
                                              ),
                                            ),
                                            subtitle: Padding(
                                              padding: const EdgeInsets.only(top: 4),
                                              child: Text(
                                                '${g.subjectName} • ${g.semester} (${g.assessmentType})',
                                                style: const TextStyle(
                                                  fontSize: 12.5,
                                                  color: AppColors.textMuted,
                                                ),
                                              ),
                                            ),
                                            trailing: Row(
                                              mainAxisSize: MainAxisSize.min,
                                              children: [
                                                Container(
                                                  padding: const EdgeInsets.symmetric(
                                                    horizontal: 12,
                                                    vertical: 6,
                                                  ),
                                                  decoration: BoxDecoration(
                                                    color: AppColors.orangeLight,
                                                    borderRadius:
                                                        BorderRadius.circular(10),
                                                  ),
                                                  child: Text(
                                                    g.score.toStringAsFixed(1),
                                                    style: const TextStyle(
                                                      fontWeight: FontWeight.w800,
                                                      color: AppColors.orange,
                                                      fontSize: 14,
                                                    ),
                                                  ),
                                                ),
                                                const SizedBox(width: 4),
                                                IconButton(
                                                  icon: const Icon(
                                                    Icons.edit_rounded,
                                                    color: AppColors.orange,
                                                  ),
                                                  onPressed: () => _openEditDialog(g),
                                                ),
                                              ],
                                            ),
                                          ),
                                        ),
                                      );
                                    },
                                  ),
                          ),
                        ],
                      ),
      ),
    );
  }

  Widget _buildClassSelector(List<_ClassGradeGroup> groups) {
    if (groups.isEmpty) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Text(
            'Bạn chưa được phân công giảng dạy môn học cho lớp nào.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.textMuted),
          ),
        ),
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.fromLTRB(18, 8, 18, 24),
      itemCount: groups.length,
      itemBuilder: (context, index) {
        final group = groups[index];
        return FadeSlideIn(
          delay: Duration(milliseconds: 55 * index),
          child: PressableScale(
            onTap: () {
              setState(() {
                _selectedClassId = group.classId;
                _selectedClassName = group.className;
                _selectedSubjectId = null;
                _selectedSemesterId = null;
                _search = '';
              });
            },
            child: Container(
              margin: const EdgeInsets.only(bottom: 14),
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(22),
                border: Border.all(color: AppColors.borderSubtle),
                boxShadow: AppColors.softShadow(),
              ),
              child: Row(
                children: [
                  Container(
                    width: 58,
                    height: 58,
                    decoration: BoxDecoration(
                      gradient: AppColors.primaryGradient,
                      borderRadius: BorderRadius.circular(16),
                    ),
                    alignment: Alignment.center,
                    child: Text(
                      group.className,
                      style: const TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.w800,
                        fontSize: 16,
                      ),
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Lớp ${group.className}',
                          style: const TextStyle(
                            fontSize: 16.5,
                            fontWeight: FontWeight.w700,
                            color: AppColors.textDark,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Môn dạy: ${group.subjectNames.join(", ")}',
                          style: const TextStyle(
                            fontSize: 13,
                            color: AppColors.orange,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          '${group.recordCount} đầu điểm',
                          style: const TextStyle(
                            fontSize: 12,
                            color: AppColors.textMuted,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const Icon(
                    Icons.arrow_forward_ios_rounded,
                    size: 16,
                    color: AppColors.textMuted,
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  Widget _buildDropdown({
    required String hint,
    required int? value,
    required List<LookupOption> options,
    required ValueChanged<int?> onChanged,
  }) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: AppColors.borderSubtle),
        borderRadius: BorderRadius.circular(12),
      ),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<int?>(
          value: value,
          hint: Text(hint, style: const TextStyle(fontSize: 13)),
          items: [
            DropdownMenuItem<int?>(value: null, child: Text(hint)),
            ...options.map(
              (o) => DropdownMenuItem<int?>(
                value: int.tryParse(o.value),
                child: Text(o.label, style: const TextStyle(fontSize: 13)),
              ),
            ),
          ],
          onChanged: onChanged,
        ),
      ),
    );
  }
}

class TeacherTimetableScreen extends StatefulWidget {
  const TeacherTimetableScreen({super.key});

  @override
  State<TeacherTimetableScreen> createState() => _TeacherTimetableScreenState();
}

class _TeacherTimetableScreenState extends State<TeacherTimetableScreen> {
  List<ManagementTimetableItem> _items = [];
  bool _loading = true;
  String? _error;
  int? _selectedClassId;
  String? _selectedWeekStartDate;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final items = await ApiService.instance.getManagementTimetables();
      if (!mounted) return;
      setState(() {
        _items = items;
        _loading = false;
      });
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString();
          _loading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final taughtClasses = <int, String>{};
    for (final item in _items) {
      taughtClasses[item.classId] = item.className;
    }

    final availableWeeks = _items
        .map((x) => x.weekStartDate)
        .where((w) => w.isNotEmpty && !w.startsWith('0001'))
        .toSet()
        .toList()
      ..sort();

    final filtered = _items.where((x) {
      if (_selectedClassId != null && x.classId != _selectedClassId) return false;
      if (_selectedWeekStartDate != null && x.weekStartDate != _selectedWeekStartDate) {
        return false;
      }
      return true;
    }).toList();

    return Scaffold(
      appBar: AppBar(title: const Text('Lịch giảng dạy của tôi')),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: AppColors.orange))
          : _error != null
              ? Center(child: Text(_error!, style: const TextStyle(color: AppColors.redFail)))
              : Column(
                  children: [
                    if (taughtClasses.isNotEmpty)
                      Padding(
                        padding: const EdgeInsets.fromLTRB(16, 4, 16, 10),
                        child: SingleChildScrollView(
                          scrollDirection: Axis.horizontal,
                          child: Row(
                            children: [
                              ChoiceChip(
                                label: const Text('Tất cả lớp'),
                                selected: _selectedClassId == null,
                                selectedColor: AppColors.orange,
                                labelStyle: TextStyle(
                                  color: _selectedClassId == null
                                      ? Colors.white
                                      : AppColors.textDark,
                                  fontWeight: FontWeight.w600,
                                ),
                                onSelected: (_) =>
                                    setState(() => _selectedClassId = null),
                              ),
                              const SizedBox(width: 8),
                              ...taughtClasses.entries.map((c) {
                                final selected = _selectedClassId == c.key;
                                return Padding(
                                  padding: const EdgeInsets.only(right: 8),
                                  child: ChoiceChip(
                                    label: Text(c.value),
                                    selected: selected,
                                    selectedColor: AppColors.orange,
                                    labelStyle: TextStyle(
                                      color: selected
                                          ? Colors.white
                                          : AppColors.textDark,
                                      fontWeight: FontWeight.w600,
                                    ),
                                    onSelected: (_) =>
                                        setState(() => _selectedClassId = c.key),
                                  ),
                                );
                              }),
                            ],
                          ),
                        ),
                      ),
                    if (availableWeeks.isNotEmpty)
                      Padding(
                        padding: const EdgeInsets.fromLTRB(16, 0, 16, 10),
                        child: SingleChildScrollView(
                          scrollDirection: Axis.horizontal,
                          child: Row(
                            children: [
                              ChoiceChip(
                                label: const Text('Tất cả tuần'),
                                selected: _selectedWeekStartDate == null,
                                selectedColor: AppColors.indigo,
                                labelStyle: TextStyle(
                                  color: _selectedWeekStartDate == null
                                      ? Colors.white
                                      : AppColors.textDark,
                                  fontWeight: FontWeight.w600,
                                ),
                                onSelected: (_) =>
                                    setState(() => _selectedWeekStartDate = null),
                              ),
                              const SizedBox(width: 8),
                              ...availableWeeks.map((w) {
                                final selected = _selectedWeekStartDate == w;
                                return Padding(
                                  padding: const EdgeInsets.only(right: 8),
                                  child: ChoiceChip(
                                    label: Text('Tuần $w'),
                                    selected: selected,
                                    selectedColor: AppColors.indigo,
                                    labelStyle: TextStyle(
                                      color: selected
                                          ? Colors.white
                                          : AppColors.textDark,
                                      fontWeight: FontWeight.w600,
                                    ),
                                    onSelected: (_) =>
                                        setState(() => _selectedWeekStartDate = w),
                                  ),
                                );
                              }),
                            ],
                          ),
                        ),
                      ),
                    Expanded(
                      child: filtered.isEmpty
                          ? const Center(
                              child: Text(
                                'Bạn chưa có tiết giảng dạy nào',
                                style: TextStyle(color: AppColors.textMuted),
                              ),
                            )
                          : ListView.builder(
                              padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                              itemCount: filtered.length,
                              itemBuilder: (context, index) {
                                final t = filtered[index];
                                return FadeSlideIn(
                                  delay: Duration(
                                      milliseconds: 30 * (index.clamp(0, 10))),
                                  child: Container(
                                    margin: const EdgeInsets.only(bottom: 10),
                                    decoration: BoxDecoration(
                                      color: Colors.white,
                                      border: Border.all(
                                          color: AppColors.borderSubtle),
                                      borderRadius: BorderRadius.circular(18),
                                      boxShadow: AppColors.softShadow(),
                                    ),
                                    child: ListTile(
                                      leading: Container(
                                        padding: const EdgeInsets.symmetric(
                                            horizontal: 12, vertical: 8),
                                        decoration: BoxDecoration(
                                          color: AppColors.orangeLight,
                                          borderRadius:
                                              BorderRadius.circular(12),
                                        ),
                                        child: Text(
                                          t.className,
                                          style: const TextStyle(
                                            color: AppColors.orange,
                                            fontWeight: FontWeight.w800,
                                          ),
                                        ),
                                      ),
                                      title: Text(
                                        t.subjectName,
                                        style: const TextStyle(
                                          fontWeight: FontWeight.w700,
                                          color: AppColors.textDark,
                                        ),
                                      ),
                                      subtitle: Text(
                                        '${t.dayOfWeek} (${t.startTime} - ${t.endTime}) • Tuần ${t.weekStartDate}',
                                        style: const TextStyle(
                                          fontSize: 12.5,
                                          color: AppColors.textMuted,
                                        ),
                                      ),
                                    ),
                                  ),
                                );
                              },
                            ),
                    ),
                  ],
                ),
    );
  }
}

class _ClassStudentGroup {
  final int classId;
  final String className;
  final int studentCount;

  _ClassStudentGroup({
    required this.classId,
    required this.className,
    required this.studentCount,
  });
}

class TeacherStudentsScreen extends StatefulWidget {
  const TeacherStudentsScreen({super.key});

  @override
  State<TeacherStudentsScreen> createState() => _TeacherStudentsScreenState();
}

class _TeacherStudentsScreenState extends State<TeacherStudentsScreen> {
  List<ManagementStudentItem> _allStudents = [];
  bool _loading = true;
  String? _error;
  int? _selectedClassId;
  String _selectedClassName = '';
  String _search = '';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final students = await ApiService.instance.getManagementStudents();
      if (!mounted) return;
      setState(() {
        _allStudents = students;
        _loading = false;
      });
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString();
          _loading = false;
        });
      }
    }
  }

  List<_ClassStudentGroup> _buildClassGroups() {
    final map = <int, List<ManagementStudentItem>>{};
    for (final s in _allStudents) {
      map.putIfAbsent(s.classId, () => []).add(s);
    }
    final groups = map.entries
        .map((e) => _ClassStudentGroup(
              classId: e.key,
              className: e.value.first.className,
              studentCount: e.value.length,
            ))
        .toList();
    groups.sort((a, b) => a.className.compareTo(b.className));
    return groups;
  }

  void _backToClassList() {
    setState(() {
      _selectedClassId = null;
      _selectedClassName = '';
      _search = '';
    });
  }

  @override
  Widget build(BuildContext context) {
    final classGroups = _buildClassGroups();
    final classStudents = _selectedClassId == null
        ? <ManagementStudentItem>[]
        : _allStudents.where((s) => s.classId == _selectedClassId).toList();

    final filtered = _search.isEmpty
        ? classStudents
        : classStudents.where((s) {
            final q = _search.toLowerCase();
            return s.fullName.toLowerCase().contains(q) ||
                s.studentCode.toLowerCase().contains(q);
          }).toList();

    return PopScope(
      canPop: _selectedClassId == null,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop && _selectedClassId != null) {
          _backToClassList();
        }
      },
      child: Scaffold(
        appBar: AppBar(
          leading: _selectedClassId != null
              ? IconButton(
                  icon: const Icon(Icons.arrow_back_rounded),
                  onPressed: _backToClassList,
                )
              : null,
          title: Text(
            _selectedClassId == null
                ? 'Chọn lớp quản lý'
                : 'Học sinh lớp $_selectedClassName',
          ),
        ),
        body: _loading
            ? const Center(child: CircularProgressIndicator(color: AppColors.orange))
            : _error != null
                ? Center(child: Text(_error!, style: const TextStyle(color: AppColors.redFail)))
                : _selectedClassId == null
                    ? _buildClassList(classGroups)
                    : Column(
                        children: [
                          Padding(
                            padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
                            child: TextField(
                              onChanged: (v) => setState(() => _search = v.trim()),
                              decoration: AppTheme.modernInput(
                                hint: 'Tìm mã HS hoặc họ tên...',
                                prefixIcon: Icons.search_rounded,
                              ),
                            ),
                          ),
                          Expanded(
                            child: filtered.isEmpty
                                ? const Center(
                                    child: Text(
                                      'Không tìm thấy học sinh phù hợp',
                                      style: TextStyle(color: AppColors.textMuted),
                                    ),
                                  )
                                : ListView.builder(
                                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 20),
                                    itemCount: filtered.length,
                                    itemBuilder: (context, index) {
                                      final s = filtered[index];
                                      return FadeSlideIn(
                                        delay: Duration(
                                            milliseconds: 30 * (index.clamp(0, 10))),
                                        child: Container(
                                          margin: const EdgeInsets.only(bottom: 10),
                                          decoration: BoxDecoration(
                                            color: Colors.white,
                                            border: Border.all(
                                                color: AppColors.borderSubtle),
                                            borderRadius: BorderRadius.circular(18),
                                            boxShadow: AppColors.softShadow(),
                                          ),
                                          child: ListTile(
                                            contentPadding: const EdgeInsets.symmetric(
                                              horizontal: 16,
                                              vertical: 6,
                                            ),
                                            leading: Container(
                                              width: 44,
                                              height: 44,
                                              decoration: BoxDecoration(
                                                color: AppColors.orangeLight,
                                                borderRadius:
                                                    BorderRadius.circular(12),
                                              ),
                                              child: const Icon(
                                                Icons.person_rounded,
                                                color: AppColors.orange,
                                              ),
                                            ),
                                            title: Text(
                                              '${s.studentCode} • ${s.fullName}',
                                              style: const TextStyle(
                                                fontWeight: FontWeight.w700,
                                                color: AppColors.textDark,
                                              ),
                                            ),
                                            subtitle: Text(
                                              'Lớp: ${s.className} • ${s.gender ?? ''} • Sinh: ${s.dateOfBirth}',
                                              style: const TextStyle(
                                                fontSize: 12.5,
                                                color: AppColors.textMuted,
                                              ),
                                            ),
                                            trailing: Container(
                                              padding: const EdgeInsets.symmetric(
                                                horizontal: 10,
                                                vertical: 4,
                                              ),
                                              decoration: BoxDecoration(
                                                color: s.isActive
                                                    ? AppColors.greenSoft
                                                    : AppColors.redSoft,
                                                borderRadius:
                                                    BorderRadius.circular(10),
                                              ),
                                              child: Text(
                                                s.isActive ? 'Đang học' : 'Nghỉ',
                                                style: TextStyle(
                                                  fontSize: 11,
                                                  fontWeight: FontWeight.w700,
                                                  color: s.isActive
                                                      ? AppColors.greenPass
                                                      : AppColors.redFail,
                                                ),
                                              ),
                                            ),
                                          ),
                                        ),
                                      );
                                    },
                                  ),
                          ),
                        ],
                      ),
      ),
    );
  }

  Widget _buildClassList(List<_ClassStudentGroup> groups) {
    if (groups.isEmpty) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Text(
            'Bạn chưa được phân công chủ nhiệm hoặc giảng dạy lớp nào.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.textMuted),
          ),
        ),
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.fromLTRB(18, 8, 18, 24),
      itemCount: groups.length,
      itemBuilder: (context, index) {
        final group = groups[index];
        return FadeSlideIn(
          delay: Duration(milliseconds: 55 * index),
          child: PressableScale(
            onTap: () {
              setState(() {
                _selectedClassId = group.classId;
                _selectedClassName = group.className;
                _search = '';
              });
            },
            child: Container(
              margin: const EdgeInsets.only(bottom: 14),
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(22),
                border: Border.all(color: AppColors.borderSubtle),
                boxShadow: AppColors.softShadow(),
              ),
              child: Row(
                children: [
                  Container(
                    width: 58,
                    height: 58,
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(
                        colors: [Color(0xFF14B8A6), AppColors.teal],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      ),
                      borderRadius: BorderRadius.circular(16),
                    ),
                    alignment: Alignment.center,
                    child: Text(
                      group.className,
                      style: const TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.w800,
                        fontSize: 16,
                      ),
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Lớp ${group.className}',
                          style: const TextStyle(
                            fontSize: 16.5,
                            fontWeight: FontWeight.w700,
                            color: AppColors.textDark,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          '${group.studentCount} học sinh',
                          style: const TextStyle(
                            fontSize: 13,
                            color: AppColors.teal,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const Icon(
                    Icons.arrow_forward_ios_rounded,
                    size: 16,
                    color: AppColors.textMuted,
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}
