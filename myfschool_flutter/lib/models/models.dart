class AuthData {
  final String accessToken;
  final String refreshToken;
  final String username;
  final String email;
  final String phoneNumber;
  final List<String> roles;
  final bool requiresTwoFactor;

  AuthData({
    required this.accessToken,
    required this.refreshToken,
    required this.username,
    required this.email,
    required this.phoneNumber,
    required this.roles,
    required this.requiresTwoFactor,
  });

  factory AuthData.fromJson(Map<String, dynamic> json) {
    return AuthData(
      accessToken: (json['accessToken'] ?? '').toString(),
      refreshToken: (json['refreshToken'] ?? '').toString(),
      username: (json['username'] ?? '').toString(),
      email: (json['email'] ?? '').toString(),
      phoneNumber: (json['phoneNumber'] ?? '').toString(),
      roles: (json['roles'] as List<dynamic>? ?? [])
          .map((e) => e.toString())
          .toList(),
      requiresTwoFactor: json['requiresTwoFactor'] == true,
    );
  }

  String get primaryRole {
    if (roles.any((r) => r.toLowerCase() == 'teacher')) return 'Teacher';
    if (roles.any((r) => r.toLowerCase() == 'parent')) return 'Parent';
    if (roles.any((r) => r.toLowerCase() == 'student')) return 'Student';
    if (roles.any((r) => r.toLowerCase() == 'admin')) return 'Admin';
    return roles.isNotEmpty ? roles.first : 'Student';
  }

  bool get isTeacher => roles.any((r) => r.toLowerCase() == 'teacher');
  bool get isParent => roles.any((r) => r.toLowerCase() == 'parent');
  bool get isStudent => roles.any((r) => r.toLowerCase() == 'student');
  bool get isAdmin => roles.any((r) => r.toLowerCase() == 'admin');
}

class GradeItem {
  final int gradeId;
  final String studentCode;
  final String studentFullName;
  final String academicYear;
  final String semester;
  final String subjectName;
  final String assessmentType;
  final double score;
  final String? grade;
  final String createdAt;

  GradeItem({
    required this.gradeId,
    required this.studentCode,
    required this.studentFullName,
    required this.academicYear,
    required this.semester,
    required this.subjectName,
    required this.assessmentType,
    required this.score,
    this.grade,
    required this.createdAt,
  });

  factory GradeItem.fromJson(Map<String, dynamic> json) {
    return GradeItem(
      gradeId: (json['gradeId'] ?? 0) as int,
      studentCode: (json['studentCode'] ?? '').toString(),
      studentFullName: (json['studentFullName'] ?? '').toString(),
      academicYear: (json['academicYear'] ?? '').toString(),
      semester: (json['semester'] ?? '').toString(),
      subjectName: (json['subjectName'] ?? '').toString(),
      assessmentType: (json['assessmentType'] ?? '').toString(),
      score: ((json['score'] ?? 0) as num).toDouble(),
      grade: json['grade']?.toString(),
      createdAt: (json['createdAt'] ?? '').toString(),
    );
  }
}

class SubjectMarkSummary {
  final String subjectName;
  final String semester;
  final String academicYear;
  final List<GradeItem> items;

  SubjectMarkSummary({
    required this.subjectName,
    required this.semester,
    required this.academicYear,
    required this.items,
  });

  double get average {
    if (items.isEmpty) return 0;
    double weightedSum = 0;
    double weightTotal = 0;
    for (final item in items) {
      final weight = switch (item.assessmentType.toLowerCase()) {
        'final' => 3.0,
        'midterm' => 2.0,
        _ => 1.0,
      };
      weightedSum += item.score * weight;
      weightTotal += weight;
    }
    if (weightTotal == 0) return 0;
    return double.parse((weightedSum / weightTotal).toStringAsFixed(1));
  }

  bool get isPassed => average >= 5.0;
}

class TimetableItem {
  final int id;
  final String subjectName;
  final String teacherName;
  final String dayOfWeek;
  final String startTime;
  final String endTime;
  final String className;
  final String weekStartDate;
  final String lessonDate;

  TimetableItem({
    required this.id,
    required this.subjectName,
    required this.teacherName,
    required this.dayOfWeek,
    required this.startTime,
    required this.endTime,
    required this.className,
    required this.weekStartDate,
    required this.lessonDate,
  });

  factory TimetableItem.fromJson(Map<String, dynamic> json) {
    return TimetableItem(
      id: (json['id'] ?? 0) as int,
      subjectName: (json['subjectName'] ?? '').toString(),
      teacherName: (json['teacherName'] ?? '').toString(),
      dayOfWeek: (json['dayOfWeek'] ?? '').toString(),
      startTime: (json['startTime'] ?? '').toString(),
      endTime: (json['endTime'] ?? '').toString(),
      className: (json['className'] ?? '').toString(),
      weekStartDate: (json['weekStartDate'] ?? '').toString(),
      lessonDate: (json['lessonDate'] ?? '').toString(),
    );
  }
}

class EventItem {
  final int id;
  final String title;
  final String description;
  final String eventDate;
  final String location;

  EventItem({
    required this.id,
    required this.title,
    required this.description,
    required this.eventDate,
    required this.location,
  });

  factory EventItem.fromJson(Map<String, dynamic> json) {
    return EventItem(
      id: (json['id'] ?? 0) as int,
      title: (json['title'] ?? '').toString(),
      description: (json['description'] ?? '').toString(),
      eventDate: (json['eventDate'] ?? '').toString(),
      location: (json['location'] ?? '').toString(),
    );
  }
}

class ClubItem {
  final int id;
  final String name;
  final String description;
  final String schedule;

  ClubItem({
    required this.id,
    required this.name,
    required this.description,
    required this.schedule,
  });

  factory ClubItem.fromJson(Map<String, dynamic> json) {
    return ClubItem(
      id: (json['id'] ?? 0) as int,
      name: (json['name'] ?? '').toString(),
      description: (json['description'] ?? '').toString(),
      schedule: (json['schedule'] ?? '').toString(),
    );
  }
}

class ApplicationItem {
  final int id;
  final String title;
  final String content;
  final String createdAt;
  final String createdBy;

  ApplicationItem({
    required this.id,
    required this.title,
    required this.content,
    required this.createdAt,
    required this.createdBy,
  });

  factory ApplicationItem.fromJson(Map<String, dynamic> json) {
    return ApplicationItem(
      id: (json['id'] ?? 0) as int,
      title: (json['title'] ?? '').toString(),
      content: (json['content'] ?? '').toString(),
      createdAt: (json['createdAt'] ?? '').toString(),
      createdBy: (json['createdBy'] ?? '').toString(),
    );
  }
}

class NotificationItem {
  final int id;
  final String title;
  final String content;
  final String createdAt;
  final String createdBy;
  final bool isRead;

  NotificationItem({
    required this.id,
    required this.title,
    required this.content,
    required this.createdAt,
    required this.createdBy,
    required this.isRead,
  });

  factory NotificationItem.fromJson(Map<String, dynamic> json) {
    return NotificationItem(
      id: (json['id'] ?? 0) as int,
      title: (json['title'] ?? '').toString(),
      content: (json['content'] ?? '').toString(),
      createdAt: (json['createdAt'] ?? '').toString(),
      createdBy: (json['createdBy'] ?? '').toString(),
      isRead: json['isRead'] == true,
    );
  }
}

class ProfileData {
  final String userId;
  final String username;
  final String email;
  final String phoneNumber;
  final bool isTwoFactorEnabled;
  final List<String> roles;

  ProfileData({
    required this.userId,
    required this.username,
    required this.email,
    required this.phoneNumber,
    required this.isTwoFactorEnabled,
    required this.roles,
  });

  factory ProfileData.fromJson(Map<String, dynamic> json) {
    return ProfileData(
      userId: (json['userId'] ?? '').toString(),
      username: (json['username'] ?? '').toString(),
      email: (json['email'] ?? '').toString(),
      phoneNumber: (json['phoneNumber'] ?? '').toString(),
      isTwoFactorEnabled: json['isTwoFactorEnabled'] == true,
      roles: (json['roles'] as List<dynamic>? ?? [])
          .map((e) => e.toString())
          .toList(),
    );
  }
}

class LookupOption {
  final String value;
  final String label;

  LookupOption({required this.value, required this.label});

  factory LookupOption.fromJson(Map<String, dynamic> json) {
    return LookupOption(
      value: (json['value'] ?? '').toString(),
      label: (json['label'] ?? '').toString(),
    );
  }
}

class AcademicLookups {
  final List<LookupOption> students;
  final List<LookupOption> subjects;
  final List<LookupOption> semesters;
  final List<LookupOption> classes;
  final List<LookupOption> teachers;

  AcademicLookups({
    required this.students,
    required this.subjects,
    required this.semesters,
    required this.classes,
    required this.teachers,
  });

  factory AcademicLookups.fromJson(Map<String, dynamic> json) {
    List<LookupOption> parseList(String key) =>
        (json[key] as List<dynamic>? ?? [])
            .map((e) => LookupOption.fromJson(e as Map<String, dynamic>))
            .toList();

    return AcademicLookups(
      students: parseList('students'),
      subjects: parseList('subjects'),
      semesters: parseList('semesters'),
      classes: parseList('classes'),
      teachers: parseList('teachers'),
    );
  }
}

class ManagementGradeItem {
  final int gradeId;
  final String studentId;
  final String studentCode;
  final String studentFullName;
  final int classId;
  final String className;
  final int subjectId;
  final String subjectName;
  final int semesterId;
  final String semester;
  final String academicYear;
  final String assessmentType;
  final double score;
  final String? grade;
  final String createdAt;

  ManagementGradeItem({
    required this.gradeId,
    required this.studentId,
    required this.studentCode,
    required this.studentFullName,
    required this.classId,
    required this.className,
    required this.subjectId,
    required this.subjectName,
    required this.semesterId,
    required this.semester,
    required this.academicYear,
    required this.assessmentType,
    required this.score,
    this.grade,
    required this.createdAt,
  });

  factory ManagementGradeItem.fromJson(Map<String, dynamic> json) {
    return ManagementGradeItem(
      gradeId: (json['gradeId'] ?? 0) as int,
      studentId: (json['studentId'] ?? '').toString(),
      studentCode: (json['studentCode'] ?? '').toString(),
      studentFullName: (json['studentFullName'] ?? '').toString(),
      classId: (json['classId'] ?? 0) as int,
      className: (json['className'] ?? '').toString(),
      subjectId: (json['subjectId'] ?? 0) as int,
      subjectName: (json['subjectName'] ?? '').toString(),
      semesterId: (json['semesterId'] ?? 0) as int,
      semester: (json['semester'] ?? '').toString(),
      academicYear: (json['academicYear'] ?? '').toString(),
      assessmentType: (json['assessmentType'] ?? '').toString(),
      score: ((json['score'] ?? 0) as num).toDouble(),
      grade: json['grade']?.toString(),
      createdAt: (json['createdAt'] ?? '').toString(),
    );
  }
}

class ManagementStudentItem {
  final String id;
  final String fullName;
  final String dateOfBirth;
  final String? gender;
  final int classId;
  final String className;
  final String studentCode;
  final String? userId;
  final List<String> parentIds;
  final bool isActive;

  ManagementStudentItem({
    required this.id,
    required this.fullName,
    required this.dateOfBirth,
    this.gender,
    required this.classId,
    required this.className,
    required this.studentCode,
    this.userId,
    required this.parentIds,
    required this.isActive,
  });

  factory ManagementStudentItem.fromJson(Map<String, dynamic> json) {
    return ManagementStudentItem(
      id: (json['id'] ?? '').toString(),
      fullName: (json['fullName'] ?? '').toString(),
      dateOfBirth: (json['dateOfBirth'] ?? '').toString(),
      gender: json['gender']?.toString(),
      classId: (json['classId'] ?? 0) as int,
      className: (json['className'] ?? '').toString(),
      studentCode: (json['studentCode'] ?? '').toString(),
      userId: json['userId']?.toString(),
      parentIds: (json['parentIds'] as List<dynamic>? ?? [])
          .map((e) => e.toString())
          .toList(),
      isActive: json['isActive'] != false,
    );
  }
}

class ManagementTimetableItem {
  final int id;
  final int classId;
  final String className;
  final int subjectId;
  final String subjectName;
  final String teacherId;
  final String teacherName;
  final String dayOfWeek;
  final String startTime;
  final String endTime;
  final String weekStartDate;

  ManagementTimetableItem({
    required this.id,
    required this.classId,
    required this.className,
    required this.subjectId,
    required this.subjectName,
    required this.teacherId,
    required this.teacherName,
    required this.dayOfWeek,
    required this.startTime,
    required this.endTime,
    required this.weekStartDate,
  });

  factory ManagementTimetableItem.fromJson(Map<String, dynamic> json) {
    return ManagementTimetableItem(
      id: (json['id'] ?? 0) as int,
      classId: (json['classId'] ?? 0) as int,
      className: (json['className'] ?? '').toString(),
      subjectId: (json['subjectId'] ?? 0) as int,
      subjectName: (json['subjectName'] ?? '').toString(),
      teacherId: (json['teacherId'] ?? '').toString(),
      teacherName: (json['teacherName'] ?? '').toString(),
      dayOfWeek: (json['dayOfWeek'] ?? '').toString(),
      startTime: (json['startTime'] ?? '').toString(),
      endTime: (json['endTime'] ?? '').toString(),
      weekStartDate: (json['weekStartDate'] ?? '').toString(),
    );
  }
}
