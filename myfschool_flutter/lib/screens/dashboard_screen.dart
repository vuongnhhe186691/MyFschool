import 'package:flutter/material.dart';
import '../core/app_theme.dart';
import '../services/api_service.dart';
import 'applications_screen.dart';
import 'change_password_screen.dart';
import 'clubs_screen.dart';
import 'events_screen.dart';
import 'login_screen.dart';
import 'mark_report_screen.dart';
import 'notifications_screen.dart';
import 'profile_screen.dart';
import 'teacher_screens.dart';
import 'weekly_timetable_screen.dart';

class _DashboardTileItem {
  final String title;
  final String subtitle;
  final IconData icon;
  final Color color;
  final VoidCallback onTap;

  _DashboardTileItem({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.color,
    required this.onTap,
  });
}

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  int _unreadCount = 0;

  @override
  void initState() {
    super.initState();
    _loadUnreadCount();
  }

  Future<void> _loadUnreadCount() async {
    final count = await ApiService.instance.getUnreadNotificationCount();
    if (mounted) {
      setState(() => _unreadCount = count);
    }
  }

  Future<void> _handleLogout() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: const Text('Đăng xuất'),
        content: const Text('Bạn có chắc chắn muốn đăng xuất khỏi My FSchool?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
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
            onPressed: () => Navigator.of(ctx).pop(true),
            child: const Text('Đăng xuất'),
          ),
        ],
      ),
    );

    if (confirm == true) {
      await ApiService.instance.logout();
      if (!mounted) return;
      Navigator.of(context).pushAndRemoveUntil(
        AppPageRoute(page: const LoginScreen()),
        (route) => false,
      );
    }
  }

  void _open(Widget screen) async {
    await Navigator.of(context).push(AppPageRoute(page: screen));
    _loadUnreadCount();
  }

  @override
  Widget build(BuildContext context) {
    final auth = ApiService.instance.currentAuth;
    final role = auth?.primaryRole ?? 'Student';
    final isTeacher = auth?.isTeacher ?? false;

    final headerTitle = switch (role) {
      'Teacher' => 'Teacher Portal',
      'Parent' => 'Parent Portal',
      _ => 'Student Portal',
    };

    final roleBadge = switch (role) {
      'Teacher' => 'Giáo viên',
      'Parent' => 'Phụ huynh',
      _ => 'Học sinh',
    };

    final tiles = isTeacher
        ? [
            _DashboardTileItem(
              title: 'Manage Grades',
              subtitle: 'Cập nhật bảng điểm',
              icon: Icons.star_rounded,
              color: AppColors.amber,
              onTap: () => _open(const TeacherGradesScreen()),
            ),
            _DashboardTileItem(
              title: 'Timetable',
              subtitle: 'Lịch giảng dạy',
              icon: Icons.calendar_month_rounded,
              color: AppColors.indigo,
              onTap: () => _open(const TeacherTimetableScreen()),
            ),
            _DashboardTileItem(
              title: 'Class Students',
              subtitle: 'Danh sách học sinh theo lớp',
              icon: Icons.groups_rounded,
              color: AppColors.teal,
              onTap: () => _open(const TeacherStudentsScreen()),
            ),
            _DashboardTileItem(
              title: 'Events',
              subtitle: 'Sự kiện nhà trường',
              icon: Icons.event_available_rounded,
              color: AppColors.rose,
              onTap: () => _open(const EventsScreen()),
            ),
            _DashboardTileItem(
              title: 'Clubs',
              subtitle: 'Câu lạc bộ',
              icon: Icons.sports_soccer_rounded,
              color: AppColors.purple,
              onTap: () => _open(const ClubsScreen()),
            ),
            _DashboardTileItem(
              title: 'Send Application',
              subtitle: 'Gửi đơn từ',
              icon: Icons.description_rounded,
              color: AppColors.sky,
              onTap: () => _open(const ApplicationsScreen()),
            ),
            _DashboardTileItem(
              title: 'Profile',
              subtitle: 'Hồ sơ & 2FA',
              icon: Icons.person_rounded,
              color: AppColors.orange,
              onTap: () => _open(const ProfileScreen()),
            ),
            _DashboardTileItem(
              title: 'Change Password',
              subtitle: 'Đổi mật khẩu',
              icon: Icons.lock_rounded,
              color: const Color(0xFF475569),
              onTap: () => _open(const ChangePasswordScreen()),
            ),
          ]
        : [
            _DashboardTileItem(
              title: 'My Grades',
              subtitle: 'Xem điểm & học lực',
              icon: Icons.star_rounded,
              color: AppColors.amber,
              onTap: () => _open(const MarkReportScreen()),
            ),
            _DashboardTileItem(
              title: 'My Timetable',
              subtitle: 'Thời khóa biểu tuần',
              icon: Icons.calendar_month_rounded,
              color: AppColors.indigo,
              onTap: () => _open(const WeeklyTimetableScreen()),
            ),
            _DashboardTileItem(
              title: 'Events',
              subtitle: 'Hoạt động & lịch thi',
              icon: Icons.event_available_rounded,
              color: AppColors.rose,
              onTap: () => _open(const EventsScreen()),
            ),
            _DashboardTileItem(
              title: 'Clubs',
              subtitle: 'CLB ngoại khóa',
              icon: Icons.groups_rounded,
              color: AppColors.purple,
              onTap: () => _open(const ClubsScreen()),
            ),
            _DashboardTileItem(
              title: 'Send Application',
              subtitle: 'Đơn xin phép / đề xuất',
              icon: Icons.description_rounded,
              color: AppColors.sky,
              onTap: () => _open(const ApplicationsScreen()),
            ),
            _DashboardTileItem(
              title: 'Profile',
              subtitle: 'Thông tin & bảo mật',
              icon: Icons.person_rounded,
              color: AppColors.teal,
              onTap: () => _open(const ProfileScreen()),
            ),
            _DashboardTileItem(
              title: 'Change Password',
              subtitle: 'Cập nhật mật khẩu',
              icon: Icons.lock_rounded,
              color: AppColors.orange,
              onTap: () => _open(const ChangePasswordScreen()),
            ),
          ];

    return Scaffold(
      backgroundColor: AppColors.bg,
      body: CustomScrollView(
        slivers: [
          SliverToBoxAdapter(
            child: Stack(
              clipBehavior: Clip.none,
              children: [
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.fromLTRB(20, 18, 20, 56),
                  decoration: const BoxDecoration(
                    gradient: AppColors.primaryGradient,
                    borderRadius: BorderRadius.only(
                      bottomLeft: Radius.circular(32),
                      bottomRight: Radius.circular(32),
                    ),
                  ),
                  child: SafeArea(
                    bottom: false,
                    child: Column(
                      children: [
                        Row(
                          children: [
                            PulseFloatingWidget(
                              floatDistance: 3,
                              child: Container(
                                width: 52,
                                height: 52,
                                decoration: BoxDecoration(
                                  color: Colors.white.withValues(alpha: 0.22),
                                  borderRadius: BorderRadius.circular(16),
                                  border: Border.all(
                                    color: Colors.white.withValues(alpha: 0.35),
                                  ),
                                ),
                                child: const Icon(
                                  Icons.school_rounded,
                                  color: Colors.white,
                                  size: 30,
                                ),
                              ),
                            ),
                            const SizedBox(width: 14),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    children: [
                                      Text(
                                        headerTitle,
                                        style: const TextStyle(
                                          color: Colors.white,
                                          fontSize: 20,
                                          fontWeight: FontWeight.w800,
                                          letterSpacing: -0.3,
                                        ),
                                      ),
                                      const SizedBox(width: 8),
                                      Container(
                                        padding: const EdgeInsets.symmetric(
                                          horizontal: 8,
                                          vertical: 2,
                                        ),
                                        decoration: BoxDecoration(
                                          color: Colors.white.withValues(alpha: 0.22),
                                          borderRadius: BorderRadius.circular(10),
                                        ),
                                        child: Text(
                                          roleBadge,
                                          style: const TextStyle(
                                            color: Colors.white,
                                            fontSize: 11,
                                            fontWeight: FontWeight.w600,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 3),
                                  Text(
                                    'Xin chào, ${auth?.username ?? 'User'} • ${auth?.phoneNumber ?? ''}',
                                    style: TextStyle(
                                      color: Colors.white.withValues(alpha: 0.92),
                                      fontSize: 13,
                                    ),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ],
                              ),
                            ),
                            Stack(
                              clipBehavior: Clip.none,
                              children: [
                                PressableScale(
                                  onTap: () => _open(const NotificationsScreen()),
                                  child: Container(
                                    width: 42,
                                    height: 42,
                                    decoration: BoxDecoration(
                                      color: Colors.white,
                                      borderRadius: BorderRadius.circular(14),
                                    ),
                                    child: const Icon(
                                      Icons.notifications_rounded,
                                      color: AppColors.orange,
                                      size: 22,
                                    ),
                                  ),
                                ),
                                if (_unreadCount > 0)
                                  Positioned(
                                    right: -4,
                                    top: -4,
                                    child: TweenAnimationBuilder<double>(
                                      tween: Tween(begin: 0.6, end: 1.0),
                                      duration: const Duration(milliseconds: 350),
                                      curve: Curves.elasticOut,
                                      builder: (_, scale, child) =>
                                          Transform.scale(scale: scale, child: child),
                                      child: Container(
                                        padding: const EdgeInsets.symmetric(
                                          horizontal: 6,
                                          vertical: 2,
                                        ),
                                        decoration: BoxDecoration(
                                          color: AppColors.redFail,
                                          borderRadius: BorderRadius.circular(10),
                                          border: Border.all(
                                            color: Colors.white,
                                            width: 1.5,
                                          ),
                                        ),
                                        child: Text(
                                          _unreadCount > 99 ? '99+' : '$_unreadCount',
                                          style: const TextStyle(
                                            color: Colors.white,
                                            fontSize: 10,
                                            fontWeight: FontWeight.bold,
                                          ),
                                        ),
                                      ),
                                    ),
                                  ),
                              ],
                            ),
                            const SizedBox(width: 8),
                            PressableScale(
                              onTap: _handleLogout,
                              child: Container(
                                width: 42,
                                height: 42,
                                decoration: BoxDecoration(
                                  color: Colors.white.withValues(alpha: 0.2),
                                  borderRadius: BorderRadius.circular(14),
                                ),
                                child: const Icon(
                                  Icons.logout_rounded,
                                  color: Colors.white,
                                  size: 21,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),

                Positioned(
                  left: 20,
                  right: 20,
                  bottom: -28,
                  child: FadeSlideIn(
                    delay: const Duration(milliseconds: 80),
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 18,
                        vertical: 14,
                      ),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(20),
                        boxShadow: AppColors.softShadow(),
                      ),
                      child: Row(
                        children: [
                          Container(
                            width: 40,
                            height: 40,
                            decoration: BoxDecoration(
                              color: AppColors.orangeLight,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: const Icon(
                              Icons.campaign_rounded,
                              color: AppColors.orange,
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Năm học 2025 - 2026',
                                  style: TextStyle(
                                    fontWeight: FontWeight.w700,
                                    fontSize: 14,
                                    color: AppColors.textDark,
                                  ),
                                ),
                                Text(
                                  _unreadCount > 0
                                      ? 'Bạn có $_unreadCount thông báo mới từ nhà trường'
                                      : 'Mọi thông báo đã được cập nhật',
                                  style: const TextStyle(
                                    fontSize: 12,
                                    color: AppColors.textMuted,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          TextButton(
                            onPressed: () => _open(const NotificationsScreen()),
                            child: const Text(
                              'Xem',
                              style: TextStyle(
                                color: AppColors.orange,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),

          const SliverToBoxAdapter(child: SizedBox(height: 44)),

          const SliverToBoxAdapter(
            child: Padding(
              padding: EdgeInsets.symmetric(horizontal: 22, vertical: 6),
              child: Text(
                'Chức năng chính',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textDark,
                ),
              ),
            ),
          ),

          SliverPadding(
            padding: const EdgeInsets.fromLTRB(20, 6, 20, 28),
            sliver: SliverGrid(
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                crossAxisSpacing: 14,
                mainAxisSpacing: 14,
                childAspectRatio: 1.08,
              ),
              delegate: SliverChildBuilderDelegate(
                (context, index) {
                  final tile = tiles[index];
                  return FadeSlideIn(
                    delay: Duration(milliseconds: 90 + (index * 55)),
                    child: PressableScale(
                      onTap: tile.onTap,
                      child: Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(22),
                          boxShadow: AppColors.softShadow(),
                          border: Border.all(
                            color: AppColors.borderSubtle,
                            width: 1,
                          ),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Container(
                                  width: 50,
                                  height: 50,
                                  decoration: BoxDecoration(
                                    gradient: LinearGradient(
                                      colors: [
                                        tile.color.withValues(alpha: 0.18),
                                        tile.color.withValues(alpha: 0.08),
                                      ],
                                      begin: Alignment.topLeft,
                                      end: Alignment.bottomRight,
                                    ),
                                    borderRadius: BorderRadius.circular(16),
                                  ),
                                  child: Icon(
                                    tile.icon,
                                    color: tile.color,
                                    size: 28,
                                  ),
                                ),
                                Icon(
                                  Icons.arrow_forward_ios_rounded,
                                  size: 14,
                                  color: Colors.grey.shade400,
                                ),
                              ],
                            ),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  tile.title,
                                  style: const TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.w700,
                                    color: AppColors.textDark,
                                  ),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  tile.subtitle,
                                  style: const TextStyle(
                                    fontSize: 11.5,
                                    color: AppColors.textMuted,
                                  ),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ),
                  );
                },
                childCount: tiles.length,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
