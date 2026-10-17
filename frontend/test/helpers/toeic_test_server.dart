import 'dart:async';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:flutter_application_1/toeic/api/toeic_api.dart';

/// Isolated HTTP contract fixtures. Never imported by production code or SQL.
class ToeicTestServer {
  ToeicTestServer({
    this.empty = false,
    this.expired = false,
    this.failSubmit = false,
  });
  final bool empty;
  bool expired;
  bool failSubmit;
  int bootstrapCalls = 0;
  int submitCalls = 0;
  int attemptCalls = 0;
  final List<Map<String, dynamic>> saves = [];
  final Map<String, String?> answers = {};
  Completer<void>? saveGate;
  Completer<void>? attemptGate;
  Completer<void>? submitGate;
  bool failSave = false;
  bool rejectSave = false;
  bool omitFinalizedSaveResult = false;
  bool submitted = false;
  DateTime now = DateTime.utc(2026, 10, 17, 7);
  late final ToeicApi api = ToeicApi(
    client: MockClient(handle),
    baseUrl: 'http://toeic.test',
    installationId: () async => List.filled(64, 'a').join(),
  );

  http.Response wrap(dynamic result, {int status = 200}) => http.Response(
    jsonEncode({'result': result, 'success': true, '__abp': true}),
    status,
    headers: {'content-type': 'application/json; charset=utf-8'},
  );

  Future<http.Response> handle(http.Request request) async {
    final path = request.url.path;
    if (path == '/api/demo/student-session') {
      bootstrapCalls++;
      return wrap({
        'accessToken': 'student-token',
        'expireInSeconds': 3600,
        'userId': 42,
      });
    }
    if (request.headers['Authorization'] != 'Bearer student-token') {
      return http.Response('{}', 401);
    }
    if (path.endsWith('/GetPublishedExams')) {
      return wrap({
        'items': empty ? [] : [exam],
      });
    }
    if (path.endsWith('/GetMyAttempts')) return wrap({'items': []});
    if (path.endsWith('/Start')) {
      return wrap({
        'attemptId': 'attempt-1',
        'startedAtUtc': now.toIso8601String(),
        'dueAtUtc': now.add(const Duration(minutes: 75)).toIso8601String(),
        'resumedExistingAttempt': false,
      });
    }
    if (path.endsWith('/GetAttempt')) {
      attemptCalls++;
      final snapshot = attempt;
      await attemptGate?.future;
      return wrap(snapshot);
    }
    if (path.endsWith('/SaveAnswer')) {
      final body = jsonDecode(request.body) as Map<String, dynamic>;
      saves.add(body);
      await saveGate?.future;
      if (failSave) {
        return http.Response(
          jsonEncode({
            'success': false,
            'error': {'message': 'Save offline test'},
          }),
          503,
        );
      }
      if (expired || submitted) {
        submitted = true;
        return wrap({
          'accepted': false,
          'isFinalized': true,
          'result': omitFinalizedSaveResult ? null : result,
        });
      }
      if (rejectSave) {
        return wrap({'accepted': false, 'isFinalized': false, 'result': null});
      }
      answers[body['questionId'] as String] =
          body['selectedOptionId'] as String?;
      return wrap({'accepted': true, 'isFinalized': false, 'result': null});
    }
    if (path.endsWith('/Submit')) {
      submitCalls++;
      await submitGate?.future;
      if (failSubmit) {
        return http.Response(
          jsonEncode({
            'success': false,
            'error': {'message': 'Offline test'},
          }),
          503,
        );
      }
      submitted = true;
      return wrap(result);
    }
    if (path == '/ToeicContent/part7.png') {
      return http.Response.bytes(
        base64Decode(
          'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jgWQAAAAASUVORK5CYII=',
        ),
        200,
        headers: {'content-type': 'image/png'},
      );
    }
    return http.Response('{}', 404);
  }

  Map<String, dynamic> get exam => {
    'id': 'exam-1',
    'code': 'TEST-ONLY',
    'title': 'TOEIC Reading — Contract Test',
    'durationMinutes': 75,
    'questionCount': 100,
    'canStart': true,
    'directions': 'Read carefully before answering.',
    'parts': [
      for (final p in [5, 6, 7])
        {
          'partNumber': p,
          'directions': 'Directions for Part $p',
          'questionCount': p == 5
              ? 30
              : p == 6
              ? 16
              : 54,
        },
    ],
  };

  Map<String, dynamic> get attempt => {
    'attemptId': 'attempt-1',
    'examId': 'exam-1',
    'examTitle': exam['title'],
    'serverNowUtc': now.toIso8601String(),
    'startedAtUtc': now.subtract(const Duration(minutes: 1)).toIso8601String(),
    'dueAtUtc': expired
        ? now.subtract(const Duration(seconds: 1)).toIso8601String()
        : now.add(const Duration(minutes: 74)).toIso8601String(),
    'status': submitted ? (expired ? 2 : 1) : 0,
    'result': submitted ? result : null,
    'groups': [
      {
        'id': 'group6',
        'title': 'Part 6 Email',
        'part': 6,
        'displayOrder': 1,
        'contentBlocks': [
          {
            'id': 'text6',
            'documentKey': 'email6',
            'displayOrder': 1,
            'type': 1,
            'textContent':
                'Dear team, Please review _____ (131) _____ before Friday. [1]',
          },
        ],
      },
      {
        'id': 'group7',
        'title': 'Part 7 Multi-document',
        'part': 7,
        'displayOrder': 2,
        'contentBlocks': [
          {
            'id': 'text7',
            'documentKey': 'notice7',
            'displayOrder': 1,
            'type': 1,
            'textContent':
                'From: Cedar Travel\nSubject: Annual meeting\n${List.filled(20, 'Please read the schedule and note [1], [2], [3], [4].').join('\n')}',
          },
          {
            'id': 'table7',
            'documentKey': 'schedule7',
            'displayOrder': 2,
            'type': 3,
            'textContent':
                '<table><tr><th>Session</th><th>Time</th><th>Room</th><th>Speaker</th></tr><tr><td>Annual meeting</td><td>09:00</td><td>Cedar Hall</td><td>Ms. Lee</td></tr></table>',
          },
          {
            'id': 'image7',
            'documentKey': 'photo7',
            'displayOrder': 3,
            'type': 2,
            'contentUrl': '/ToeicContent/part7.png',
            'mediaType': 'image/png',
          },
        ],
      },
    ],
    'questions': [
      for (var i = 101; i <= 200; i++)
        {
          'id': 'q$i',
          'part': i <= 130
              ? 5
              : i <= 146
              ? 6
              : 7,
          'originalNumber': i,
          'displayOrder': i - 100,
          'prompt': 'Question $i: Select the best answer for this sentence.',
          'passageGroupId': i <= 130
              ? null
              : i <= 146
              ? 'group6'
              : 'group7',
          'selectedOptionId': answers['q$i'],
          'options': [
            for (var n = 0; n < 4; n++)
              {
                'id': 'q$i-o$n',
                'key': String.fromCharCode(65 + n),
                'text': 'Choice ${String.fromCharCode(65 + n)} for question $i',
              },
          ],
        },
    ],
  };

  Map<String, dynamic> get result {
    final selected = answers.values.whereType<String>().length;
    final correct = answers.values
        .where((value) => value?.endsWith('-o0') == true)
        .length;
    return {
      'attemptId': 'attempt-1',
      'status': expired ? 2 : 1,
      'submittedAtUtc': now.toIso8601String(),
      'totalQuestions': 100,
      'correctCount': correct,
      'incorrectCount': selected - correct,
      'skippedCount': 100 - selected,
      'correctPercentage': correct.toDouble(),
    };
  }
}
