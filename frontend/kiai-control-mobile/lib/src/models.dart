import 'mobile_models.dart';

class ClientProfile {
  const ClientProfile({
    required this.clientId,
    required this.fullName,
    required this.status,
    this.preferredName,
    this.email,
    this.phone,
    this.joinedAt,
  });

  final String clientId;
  final String fullName;
  final String status;
  final String? preferredName;
  final String? email;
  final String? phone;
  final DateTime? joinedAt;

  String get displayName => preferredName?.trim().isNotEmpty == true
      ? preferredName!.trim()
      : fullName;

  factory ClientProfile.fromJson(Map<String, dynamic> json) => ClientProfile(
    clientId: json['clientId'] as String,
    fullName: requiredString(json, 'fullName'),
    preferredName: json['preferredName'] as String?,
    email: json['email'] as String?,
    phone: json['phone'] as String?,
    joinedAt: json['joinedAt'] == null
        ? null
        : DateTime.parse(json['joinedAt'] as String),
    status: requiredString(json, 'status'),
  );
}

class ClientLesson {
  const ClientLesson({
    required this.id,
    required this.modalityName,
    required this.startsAt,
    required this.endsAt,
    required this.status,
    this.branchName,
    this.checkInStatus,
    this.allowCheckIn = false,
    this.checkInAllowed = false,
    this.cancellable,
  });

  final String id;
  final String modalityName;
  final DateTime startsAt;
  final DateTime endsAt;
  final String status;
  final String? branchName;
  final String? checkInStatus;
  final bool allowCheckIn;
  final bool checkInAllowed;
  final bool? cancellable;

  bool get canCheckIn => checkInAllowed;
  bool get canCancelCheckIn => cancellable ?? false;

  factory ClientLesson.fromJson(Map<String, dynamic> json) => ClientLesson(
    id: json['id'] as String,
    modalityName: json['modalityName'] as String? ?? 'Aula',
    startsAt: DateTime.parse(json['startsAt'] as String).toLocal(),
    endsAt: DateTime.parse(json['endsAt'] as String).toLocal(),
    status: requiredString(json, 'status'),
    branchName: json['branchName'] as String?,
    checkInStatus: json['checkInStatus'] as String?,
    allowCheckIn: json['allowCheckIn'] as bool? ?? false,
    checkInAllowed: json['canCheckIn'] as bool? ?? false,
    cancellable: json['canCancelCheckIn'] as bool?,
  );
}

class TeacherProfile {
  const TeacherProfile({
    required this.teacherId,
    required this.fullName,
    this.preferredName,
  });

  final String teacherId;
  final String fullName;
  final String? preferredName;

  String get displayName => preferredName?.trim().isNotEmpty == true
      ? preferredName!.trim()
      : fullName;

  factory TeacherProfile.fromJson(Map<String, dynamic> json) => TeacherProfile(
    teacherId: json['teacherId'] as String,
    fullName: requiredString(json, 'fullName'),
    preferredName: json['preferredName'] as String?,
  );
}

class TeacherLesson {
  const TeacherLesson({
    required this.id,
    required this.startsAt,
    required this.endsAt,
    required this.status,
    this.modalityName,
    this.branchName,
  });

  final String id;
  final DateTime startsAt;
  final DateTime endsAt;
  final String status;
  final String? modalityName;
  final String? branchName;

  factory TeacherLesson.fromJson(Map<String, dynamic> json) => TeacherLesson(
    id: json['id'] as String,
    startsAt: DateTime.parse(json['startsAt'] as String).toLocal(),
    endsAt: DateTime.parse(json['endsAt'] as String).toLocal(),
    status: requiredString(json, 'status'),
    modalityName: json['modalityName'] as String?,
    branchName: json['branchName'] as String?,
  );
}

class EligibleClient {
  const EligibleClient({required this.clientId, required this.clientName});

  final String clientId;
  final String clientName;

  factory EligibleClient.fromJson(Map<String, dynamic> json) => EligibleClient(
    clientId: json['clientId'] as String,
    clientName: requiredString(json, 'clientName'),
  );
}

class AttendanceRecord {
  const AttendanceRecord({
    required this.id,
    required this.clientId,
    required this.status,
  });

  final String id;
  final String clientId;
  final String status;

  factory AttendanceRecord.fromJson(Map<String, dynamic> json) =>
      AttendanceRecord(
        id: json['id'] as String,
        clientId: json['clientId'] as String,
        status: requiredString(json, 'status'),
      );
}

class FitnessGoal {
  const FitnessGoal({
    this.id,
    required this.title,
    required this.currentValue,
    required this.targetValue,
    required this.status,
    this.unit,
    this.direction = 'increase',
    this.initialValue = 0,
    this.dueDate,
  });

  final String? id;
  final String title;
  final double currentValue;
  final double targetValue;
  final String status;
  final String? unit;
  final String direction;
  final double initialValue;
  final DateTime? dueDate;

  double get progress {
    if (direction == 'decrease') {
      return initialValue <= targetValue
          ? 0
          : ((initialValue - currentValue) / (initialValue - targetValue))
                .clamp(0, 1)
                .toDouble();
    }
    return targetValue <= 0
        ? 0
        : (currentValue / targetValue).clamp(0, 1).toDouble();
  }

  factory FitnessGoal.fromJson(Map<String, dynamic> json) => FitnessGoal(
    id: json['id'] as String?,
    title: requiredString(json, 'title'),
    currentValue: (json['currentValue'] as num).toDouble(),
    targetValue: (json['targetValue'] as num).toDouble(),
    status: requiredString(json, 'status'),
    unit: json['unit'] as String?,
    direction: json['direction'] as String? ?? 'increase',
    initialValue: (json['initialValue'] as num?)?.toDouble() ?? 0,
    dueDate: json['dueDate'] == null
        ? null
        : DateTime.parse(json['dueDate'] as String),
  );
}

class FitnessOverview {
  const FitnessOverview({
    required this.activeGoals,
    required this.achievedGoals,
    required this.goals,
  });

  final int activeGoals;
  final int achievedGoals;
  final List<FitnessGoal> goals;

  factory FitnessOverview.fromJson(Map<String, dynamic> json) =>
      FitnessOverview(
        activeGoals: json['activeGoals'] as int,
        achievedGoals: json['achievedGoals'] as int,
        goals: (json['goals'] as List<dynamic>)
            .map((item) => FitnessGoal.fromJson(item as Map<String, dynamic>))
            .toList(),
      );
}
