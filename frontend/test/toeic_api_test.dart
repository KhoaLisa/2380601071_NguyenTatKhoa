import 'dart:async';
import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:flutter_application_1/toeic/api/demo_identity_store.dart';
import 'package:flutter_application_1/toeic/api/toeic_api.dart';
import 'helpers/toeic_test_server.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  final startedAt = DateTime.utc(2026, 10, 17, 10);
  Map<String, dynamic> startResult() => {
    'attemptId': 'committed-attempt',
    'startedAtUtc': startedAt.toIso8601String(),
    'dueAtUtc': startedAt.add(const Duration(minutes: 75)).toIso8601String(),
    'resumedExistingAttempt': false,
  };
  http.Response ok(dynamic result) => http.Response(
    jsonEncode({'success': true, 'result': result}),
    200,
    headers: {'content-type': 'application/json; charset=utf-8'},
  );
  http.Response session() =>
      ok({'accessToken': 'token', 'expireInSeconds': 3600});

  test(
    'Start has a dedicated budget instead of the short read timeout',
    () async {
      final api = ToeicApi(
        baseUrl: 'http://toeic.test',
        installationId: () async => 'a' * 64,
        requestTimeout: const Duration(milliseconds: 10),
        startTimeout: const Duration(seconds: 2),
        client: MockClient((request) async {
          if (request.url.path.contains('/api/demo/')) return session();
          await Future<void>.delayed(const Duration(milliseconds: 60));
          return ok(startResult());
        }),
      );
      addTearDown(api.dispose);
      expect((await api.startExam('exam-1')).attemptId, 'committed-attempt');
    },
  );

  test(
    'parallel Start calls send exactly one POST for the same exam',
    () async {
      final seen = Completer<void>();
      final gate = Completer<http.Response>();
      var calls = 0;
      final api = ToeicApi(
        baseUrl: 'http://toeic.test',
        installationId: () async => 'a' * 64,
        client: MockClient((request) async {
          if (request.url.path.contains('/api/demo/')) return session();
          calls++;
          seen.complete();
          return gate.future;
        }),
      );
      addTearDown(api.dispose);
      final first = api.startExam('exam-1');
      final second = api.startExam('exam-1');
      await seen.future;
      expect(calls, 1);
      gate.complete(ok(startResult()));
      expect((await first).attemptId, (await second).attemptId);
    },
  );

  test(
    'timed-out committed Start recovers history without a second POST',
    () async {
      final gate = Completer<http.Response>();
      var starts = 0;
      var reads = 0;
      final api = ToeicApi(
        baseUrl: 'http://toeic.test',
        installationId: () async => 'a' * 64,
        startTimeout: const Duration(milliseconds: 20),
        client: MockClient((request) async {
          if (request.url.path.contains('/api/demo/')) return session();
          if (request.url.path.endsWith('/Start')) {
            starts++;
            return gate.future;
          }
          reads++;
          return ok({
            'items': [
              {
                ...startResult(),
                'examId': 'exam-1',
                'examTitle': 'Reading',
                'status': 0,
              },
            ],
          });
        }),
      );
      addTearDown(() {
        gate.complete(ok(startResult()));
        api.dispose();
      });
      final recovered = await api.startExam('exam-1');
      expect(recovered.resumedExistingAttempt, isTrue);
      expect(recovered.startedAtUtc, startedAt);
      expect(recovered.dueAtUtc, startedAt.add(const Duration(minutes: 75)));
      expect(starts, 1);
      expect(reads, 1);
    },
  );

  test(
    'unknown Start outcome with empty history stays explicit and is not retried',
    () async {
      var starts = 0;
      var reads = 0;
      final api = ToeicApi(
        baseUrl: 'http://toeic.test',
        installationId: () async => 'a' * 64,
        client: MockClient((request) async {
          if (request.url.path.contains('/api/demo/')) return session();
          if (request.url.path.endsWith('/Start')) {
            starts++;
            throw http.ClientException('Connection lost after server commit.');
          }
          reads++;
          return ok({'items': []});
        }),
      );
      addTearDown(api.dispose);
      await expectLater(
        api.startExam('exam-1'),
        throwsA(
          isA<ApiException>().having(
            (e) => e.requestOutcomeUnknown,
            'uncertain outcome',
            isTrue,
          ),
        ),
      );
      expect(starts, 1);
      expect(reads, 1);
    },
  );

  test('rejected Start does not recover history or retry the POST', () async {
    var starts = 0;
    var reads = 0;
    final api = ToeicApi(
      baseUrl: 'http://toeic.test',
      installationId: () async => 'a' * 64,
      client: MockClient((request) async {
        if (request.url.path.contains('/api/demo/')) return session();
        if (request.url.path.endsWith('/Start')) {
          starts++;
          return http.Response(
            jsonEncode({
              'success': false,
              'error': {'message': 'Draft'},
            }),
            403,
          );
        }
        reads++;
        return ok({'items': []});
      }),
    );
    addTearDown(api.dispose);
    await expectLater(
      api.startExam('draft-exam'),
      throwsA(isA<ApiException>().having((e) => e.statusCode, 'status', 403)),
    );
    expect(starts, 1);
    expect(reads, 0);
  });
  test(
    'installation identity is 256 bit random and persisted across launches',
    () async {
      SharedPreferences.setMockInitialValues({});
      final first = await DemoIdentityStore().getInstallationId();
      expect(first, matches(RegExp(r'^[a-f0-9]{64}$')));
      expect(await DemoIdentityStore().getInstallationId(), first);
    },
  );

  test('parallel APIs bootstrap once and preserve ABP result.items', () async {
    final server = ToeicTestServer();
    addTearDown(server.api.dispose);
    await Future.wait([
      server.api.getPublishedExams(),
      server.api.getMyAttempts(),
    ]);
    expect(server.bootstrapCalls, 1);
    final exam = (await server.api.getPublishedExams()).single;
    expect(exam.parts.length, 3);
    expect(exam.questionCount, 100);
    expect(exam.directions, isNotEmpty);
  });

  test(
    'expired save is not treated as success and returns finalized result',
    () async {
      final server = ToeicTestServer(expired: true);
      addTearDown(server.api.dispose);
      final result = await server.api.saveAnswer(
        attemptId: 'attempt-1',
        questionId: 'q101',
        selectedOptionId: 'q101-o0',
      );
      expect(result.accepted, isFalse);
      expect(result.isFinalized, isTrue);
      expect(result.result!.skippedCount, 100);
    },
  );

  test('renews session only once on authorization failure', () async {
    var bootstrapCalls = 0;
    var apiCalls = 0;
    final api = ToeicApi(
      baseUrl: 'http://toeic.test',
      installationId: () async => List.filled(64, 'a').join(),
      client: MockClient((request) async {
        if (request.url.path.contains('/api/demo/')) {
          bootstrapCalls++;
          return http.Response(
            jsonEncode({
              'result': {'accessToken': 'token', 'expireInSeconds': 3600},
              'success': true,
            }),
            200,
          );
        }
        apiCalls++;
        return http.Response(
          jsonEncode({
            'success': false,
            'error': {'message': 'Denied'},
          }),
          401,
        );
      }),
    );
    addTearDown(api.dispose);
    await expectLater(api.getPublishedExams(), throwsA(isA<ApiException>()));
    expect(bootstrapCalls, 2);
    expect(apiCalls, 2);
  });

  test('does not send bearer token to off-origin image URL', () async {
    final server = ToeicTestServer();
    addTearDown(server.api.dispose);
    await expectLater(
      server.api.loadImage('https://unrelated.test/image.png'),
      throwsA(isA<ApiException>()),
    );
  });
}
