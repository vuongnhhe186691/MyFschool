import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../core/api_config.dart';
import '../models/models.dart';

class ApiException implements Exception {
  final String message;
  final int? statusCode;
  ApiException(this.message, [this.statusCode]);

  @override
  String toString() => message;
}

class ApiService {
  ApiService._();
  static final ApiService instance = ApiService._();

  static const _kAccessToken = 'auth_access_token';
  static const _kRefreshToken = 'auth_refresh_token';
  static const _kUsername = 'auth_username';
  static const _kEmail = 'auth_email';
  static const _kPhone = 'auth_phone';
  static const _kRoles = 'auth_roles';

  AuthData? _currentAuth;
  AuthData? get currentAuth => _currentAuth;

  Future<AuthData?> loadSavedSession() async {
    final prefs = await SharedPreferences.getInstance();
    final accessToken = prefs.getString(_kAccessToken);
    if (accessToken == null || accessToken.isEmpty) return null;

    _currentAuth = AuthData(
      accessToken: accessToken,
      refreshToken: prefs.getString(_kRefreshToken) ?? '',
      username: prefs.getString(_kUsername) ?? '',
      email: prefs.getString(_kEmail) ?? '',
      phoneNumber: prefs.getString(_kPhone) ?? '',
      roles: prefs.getStringList(_kRoles) ?? [],
      requiresTwoFactor: false,
    );
    return _currentAuth;
  }

  Future<void> _saveSession(AuthData auth) async {
    _currentAuth = auth;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_kAccessToken, auth.accessToken);
    await prefs.setString(_kRefreshToken, auth.refreshToken);
    await prefs.setString(_kUsername, auth.username);
    await prefs.setString(_kEmail, auth.email);
    await prefs.setString(_kPhone, auth.phoneNumber);
    await prefs.setStringList(_kRoles, auth.roles);
  }

  Future<void> clearSession() async {
    _currentAuth = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_kAccessToken);
    await prefs.remove(_kRefreshToken);
    await prefs.remove(_kUsername);
    await prefs.remove(_kEmail);
    await prefs.remove(_kPhone);
    await prefs.remove(_kRoles);
  }

  Future<Map<String, String>> _headers({bool authenticated = true}) async {
    final headers = <String, String>{'Content-Type': 'application/json'};
    if (authenticated && _currentAuth != null && _currentAuth!.accessToken.isNotEmpty) {
      headers['Authorization'] = 'Bearer ${_currentAuth!.accessToken}';
    }
    return headers;
  }

  Future<http.Response> _request(
    String method,
    String path, {
    Object? body,
    bool authenticated = true,
    bool allowRefresh = true,
  }) async {
    final baseUrl = await ApiConfig.getBaseUrl();
    final uri = Uri.parse('$baseUrl$path');
    final headers = await _headers(authenticated: authenticated);
    final encodedBody = body != null ? jsonEncode(body) : null;

    http.Response response;
    switch (method.toUpperCase()) {
      case 'GET':
        response = await http.get(uri, headers: headers);
        break;
      case 'POST':
        response = await http.post(uri, headers: headers, body: encodedBody);
        break;
      case 'PUT':
        response = await http.put(uri, headers: headers, body: encodedBody);
        break;
      case 'DELETE':
        response = await http.delete(uri, headers: headers);
        break;
      default:
        throw ApiException('Unsupported HTTP method: $method');
    }

    if (response.statusCode == 401 && authenticated && allowRefresh && (_currentAuth?.refreshToken.isNotEmpty ?? false)) {
      final refreshed = await _tryRefreshToken();
      if (refreshed) {
        return _request(method, path, body: body, authenticated: authenticated, allowRefresh: false);
      }
    }

    return response;
  }

  Future<bool> _tryRefreshToken() async {
    try {
      final baseUrl = await ApiConfig.getBaseUrl();
      final res = await http.post(
        Uri.parse('$baseUrl/api/auth/refresh'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'refreshToken': _currentAuth?.refreshToken ?? ''}),
      );
      if (res.statusCode == 200) {
        final decoded = jsonDecode(res.body) as Map<String, dynamic>;
        final code = decoded['code'] as int? ?? 200;
        if (code == 200 && decoded['data'] != null) {
          final newAuth = AuthData.fromJson(decoded['data'] as Map<String, dynamic>);
          await _saveSession(newAuth);
          return true;
        }
      }
    } catch (_) {}
    await clearSession();
    return false;
  }

  String _extractErrorMessage(http.Response res, {String fallback = 'Có lỗi xảy ra'}) {
    try {
      final decoded = jsonDecode(res.body);
      if (decoded is Map<String, dynamic>) {
        if (decoded['message'] != null && decoded['message'].toString().isNotEmpty) {
          return decoded['message'].toString();
        }
        if (decoded['status'] != null && decoded['status'].toString().isNotEmpty) {
          return decoded['status'].toString();
        }
        if (decoded['errors'] is List && (decoded['errors'] as List).isNotEmpty) {
          return (decoded['errors'] as List).join('\n');
        }
      }
    } catch (_) {}
    return '$fallback (HTTP ${res.statusCode})';
  }

  Future<AuthData> login(String phoneNumber, String password) async {
    final res = await _request(
      'POST',
      '/api/auth/login',
      authenticated: false,
      body: {'phoneNumber': phoneNumber.trim(), 'password': password},
    );

    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    if (res.statusCode != 200 || decoded['data'] == null) {
      throw ApiException(
        decoded['message']?.toString() ?? decoded['status']?.toString() ?? 'Đăng nhập thất bại',
        res.statusCode,
      );
    }

    final auth = AuthData.fromJson(decoded['data'] as Map<String, dynamic>);
    if (!auth.requiresTwoFactor) {
      if (auth.isAdmin && !auth.isTeacher && !auth.isParent && !auth.isStudent) {
        throw ApiException('Tài khoản Admin vui lòng đăng nhập tại cổng Web Razor Pages.');
      }
      await _saveSession(auth);
    }
    return auth;
  }

  Future<AuthData> verifyLoginOtp(String phoneNumber, String otpCode) async {
    final res = await _request(
      'POST',
      '/api/auth/verify-login-otp',
      authenticated: false,
      body: {'phoneNumber': phoneNumber.trim(), 'otpCode': otpCode.trim()},
    );

    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    if (res.statusCode != 200 || decoded['data'] == null) {
      throw ApiException(
        decoded['message']?.toString() ?? decoded['status']?.toString() ?? 'Xác thực OTP thất bại',
        res.statusCode,
      );
    }

    final auth = AuthData.fromJson(decoded['data'] as Map<String, dynamic>);
    await _saveSession(auth);
    return auth;
  }

  Future<String> requestForgotPasswordOtp(String phoneNumber) async {
    final res = await _request(
      'POST',
      '/api/auth/forgot-password/request-otp',
      authenticated: false,
      body: {'phoneNumber': phoneNumber.trim()},
    );
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không thể gửi OTP'), res.statusCode);
    }
    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    return decoded['message']?.toString() ?? 'Đã gửi OTP về email của tài khoản';
  }

  Future<String> resetPassword({
    required String phoneNumber,
    required String otpCode,
    required String newPassword,
  }) async {
    final res = await _request(
      'POST',
      '/api/auth/forgot-password/reset',
      authenticated: false,
      body: {
        'phoneNumber': phoneNumber.trim(),
        'otpCode': otpCode.trim(),
        'newPassword': newPassword,
      },
    );
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Đặt lại mật khẩu thất bại'), res.statusCode);
    }
    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    return decoded['message']?.toString() ?? 'Đặt lại mật khẩu thành công';
  }

  Future<String> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    final res = await _request(
      'POST',
      '/api/auth/change-password',
      body: {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
      },
    );
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Đổi mật khẩu thất bại'), res.statusCode);
    }
    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    return decoded['message']?.toString() ?? 'Đổi mật khẩu thành công';
  }

  Future<void> logout() async {
    try {
      if (_currentAuth?.refreshToken.isNotEmpty ?? false) {
        await _request(
          'POST',
          '/api/auth/logout',
          body: {'refreshToken': _currentAuth!.refreshToken},
          allowRefresh: false,
        );
      }
    } catch (_) {}
    await clearSession();
  }

  Future<List<GradeItem>> getStudentGrades() async {
    final res = await _request('GET', '/api/student/grades');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không tải được bảng điểm'), res.statusCode);
    }
    final list = jsonDecode(res.body) as List<dynamic>;
    return list.map((e) => GradeItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<TimetableItem>> getWeeklyTimetable() async {
    final res = await _request('GET', '/api/student/weekly-timetable');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không tải được thời khóa biểu'), res.statusCode);
    }
    final list = jsonDecode(res.body) as List<dynamic>;
    return list.map((e) => TimetableItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<EventItem>> getEvents() async {
    final res = await _request('GET', '/api/student/events');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không tải được sự kiện'), res.statusCode);
    }
    final list = jsonDecode(res.body) as List<dynamic>;
    return list.map((e) => EventItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<ClubItem>> getClubs() async {
    final res = await _request('GET', '/api/student/clubs');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không tải được danh sách CLB'), res.statusCode);
    }
    final list = jsonDecode(res.body) as List<dynamic>;
    return list.map((e) => ClubItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<List<NotificationItem>> getUserNotifications() async {
    final res = await _request('GET', '/api/user-notifications');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không tải được thông báo'), res.statusCode);
    }
    final list = jsonDecode(res.body) as List<dynamic>;
    return list.map((e) => NotificationItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<int> getUnreadNotificationCount() async {
    final res = await _request('GET', '/api/user-notifications/unread-count');
    if (res.statusCode != 200) return 0;
    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    return (decoded['unreadCount'] ?? 0) as int;
  }

  Future<void> markNotificationRead(int id) async {
    final res = await _request('PUT', '/api/user-notifications/$id/read');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không thể đánh dấu đã đọc'), res.statusCode);
    }
  }

  Future<List<ApplicationItem>> getMyApplications() async {
    final res = await _request('GET', '/api/applications/my');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không tải được đơn từ'), res.statusCode);
    }
    final list = jsonDecode(res.body) as List<dynamic>;
    return list.map((e) => ApplicationItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<String> createApplication(String title, String content) async {
    final res = await _request(
      'POST',
      '/api/applications',
      body: {'title': title.trim(), 'content': content.trim()},
    );
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Gửi đơn thất bại'), res.statusCode);
    }
    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    return decoded['message']?.toString() ?? 'Gửi đơn thành công';
  }

  Future<ProfileData> getMyProfile() async {
    final res = await _request('GET', '/api/profile/me');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không tải được hồ sơ'), res.statusCode);
    }
    return ProfileData.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<String> updateMyProfile({
    required String username,
    required String email,
    required String phoneNumber,
  }) async {
    final res = await _request(
      'PUT',
      '/api/profile/me',
      body: {
        'username': username.trim(),
        'email': email.trim(),
        'phoneNumber': phoneNumber.trim(),
      },
    );
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Cập nhật thất bại'), res.statusCode);
    }
    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    if (_currentAuth != null) {
      await _saveSession(AuthData(
        accessToken: _currentAuth!.accessToken,
        refreshToken: _currentAuth!.refreshToken,
        username: username.trim(),
        email: email.trim(),
        phoneNumber: phoneNumber.trim(),
        roles: _currentAuth!.roles,
        requiresTwoFactor: false,
      ));
    }
    return decoded['message']?.toString() ?? 'Cập nhật thành công';
  }

  Future<String> updateTwoFactor(bool isEnabled) async {
    final res = await _request(
      'PUT',
      '/api/profile/two-factor',
      body: {'isEnabled': isEnabled},
    );
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không cập nhật được 2FA'), res.statusCode);
    }
    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    return decoded['message']?.toString() ?? 'Cập nhật 2FA thành công';
  }

  Future<AcademicLookups> getLookups() async {
    final res = await _request('GET', '/api/academic-management/lookups');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không tải được lookups'), res.statusCode);
    }
    return AcademicLookups.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<List<ManagementGradeItem>> getManagementGrades() async {
    final res = await _request('GET', '/api/academic-management/grades');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không tải được bảng điểm'), res.statusCode);
    }
    final list = jsonDecode(res.body) as List<dynamic>;
    return list.map((e) => ManagementGradeItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<String> updateGrade({
    required int gradeId,
    required String studentId,
    required int subjectId,
    required int semesterId,
    required String assessmentType,
    required double score,
    String? grade,
  }) async {
    final res = await _request(
      'PUT',
      '/api/academic-management/grades/$gradeId',
      body: {
        'studentId': studentId,
        'subjectId': subjectId,
        'semesterId': semesterId,
        'assessmentType': assessmentType,
        'score': score,
        'grade': grade,
      },
    );
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Cập nhật điểm thất bại'), res.statusCode);
    }
    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    return decoded['message']?.toString() ?? 'Cập nhật điểm thành công';
  }

  Future<List<ManagementTimetableItem>> getManagementTimetables({String? weekStartDate}) async {
    final query = (weekStartDate != null && weekStartDate.isNotEmpty)
        ? '?weekStartDate=$weekStartDate'
        : '';
    final res = await _request('GET', '/api/academic-management/timetables$query');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không tải được lịch dạy'), res.statusCode);
    }
    final list = jsonDecode(res.body) as List<dynamic>;
    return list.map((e) => ManagementTimetableItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<String> createTimetable({
    required int classId,
    required int subjectId,
    required String teacherId,
    required String dayOfWeek,
    required String startTime,
    required String endTime,
    required String weekStartDate,
  }) async {
    final res = await _request(
      'POST',
      '/api/academic-management/timetables',
      body: {
        'classId': classId,
        'subjectId': subjectId,
        'teacherId': teacherId,
        'dayOfWeek': dayOfWeek,
        'startTime': startTime,
        'endTime': endTime,
        'weekStartDate': weekStartDate,
      },
    );
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Tạo lịch học thất bại'), res.statusCode);
    }
    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    return decoded['message']?.toString() ?? 'Tạo lịch học thành công';
  }

  Future<List<ManagementStudentItem>> getManagementStudents({int? classId}) async {
    final query = classId != null ? '?classId=$classId' : '';
    final res = await _request('GET', '/api/academic-management/students$query');
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Không tải được danh sách học sinh'), res.statusCode);
    }
    final list = jsonDecode(res.body) as List<dynamic>;
    return list.map((e) => ManagementStudentItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<String> assignStudentsToClass({
    required int targetClassId,
    required List<String> studentIds,
  }) async {
    final res = await _request(
      'POST',
      '/api/academic-management/students/assign-class',
      body: {
        'targetClassId': targetClassId,
        'studentIds': studentIds,
      },
    );
    if (res.statusCode != 200) {
      throw ApiException(_extractErrorMessage(res, fallback: 'Chuyển lớp thất bại'), res.statusCode);
    }
    final decoded = jsonDecode(res.body) as Map<String, dynamic>;
    return decoded['message']?.toString() ?? 'Chuyển lớp thành công';
  }
}
