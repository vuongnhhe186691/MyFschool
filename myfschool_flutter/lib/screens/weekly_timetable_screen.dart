import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../core/app_theme.dart';
import '../models/models.dart';
import '../services/api_service.dart';

class WeeklyTimetableScreen extends StatefulWidget {
  const WeeklyTimetableScreen({super.key});

  @override
  State<WeeklyTimetableScreen> createState() => _WeeklyTimetableScreenState();
}

class _WeeklyTimetableScreenState extends State<WeeklyTimetableScreen> {
  List<TimetableItem> _allItems = [];
  List<String> _availableWeeks = [];
  String _selectedWeekKey = '';
  late int _selectedDayIndex;
  bool _userPickedDay = false;
  bool _isGridView = false;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _selectedDayIndex = DateTime.now().weekday - 1;
    _loadTimetable();
  }

  Future<void> _loadTimetable() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final items = await ApiService.instance.getWeeklyTimetable();
      if (!mounted) return;

      final weeks = items
          .map((e) => e.weekStartDate.trim())
          .where((w) => w.isNotEmpty)
          .toSet()
          .toList()
        ..sort();

      final now = DateTime.now();
      final currentMondayStr = DateFormat('yyyy-MM-dd').format(
        DateTime(now.year, now.month, now.day)
            .subtract(Duration(days: now.weekday - 1)),
      );

      String chosenWeek = _selectedWeekKey;
      if (weeks.isNotEmpty && !weeks.contains(chosenWeek)) {
        chosenWeek = weeks.contains(currentMondayStr)
            ? currentMondayStr
            : weeks.last;
      }

      var chosenDayIdx = _selectedDayIndex;
      if (!_userPickedDay && items.isNotEmpty) {
        final weekItems = chosenWeek.isEmpty
            ? items
            : items.where((x) => x.weekStartDate == chosenWeek).toList();
        final currentDayEnum = _dayEnumName(chosenDayIdx + 1);
        final hasToday = weekItems.any(
          (x) => x.dayOfWeek.toLowerCase() == currentDayEnum.toLowerCase(),
        );
        if (!hasToday && weekItems.isNotEmpty) {
          chosenDayIdx = _dayEnumToIndex(weekItems.first.dayOfWeek);
        }
      }

      setState(() {
        _allItems = items;
        _availableWeeks = weeks;
        _selectedWeekKey = chosenWeek;
        _selectedDayIndex = chosenDayIdx;
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

  DateTime _resolveSelectedWeekMonday() {
    if (_selectedWeekKey.isNotEmpty) {
      final parsed = DateTime.tryParse(_selectedWeekKey);
      if (parsed != null) return parsed;
    }
    final now = DateTime.now();
    return DateTime(now.year, now.month, now.day)
        .subtract(Duration(days: now.weekday - 1));
  }

  int _dayEnumToIndex(String day) => switch (day.toLowerCase()) {
        'monday' => 0,
        'tuesday' => 1,
        'wednesday' => 2,
        'thursday' => 3,
        'friday' => 4,
        'saturday' => 5,
        _ => 6,
      };

  String _dayEnumName(int weekday) => switch (weekday) {
        DateTime.monday => 'Monday',
        DateTime.tuesday => 'Tuesday',
        DateTime.wednesday => 'Wednesday',
        DateTime.thursday => 'Thursday',
        DateTime.friday => 'Friday',
        DateTime.saturday => 'Saturday',
        _ => 'Sunday',
      };

  String _shortDayName(int weekday) => switch (weekday) {
        DateTime.monday => 'T2',
        DateTime.tuesday => 'T3',
        DateTime.wednesday => 'T4',
        DateTime.thursday => 'T5',
        DateTime.friday => 'T6',
        DateTime.saturday => 'T7',
        _ => 'CN',
      };

  int _parseMinutes(String timeStr) {
    final parts = timeStr.split(':');
    if (parts.length < 2) return 0;
    final h = int.tryParse(parts[0]) ?? 0;
    final m = int.tryParse(parts[1]) ?? 0;
    return h * 60 + m;
  }

  bool _isMorning(TimetableItem item) => _parseMinutes(item.startTime) < 12 * 60;

  @override
  Widget build(BuildContext context) {
    final weekMonday = _resolveSelectedWeekMonday();
    final selectedDate = weekMonday.add(Duration(days: _selectedDayIndex));
    final selectedDayEnum = _dayEnumName(selectedDate.weekday);
    final weekRangeLabel =
        '${DateFormat('dd/MM').format(weekMonday)} - ${DateFormat('dd/MM/yyyy').format(weekMonday.add(const Duration(days: 6)))}';
    final dateHeader = DateFormat('dd/MM/yyyy').format(selectedDate);

    final weekItems = _selectedWeekKey.isEmpty
        ? _allItems
        : _allItems.where((x) => x.weekStartDate == _selectedWeekKey).toList();

    final dayLessons = weekItems
        .where((x) => x.dayOfWeek.toLowerCase() == selectedDayEnum.toLowerCase())
        .toList()
      ..sort((a, b) =>
          _parseMinutes(a.startTime).compareTo(_parseMinutes(b.startTime)));

    final currentWeekIdx = _availableWeeks.indexOf(_selectedWeekKey);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Weekly Timetable'),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 12),
            child: IconButton.filledTonal(
              style: IconButton.styleFrom(
                backgroundColor: AppColors.orangeLight,
                foregroundColor: AppColors.orange,
              ),
              tooltip: _isGridView ? 'Chế độ thẻ' : 'Chế độ bảng lưới',
              icon: Icon(
                _isGridView ? Icons.view_agenda_rounded : Icons.grid_on_rounded,
              ),
              onPressed: () => setState(() => _isGridView = !_isGridView),
            ),
          ),
        ],
      ),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(color: AppColors.orange),
            )
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          _error!,
                          textAlign: TextAlign.center,
                          style: const TextStyle(color: AppColors.redFail),
                        ),
                        const SizedBox(height: 12),
                        GradientButton(
                          label: 'Thử lại',
                          onPressed: _loadTimetable,
                        ),
                      ],
                    ),
                  ),
                )
              : RefreshIndicator(
                  color: AppColors.orange,
                  onRefresh: _loadTimetable,
                  child: SingleChildScrollView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.fromLTRB(16, 4, 16, 28),
                    child: Center(
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: 560),
                        child: Column(
                          children: [
                            FadeSlideIn(
                              child: Container(
                                padding: const EdgeInsets.all(14),
                                decoration: BoxDecoration(
                                  color: Colors.white,
                                  borderRadius: BorderRadius.circular(22),
                                  border: Border.all(color: AppColors.borderSubtle),
                                  boxShadow: AppColors.softShadow(),
                                ),
                                child: Column(
                                  children: [
                                    Row(
                                      mainAxisAlignment:
                                          MainAxisAlignment.spaceBetween,
                                      children: [
                                        IconButton(
                                          icon: Icon(
                                            Icons.chevron_left_rounded,
                                            color: currentWeekIdx > 0
                                                ? AppColors.orange
                                                : Colors.grey.shade300,
                                            size: 28,
                                          ),
                                          onPressed: currentWeekIdx > 0
                                              ? () {
                                                  setState(() {
                                                    _selectedWeekKey =
                                                        _availableWeeks[
                                                            currentWeekIdx - 1];
                                                  });
                                                }
                                              : null,
                                        ),
                                        Column(
                                          children: [
                                            Text(
                                              _availableWeeks.isEmpty
                                                  ? 'Thời khóa biểu'
                                                  : 'Tuần $weekRangeLabel',
                                              style: const TextStyle(
                                                color: AppColors.textDark,
                                                fontSize: 15.5,
                                                fontWeight: FontWeight.w700,
                                              ),
                                            ),
                                            if (_availableWeeks.length > 1)
                                              Text(
                                                'Tuần ${currentWeekIdx + 1}/${_availableWeeks.length} trong CSDL',
                                                style: const TextStyle(
                                                  fontSize: 11,
                                                  color: AppColors.textMuted,
                                                ),
                                              ),
                                          ],
                                        ),
                                        IconButton(
                                          icon: Icon(
                                            Icons.chevron_right_rounded,
                                            color: (currentWeekIdx >= 0 &&
                                                    currentWeekIdx <
                                                        _availableWeeks.length -
                                                            1)
                                                ? AppColors.orange
                                                : Colors.grey.shade300,
                                            size: 28,
                                          ),
                                          onPressed: (currentWeekIdx >= 0 &&
                                                  currentWeekIdx <
                                                      _availableWeeks.length -
                                                          1)
                                              ? () {
                                                  setState(() {
                                                    _selectedWeekKey =
                                                        _availableWeeks[
                                                            currentWeekIdx + 1];
                                                  });
                                                }
                                              : null,
                                        ),
                                      ],
                                    ),
                                    if (_availableWeeks.length > 1) ...[
                                      const SizedBox(height: 6),
                                      SingleChildScrollView(
                                        scrollDirection: Axis.horizontal,
                                        child: Row(
                                          children: _availableWeeks.map((w) {
                                            final active = w == _selectedWeekKey;
                                            return Padding(
                                              padding: const EdgeInsets.only(
                                                  right: 6),
                                              child: ChoiceChip(
                                                label: Text(w),
                                                selected: active,
                                                selectedColor: AppColors.orange,
                                                labelStyle: TextStyle(
                                                  fontSize: 11.5,
                                                  fontWeight: FontWeight.w600,
                                                  color: active
                                                      ? Colors.white
                                                      : AppColors.textDark,
                                                ),
                                                onSelected: (_) {
                                                  setState(
                                                    () => _selectedWeekKey = w,
                                                  );
                                                },
                                              ),
                                            );
                                          }).toList(),
                                        ),
                                      ),
                                    ],
                                    const SizedBox(height: 8),
                                    Row(
                                      mainAxisAlignment:
                                          MainAxisAlignment.spaceBetween,
                                      children: List.generate(7, (idx) {
                                        final dayDate =
                                            weekMonday.add(Duration(days: idx));
                                        final isSelected =
                                            idx == _selectedDayIndex;
                                        final dayEnum =
                                            _dayEnumName(dayDate.weekday);
                                        final hasClasses = weekItems.any(
                                          (x) =>
                                              x.dayOfWeek.toLowerCase() ==
                                              dayEnum.toLowerCase(),
                                        );

                                        return PressableScale(
                                          onTap: () => setState(() {
                                            _selectedDayIndex = idx;
                                            _userPickedDay = true;
                                          }),
                                          child: AnimatedContainer(
                                            duration: const Duration(
                                                milliseconds: 220),
                                            curve: Curves.easeOutCubic,
                                            width: 42,
                                            padding: const EdgeInsets.symmetric(
                                              vertical: 8,
                                            ),
                                            decoration: BoxDecoration(
                                              gradient: isSelected
                                                  ? AppColors.primaryGradient
                                                  : null,
                                              color: isSelected
                                                  ? null
                                                  : Colors.transparent,
                                              borderRadius:
                                                  BorderRadius.circular(14),
                                              boxShadow: isSelected
                                                  ? AppColors.softShadow(
                                                      tint: AppColors.orange,
                                                    )
                                                  : null,
                                            ),
                                            child: Column(
                                              children: [
                                                Text(
                                                  _shortDayName(
                                                      dayDate.weekday),
                                                  style: TextStyle(
                                                    fontSize: 11,
                                                    fontWeight: FontWeight.w600,
                                                    color: isSelected
                                                        ? Colors.white70
                                                        : AppColors.textMuted,
                                                  ),
                                                ),
                                                const SizedBox(height: 3),
                                                Text(
                                                  '${dayDate.day}',
                                                  style: TextStyle(
                                                    fontSize: 15,
                                                    fontWeight: FontWeight.w800,
                                                    color: isSelected
                                                        ? Colors.white
                                                        : AppColors.textDark,
                                                  ),
                                                ),
                                                const SizedBox(height: 3),
                                                Container(
                                                  width: 5,
                                                  height: 5,
                                                  decoration: BoxDecoration(
                                                    shape: BoxShape.circle,
                                                    color: hasClasses
                                                        ? (isSelected
                                                            ? Colors.white
                                                            : AppColors.orange)
                                                        : Colors.transparent,
                                                  ),
                                                ),
                                              ],
                                            ),
                                          ),
                                        );
                                      }),
                                    ),
                                  ],
                                ),
                              ),
                            ),

                            const SizedBox(height: 18),

                            AnimatedSwitcher(
                              duration: const Duration(milliseconds: 280),
                              switchInCurve: Curves.easeOutCubic,
                              child: _isGridView
                                  ? _buildGridTable(
                                      key: ValueKey(
                                          'grid_${_selectedWeekKey}_$selectedDayEnum'),
                                      dayName: selectedDayEnum,
                                      dateHeader: dateHeader,
                                      lessons: dayLessons,
                                    )
                                  : _buildTimelineView(
                                      key: ValueKey(
                                          'list_${_selectedWeekKey}_$selectedDayEnum'),
                                      dayName: selectedDayEnum,
                                      dateHeader: dateHeader,
                                      lessons: dayLessons,
                                    ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
    );
  }

  Widget _buildTimelineView({
    required Key key,
    required String dayName,
    required String dateHeader,
    required List<TimetableItem> lessons,
  }) {
    if (lessons.isEmpty) {
      return Container(
        key: key,
        width: double.infinity,
        padding: const EdgeInsets.all(36),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(22),
          border: Border.all(color: AppColors.borderSubtle),
        ),
        child: Column(
          children: [
            const Icon(
              Icons.weekend_rounded,
              size: 48,
              color: AppColors.orangeBright,
            ),
            const SizedBox(height: 10),
            Text(
              'Không có lịch học vào $dayName ($dateHeader)',
              style: const TextStyle(
                fontWeight: FontWeight.w600,
                color: AppColors.textSecondary,
              ),
            ),
          ],
        ),
      );
    }

    return Column(
      key: key,
      children: [
        for (int i = 0; i < lessons.length; i++)
          _TimelineLessonCard(
            item: lessons[i],
            periodNumber: i + 1,
            sessionName: _isMorning(lessons[i]) ? 'Sáng' : 'Chiều',
            index: i,
          ),
      ],
    );
  }

  Widget _buildGridTable({
    required Key key,
    required String dayName,
    required String dateHeader,
    required List<TimetableItem> lessons,
  }) {
    if (lessons.isEmpty) {
      return _buildTimelineView(
        key: key,
        dayName: dayName,
        dateHeader: dateHeader,
        lessons: lessons,
      );
    }

    final morningLessons = lessons.where(_isMorning).toList();
    final afternoonLessons = lessons.where((x) => !_isMorning(x)).toList();

    final tableRows = <TableRow>[
      TableRow(
        children: [
          _gridCell('Buổi', AppColors.orange, textColor: Colors.white, bold: true),
          _gridCell('Tiết', AppColors.orange, textColor: Colors.white, bold: true),
          _gridCell(
            '$dayName ($dateHeader)',
            AppColors.orangeBright,
            textColor: Colors.white,
            bold: true,
          ),
        ],
      ),
    ];

    void appendGroup(String sessionTitle, List<TimetableItem> group) {
      for (var i = 0; i < group.length; i++) {
        final item = group[i];
        tableRows.add(
          TableRow(
            children: [
              _gridCell(
                i == 0 ? sessionTitle : '',
                AppColors.orangeLight,
                bold: i == 0,
              ),
              _gridCell('Tiết ${i + 1}\n${item.startTime}', const Color(0xFFFFF8F2)),
              Container(
                constraints: const BoxConstraints(minHeight: 56),
                padding: const EdgeInsets.all(10),
                color: Colors.white,
                alignment: Alignment.centerLeft,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      item.subjectName,
                      style: const TextStyle(
                        fontWeight: FontWeight.w700,
                        fontSize: 13.5,
                      ),
                    ),
                    Text(
                      '${item.startTime}-${item.endTime} • ${item.teacherName}',
                      style: const TextStyle(
                        fontSize: 11.5,
                        color: AppColors.textMuted,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      }
    }

    appendGroup('Sáng', morningLessons);
    appendGroup('Chiều', afternoonLessons);

    return Container(
      key: key,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: AppColors.softShadow(),
      ),
      clipBehavior: Clip.antiAlias,
      child: Table(
        border: TableBorder.all(color: AppColors.orangeSoft, width: 1),
        columnWidths: const {
          0: FlexColumnWidth(0.9),
          1: FlexColumnWidth(1.0),
          2: FlexColumnWidth(2.0),
        },
        children: tableRows,
      ),
    );
  }

  Widget _gridCell(
    String text,
    Color bg, {
    Color textColor = AppColors.textDark,
    bool bold = false,
  }) {
    return Container(
      constraints: const BoxConstraints(minHeight: 56),
      color: bg,
      alignment: Alignment.center,
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 10),
      child: Text(
        text,
        textAlign: TextAlign.center,
        style: TextStyle(
          fontSize: 12.5,
          fontWeight: bold ? FontWeight.w700 : FontWeight.w500,
          color: textColor,
        ),
      ),
    );
  }
}

class _TimelineLessonCard extends StatelessWidget {
  final TimetableItem item;
  final int periodNumber;
  final String sessionName;
  final int index;

  const _TimelineLessonCard({
    required this.item,
    required this.periodNumber,
    required this.sessionName,
    required this.index,
  });

  @override
  Widget build(BuildContext context) {
    final isMorning = sessionName == 'Sáng';
    final accent = isMorning ? AppColors.orange : AppColors.indigo;

    return FadeSlideIn(
      delay: Duration(milliseconds: index * 55),
      child: Container(
        margin: const EdgeInsets.only(bottom: 12),
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: AppColors.borderSubtle),
          boxShadow: AppColors.softShadow(),
        ),
        child: Row(
          children: [
            Container(
              width: 72,
              padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 6),
              decoration: BoxDecoration(
                color: accent.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(14),
              ),
              child: Column(
                children: [
                  Text(
                    'Tiết $periodNumber',
                    style: TextStyle(
                      fontSize: 11.5,
                      fontWeight: FontWeight.w800,
                      color: accent,
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    item.startTime,
                    style: const TextStyle(
                      fontSize: 12.5,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textDark,
                    ),
                  ),
                  Text(
                    item.endTime,
                    style: const TextStyle(
                      fontSize: 10.5,
                      color: AppColors.textMuted,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 2,
                        ),
                        decoration: BoxDecoration(
                          color: accent.withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          '$sessionName • Lớp ${item.className}',
                          style: TextStyle(
                            fontSize: 10.5,
                            fontWeight: FontWeight.w700,
                            color: accent,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    item.subjectName,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textDark,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      const Icon(
                        Icons.person_outline_rounded,
                        size: 15,
                        color: AppColors.textMuted,
                      ),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          item.teacherName,
                          style: const TextStyle(
                            fontSize: 12.5,
                            color: AppColors.textMuted,
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
