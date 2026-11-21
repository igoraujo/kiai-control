import 'dart:convert';
import 'dart:typed_data';

Map<String, dynamic> object(dynamic value) {
  if (value is! Map<String, dynamic>) {
    throw const FormatException('Objeto inválido na resposta da API.');
  }
  return value;
}

List<Map<String, dynamic>> objects(dynamic value) {
  if (value is! List) {
    throw const FormatException('Lista inválida na resposta da API.');
  }
  return value.map(object).toList();
}

String requiredString(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value is! String || value.isEmpty) {
    throw FormatException('Campo obrigatório inválido: $key.');
  }
  return value;
}

class MobileContext {
  MobileContext.fromJson(Map<String, dynamic> json)
    : organizationName = requiredString(json, 'organizationName'),
      timezone = requiredString(json, 'timezone'),
      capabilities = (json['capabilities'] as List).cast<String>().toSet(),
      roles = (json['roles'] as List).cast<String>(),
      permissions = (json['permissions'] as List).cast<String>().toSet(),
      mustChangePassword = json['mustChangePassword'] as bool;

  final String organizationName;
  final String timezone;
  final Set<String> capabilities;
  final List<String> roles;
  final Set<String> permissions;
  final bool mustChangePassword;
  bool can(String capability) => capabilities.contains(capability);
}

class MobileAccount {
  MobileAccount.fromJson(Map<String, dynamic> json)
    : fullName = requiredString(json, 'fullName'),
      preferredName = json['preferredName'] as String?,
      email = json['email'] as String?,
      phone = json['phone'] as String?,
      hideFromRanking = json['hideFromRanking'] as bool,
      pushEnabled = json['pushEnabled'] as bool,
      photo = json['photoBase64'] == null
          ? null
          : base64Decode(json['photoBase64'] as String);

  final String fullName;
  final String? preferredName;
  final String? email;
  final String? phone;
  final bool hideFromRanking;
  final bool pushEnabled;
  final Uint8List? photo;
  String get displayName => preferredName?.trim().isNotEmpty == true
      ? preferredName!.trim()
      : fullName;
}

class MobileProgress {
  MobileProgress.fromJson(Map<String, dynamic> json)
    : year = json['year'] as int,
      totalClasses = json['totalClasses'] as int,
      yearClasses = json['yearClasses'] as int,
      currentStreak = json['currentStreak'] as int,
      totalPosition = json['totalPosition'] as int?,
      yearPosition = json['yearPosition'] as int?,
      months = objects(json['months']).map(AttendanceMonth.fromJson).toList(),
      graduations = objects(
        json['graduations'],
      ).map(GraduationProgress.fromJson).toList();

  final int year;
  final int totalClasses;
  final int yearClasses;
  final int currentStreak;
  final int? totalPosition;
  final int? yearPosition;
  final List<AttendanceMonth> months;
  final List<GraduationProgress> graduations;
}

class AppNotification {
  AppNotification.fromJson(Map<String, dynamic> json)
    : id = requiredString(json, 'id'),
      title = requiredString(json, 'title'),
      body = requiredString(json, 'body'),
      readAt = json['readAt'] == null
          ? null
          : DateTime.parse(json['readAt'] as String),
      createdAt = DateTime.parse(requiredString(json, 'createdAt')).toLocal();

  final String id;
  final String title;
  final String body;
  final DateTime? readAt;
  final DateTime createdAt;
}

class PendingCheckIn {
  PendingCheckIn.fromJson(Map<String, dynamic> json)
    : id = requiredString(json, 'id'),
      clientName = requiredString(json, 'clientName'),
      lessonId = requiredString(json, 'lessonId'),
      status = requiredString(json, 'status'),
      modalityName = requiredString(json, 'modalityName'),
      lessonStartsAt = DateTime.parse(
        requiredString(json, 'lessonStartsAt'),
      ).toLocal(),
      checkedInAt = DateTime.parse(
        requiredString(json, 'attendanceDate'),
      ).toLocal();
  final String id;
  final String clientName;
  final String lessonId;
  final String status;
  final DateTime checkedInAt;
  final String modalityName;
  final DateTime lessonStartsAt;
}

class RankingEntry {
  RankingEntry.fromJson(Map<String, dynamic> j)
    : clientName = requiredString(j, 'clientName'),
      position = j['position'] as int,
      attendanceCount = j['attendanceCount'] as int,
      isMe = j['isMe'] as bool;
  final String clientName;
  final int position;
  final int attendanceCount;
  final bool isMe;
}

class AttendanceMonth {
  AttendanceMonth.fromJson(Map<String, dynamic> j)
    : month = requiredString(j, 'month'),
      count = j['attendanceCount'] as int;
  final String month;
  final int count;
}

class GraduationProgress {
  GraduationProgress.fromJson(Map<String, dynamic> j)
    : modalityName = requiredString(j, 'modalityName'),
      beltName = requiredString(j, 'beltName'),
      degreeNumber = j['degreeNumber'] as int,
      nextLevelName = j['nextLevelName'] as String?,
      lessonsRemaining = j['lessonsRemaining'] as int?;
  final String modalityName;
  final String beltName;
  final int degreeNumber;
  final String? nextLevelName;
  final int? lessonsRemaining;
}

class DashboardData {
  DashboardData.fromJson(Map<String, dynamic> j)
    : activeClients = j['activeClients'] as int,
      newClients = j['newClients'] as int,
      completedLessons = j['completedLessons'] as int,
      attendanceRate = j['attendanceRate'] as num,
      revenueExpected = j['revenueExpected'] as num,
      revenueReceived = j['revenueReceived'] as num,
      revenueOverdue = j['revenueOverdue'] as num;
  final int activeClients;
  final int newClients;
  final int completedLessons;
  final num attendanceRate;
  final num revenueExpected;
  final num revenueReceived;
  final num revenueOverdue;
}

class JourneyAttendance {
  JourneyAttendance.fromJson(Map<String, dynamic> j)
    : modalityName = requiredString(j, 'modalityName'),
      status = requiredString(j, 'status'),
      startsAt = DateTime.parse(requiredString(j, 'startsAt')).toLocal();
  final String modalityName;
  final String status;
  final DateTime startsAt;
}

class JourneyMedal {
  JourneyMedal.fromJson(Map<String, dynamic> j)
    : name = requiredString(j, 'name'),
      reason = j['reason'] as String?;
  final String name;
  final String? reason;
}

class StudentJourney {
  StudentJourney.fromJson(Map<String, dynamic> j)
    : medals = objects(j['medals']).map(JourneyMedal.fromJson).toList(),
      attendance = objects(
        object(j['attendance'])['items'],
      ).map(JourneyAttendance.fromJson).toList(),
      attendanceTotal = object(j['attendance'])['total'] as int;
  final List<JourneyMedal> medals;
  final List<JourneyAttendance> attendance;
  final int attendanceTotal;
}

class FitnessProgressRecord {
  FitnessProgressRecord.fromJson(Map<String, dynamic> j)
    : value = j['recordedValue'] as num,
      notes = j['notes'] as String?,
      recordedAt = DateTime.parse(requiredString(j, 'recordedAt')).toLocal();
  final num value;
  final String? notes;
  final DateTime recordedAt;
}
