import 'package:flutter_test/flutter_test.dart';
import 'package:kiai_control_mobile/src/models.dart';

void main() {
  test('uses the academy check-in setting and server cancellation permission', () {
    final now = DateTime.now().toUtc();
    final lesson = ClientLesson.fromJson({
      'id': 'lesson-policy',
      'startsAt': now.add(const Duration(hours: 1)).toIso8601String(),
      'endsAt': now.add(const Duration(hours: 2)).toIso8601String(),
      'checkInStatus': 'present',
      'allowCheckIn': false,
      'canCancelCheckIn': true,
    });
    expect(lesson.canCheckIn, isFalse);
    expect(lesson.canCancelCheckIn, isTrue);
  });
  test('measures a reducing fitness goal from its initial value', () {
    final goal = FitnessGoal.fromJson({
      'title': 'Peso', 'direction': 'decrease', 'initialValue': 80,
      'currentValue': 75, 'targetValue': 70, 'status': 'active',
    });
    expect(goal.progress, .5);
  });

  group('ClientProfile', () {
    test('parses API response and prefers preferred name for display', () {
      final profile = ClientProfile.fromJson({
        'clientId': 'client-1',
        'fullName': 'Ana Silva',
        'preferredName': ' Ana ',
        'email': 'ana@example.test',
        'status': 'active',
      });

      expect(profile.clientId, 'client-1');
      expect(profile.displayName, 'Ana');
      expect(profile.email, 'ana@example.test');
      expect(profile.status, 'active');
    });
  });

  group('FitnessOverview', () {
    test('parses goal values and bounds progress between zero and one', () {
      final overview = FitnessOverview.fromJson({
        'activeGoals': 1,
        'achievedGoals': 0,
        'goals': [
          {
            'title': 'Treinos',
            'currentValue': 12,
            'targetValue': 10,
            'status': 'active',
            'unit': 'aulas',
          },
        ],
      });

      expect(overview.activeGoals, 1);
      expect(overview.goals.single.title, 'Treinos');
      expect(overview.goals.single.progress, 1);
      expect(overview.goals.single.unit, 'aulas');
    });
  });

  group('ClientLesson check-in eligibility', () {
    test('allows checking in for a lesson that has not started', () {
      final now = DateTime.now();
      final lesson = ClientLesson.fromJson({
        'id': 'lesson-1',
        'startsAt':
            now.add(const Duration(minutes: 5)).toUtc().toIso8601String(),
        'endsAt':
            now.add(const Duration(minutes: 65)).toUtc().toIso8601String(),
        'status': 'scheduled',
      });

      expect(lesson.canCheckIn, isTrue);
      expect(lesson.canCancelCheckIn, isFalse);
    });

    test('allows cancelling a pending check-in before the lesson starts', () {
      final now = DateTime.now();
      final lesson = ClientLesson.fromJson({
        'id': 'lesson-2',
        'startsAt':
            now.add(const Duration(minutes: 5)).toUtc().toIso8601String(),
        'endsAt':
            now.add(const Duration(minutes: 65)).toUtc().toIso8601String(),
        'status': 'scheduled',
        'checkInStatus': 'pending_confirmation',
      });

      expect(lesson.canCheckIn, isFalse);
      expect(lesson.canCancelCheckIn, isTrue);
    });

    test('does not allow changing check-in after the lesson starts', () {
      final now = DateTime.now();
      final lesson = ClientLesson.fromJson({
        'id': 'lesson-3',
        'startsAt':
            now.subtract(const Duration(minutes: 1)).toUtc().toIso8601String(),
        'endsAt':
            now.add(const Duration(minutes: 59)).toUtc().toIso8601String(),
        'status': 'scheduled',
        'checkInStatus': 'pending_confirmation',
      });

      expect(lesson.canCheckIn, isFalse);
      expect(lesson.canCancelCheckIn, isFalse);
    });
  });

  group('Teacher lesson', () {
    test('parses the lesson details used by the attendance screen', () {
      final lesson = TeacherLesson.fromJson({
        'id': 'lesson-4',
        'modalityName': 'Jiu-jitsu',
        'branchName': 'Centro',
        'startsAt': '2026-11-03T18:00:00Z',
        'endsAt': '2026-11-03T19:00:00Z',
        'status': 'scheduled',
      });

      expect(lesson.id, 'lesson-4');
      expect(lesson.modalityName, 'Jiu-jitsu');
      expect(lesson.branchName, 'Centro');
      expect(lesson.status, 'scheduled');
    });

    test('parses eligible students and check-in records', () {
      final client = EligibleClient.fromJson({
        'clientId': 'client-2',
        'clientName': 'Bruno Silva',
      });
      final attendance = AttendanceRecord.fromJson({
        'id': 'attendance-1',
        'clientId': 'client-2',
        'status': 'pending_confirmation',
      });

      expect(client.clientId, 'client-2');
      expect(client.clientName, 'Bruno Silva');
      expect(attendance.clientId, client.clientId);
      expect(attendance.status, 'pending_confirmation');
    });
  });
}
