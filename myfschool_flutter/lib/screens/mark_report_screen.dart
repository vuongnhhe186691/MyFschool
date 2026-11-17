import 'package:flutter/material.dart';
import '../core/app_theme.dart';
import '../models/models.dart';
import '../services/api_service.dart';

class MarkReportScreen extends StatefulWidget {
  const MarkReportScreen({super.key});

  @override
  State<MarkReportScreen> createState() => _MarkReportScreenState();
}

class _MarkReportScreenState extends State<MarkReportScreen> {
  late Future<List<GradeItem>> _future;
  String _selectedSemester = 'All';

  @override
  void initState() {
    super.initState();
    _future = ApiService.instance.getStudentGrades();
  }

  List<SubjectMarkSummary> _groupGrades(List<GradeItem> raw) {
    final filtered = _selectedSemester == 'All'
        ? raw
        : raw.where((e) => e.semester == _selectedSemester).toList();

    final map = <String, List<GradeItem>>{};
    for (final item in filtered) {
      final key = '${item.academicYear}|${item.semester}|${item.subjectName}';
      map.putIfAbsent(key, () => []).add(item);
    }

    final summaries = map.values.map((list) {
      final first = list.first;
      return SubjectMarkSummary(
        subjectName: first.subjectName,
        semester: first.semester,
        academicYear: first.academicYear,
        items: list,
      );
    }).toList();

    summaries.sort((a, b) {
      final semCmp = a.semester.compareTo(b.semester);
      if (semCmp != 0) return semCmp;
      return a.subjectName.compareTo(b.subjectName);
    });

    return summaries;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Mark Report')),
      body: FutureBuilder<List<GradeItem>>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(
              child: CircularProgressIndicator(color: AppColors.orange),
            );
          }

          if (snapshot.hasError) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      snapshot.error.toString(),
                      textAlign: TextAlign.center,
                      style: const TextStyle(color: AppColors.redFail),
                    ),
                    const SizedBox(height: 12),
                    GradientButton(
                      label: 'Thử lại',
                      onPressed: () {
                        setState(() {
                          _future = ApiService.instance.getStudentGrades();
                        });
                      },
                    ),
                  ],
                ),
              ),
            );
          }

          final raw = snapshot.data ?? [];
          final semesters = <String>{'All', ...raw.map((e) => e.semester)}.toList();
          final summaries = _groupGrades(raw);

          final overallAvg = summaries.isEmpty
              ? 0.0
              : summaries.map((e) => e.average).reduce((a, b) => a + b) /
                  summaries.length;
          final passedCount = summaries.where((e) => e.isPassed).length;

          return RefreshIndicator(
            color: AppColors.orange,
            onRefresh: () async {
              setState(() {
                _future = ApiService.instance.getStudentGrades();
              });
              await _future;
            },
            child: ListView(
              padding: const EdgeInsets.fromLTRB(18, 6, 18, 28),
              children: [
                FadeSlideIn(
                  child: Container(
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      gradient: AppColors.primaryGradient,
                      borderRadius: BorderRadius.circular(24),
                      boxShadow: AppColors.softShadow(tint: AppColors.orange),
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 10,
                                  vertical: 4,
                                ),
                                decoration: BoxDecoration(
                                  color: Colors.white.withValues(alpha: 0.22),
                                  borderRadius: BorderRadius.circular(12),
                                ),
                                child: Text(
                                  raw.isNotEmpty
                                      ? '${raw.first.studentFullName} • ${raw.first.studentCode}'
                                      : 'Bảng điểm học sinh',
                                  style: const TextStyle(
                                    color: Colors.white,
                                    fontSize: 12,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ),
                              const SizedBox(height: 12),
                              const Text(
                                'Điểm trung bình',
                                style: TextStyle(
                                  color: Colors.white70,
                                  fontSize: 13,
                                ),
                              ),
                              TweenAnimationBuilder<double>(
                                tween: Tween(begin: 0, end: overallAvg),
                                duration: const Duration(milliseconds: 800),
                                curve: Curves.easeOutCubic,
                                builder: (_, val, _) => Text(
                                  val.toStringAsFixed(2),
                                  style: const TextStyle(
                                    color: Colors.white,
                                    fontSize: 34,
                                    fontWeight: FontWeight.w800,
                                    height: 1.15,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 16,
                            vertical: 12,
                          ),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(18),
                          ),
                          child: Column(
                            children: [
                              Text(
                                '$passedCount/${summaries.length}',
                                style: const TextStyle(
                                  fontSize: 20,
                                  fontWeight: FontWeight.w800,
                                  color: AppColors.greenPass,
                                ),
                              ),
                              const Text(
                                'Môn đạt',
                                style: TextStyle(
                                  fontSize: 11.5,
                                  color: AppColors.textMuted,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),

                const SizedBox(height: 16),

                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: semesters.map((sem) {
                      final selected = _selectedSemester == sem;
                      return Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: PressableScale(
                          onTap: () => setState(() => _selectedSemester = sem),
                          child: AnimatedContainer(
                            duration: const Duration(milliseconds: 220),
                            padding: const EdgeInsets.symmetric(
                              horizontal: 18,
                              vertical: 10,
                            ),
                            decoration: BoxDecoration(
                              color: selected ? AppColors.orange : Colors.white,
                              borderRadius: BorderRadius.circular(24),
                              border: Border.all(
                                color: selected
                                    ? AppColors.orange
                                    : AppColors.borderSubtle,
                              ),
                              boxShadow: selected
                                  ? AppColors.softShadow(tint: AppColors.orange)
                                  : null,
                            ),
                            child: Text(
                              sem,
                              style: TextStyle(
                                color: selected
                                    ? Colors.white
                                    : AppColors.textSecondary,
                                fontWeight: FontWeight.w700,
                                fontSize: 13.5,
                              ),
                            ),
                          ),
                        ),
                      );
                    }).toList(),
                  ),
                ),

                const SizedBox(height: 16),

                if (summaries.isEmpty)
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 48),
                    child: Center(
                      child: Text(
                        'Chưa có dữ liệu điểm',
                        style: TextStyle(color: AppColors.textMuted),
                      ),
                    ),
                  )
                else
                  ...List.generate(
                    summaries.length,
                    (idx) => FadeSlideIn(
                      delay: Duration(milliseconds: 60 * (idx.clamp(0, 8))),
                      child: _MarkCard(summary: summaries[idx]),
                    ),
                  ),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _MarkCard extends StatelessWidget {
  final SubjectMarkSummary summary;

  const _MarkCard({required this.summary});

  @override
  Widget build(BuildContext context) {
    final badgeBg = summary.isPassed ? AppColors.greenSoft : AppColors.redSoft;
    final badgeText =
        summary.isPassed ? AppColors.greenPass : AppColors.redFail;

    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: AppColors.borderSubtle),
        boxShadow: AppColors.softShadow(),
      ),
      child: Theme(
        data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
        child: ExpansionTile(
          tilePadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          childrenPadding: const EdgeInsets.fromLTRB(18, 0, 18, 16),
          leading: AnimatedScoreRing(
            score: summary.average,
            isPassed: summary.isPassed,
            size: 64,
          ),
          title: Row(
            children: [
              Expanded(
                child: Text(
                  summary.subjectName,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textDark,
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                decoration: BoxDecoration(
                  color: badgeBg,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Text(
                  summary.isPassed ? 'Passed' : 'Failed',
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                    color: badgeText,
                  ),
                ),
              ),
            ],
          ),
          subtitle: Padding(
            padding: const EdgeInsets.only(top: 6),
            child: Text(
              'Học kỳ: ${summary.semester} (${summary.academicYear}) • Trung bình: ${summary.average.toStringAsFixed(1)}',
              style: const TextStyle(
                fontSize: 12.5,
                color: AppColors.textMuted,
              ),
            ),
          ),
          children: [
            const Divider(color: AppColors.borderSubtle),
            const SizedBox(height: 8),
            ...summary.items.map((item) {
              final ratio = (item.score / 10.0).clamp(0.0, 1.0);
              return Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: Column(
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          item.assessmentType,
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: AppColors.textSecondary,
                          ),
                        ),
                        Row(
                          children: [
                            Text(
                              item.score.toStringAsFixed(1),
                              style: const TextStyle(
                                fontWeight: FontWeight.w800,
                                fontSize: 14,
                                color: AppColors.textDark,
                              ),
                            ),
                            if (item.grade != null && item.grade!.isNotEmpty) ...[
                              const SizedBox(width: 8),
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 8,
                                  vertical: 2,
                                ),
                                decoration: BoxDecoration(
                                  color: AppColors.orangeLight,
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: Text(
                                  item.grade!,
                                  style: const TextStyle(
                                    fontSize: 11,
                                    color: AppColors.orange,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                              ),
                            ],
                          ],
                        ),
                      ],
                    ),
                    const SizedBox(height: 5),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(6),
                      child: TweenAnimationBuilder<double>(
                        tween: Tween(begin: 0, end: ratio),
                        duration: const Duration(milliseconds: 650),
                        curve: Curves.easeOutCubic,
                        builder: (_, val, _) => LinearProgressIndicator(
                          value: val,
                          minHeight: 6,
                          backgroundColor: AppColors.bg,
                          color: item.score >= 5.0
                              ? AppColors.orange
                              : AppColors.redFail,
                        ),
                      ),
                    ),
                  ],
                ),
              );
            }),
          ],
        ),
      ),
    );
  }
}
