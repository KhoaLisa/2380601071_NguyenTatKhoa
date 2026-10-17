import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_application_1/main.dart';
import 'package:flutter_application_1/toeic/screens/attempt_screen.dart';
import 'package:flutter_application_1/toeic/theme/toeic_theme.dart';
import 'package:flutter_application_1/toeic/widgets/passage_view.dart';
import 'helpers/toeic_test_server.dart';

void main() {
  Future<void> mobile(WidgetTester tester) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(390, 844);
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
  }

  Future<void> openAttempt(WidgetTester tester, ToeicTestServer server) async {
    await mobile(tester);
    addTearDown(server.api.dispose);
    await tester.pumpWidget(
      MaterialApp(
        theme: toeicFlowTheme(),
        home: AttemptScreen(
          api: server.api,
          attemptId: 'attempt-1',
          resumed: false,
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  Future<void> refreshAttempt(WidgetTester tester) async {
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.inactive);
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.resumed);
    await tester.pump();
  }

  bool selected(WidgetTester tester, String optionKey) => tester
      .widget<Semantics>(
        find.byWidgetPredicate(
          (widget) =>
              widget is Semantics &&
              widget.properties.label ==
                  '$optionKey. Choice $optionKey for question 101',
        ),
      )
      .properties
      .checked!;

  Future<void> confirmSubmit(WidgetTester tester) async {
    await tester.tap(find.byKey(const Key('submitExamButton')));
    await tester.pump(const Duration(milliseconds: 300));
    await tester.tap(find.byKey(const Key('confirmSubmitButton')));
    await tester.pump();
  }

  testWidgets(
    'starts on real exam list with no login and handles empty database',
    (tester) async {
      await mobile(tester);
      final server = ToeicTestServer(empty: true);
      addTearDown(server.api.dispose);
      await tester.pumpWidget(ToeicReadingApp(api: server.api));
      await tester.pumpAndSettle();
      expect(find.text('TOEICFlow'), findsOneWidget);
      expect(find.text('Chưa có đề được xuất bản'), findsOneWidget);
      expect(find.byKey(const Key('usernameField')), findsNothing);
      expect(server.bootstrapCalls, 1);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'list to instructions to attempt saves changes clears and submits',
    (tester) async {
      await mobile(tester);
      final server = ToeicTestServer();
      addTearDown(server.api.dispose);
      await tester.pumpWidget(ToeicReadingApp(api: server.api));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Xem đề thi'));
      await tester.tap(find.text('Xem đề thi'));
      await tester.pumpAndSettle();
      expect(find.text('Thông tin bài thi'), findsOneWidget);
      await tester.tap(find.byKey(const Key('startExamButton')));
      await tester.pumpAndSettle();
      expect(find.text('Câu 101'), findsOneWidget);
      await tester.tap(find.byKey(const Key('optionq101-o0')));
      await tester.pumpAndSettle();
      expect(server.answers['q101'], 'q101-o0');
      await tester.tap(find.byKey(const Key('optionq101-o1')));
      await tester.pumpAndSettle();
      expect(server.answers['q101'], 'q101-o1');
      await tester.ensureVisible(find.byKey(const Key('clearAnswerButton')));
      await tester.tap(find.byKey(const Key('clearAnswerButton')));
      await tester.pumpAndSettle();
      expect(server.answers['q101'], isNull);
      await tester.tap(find.byKey(const Key('submitExamButton')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('confirmSubmitButton')));
      await tester.pumpAndSettle();
      expect(server.submitCalls, 1);
      expect(find.byKey(const Key('resultPercentage')), findsOneWidget);
      expect(find.text('0.00%'), findsOneWidget);
      expect(find.text('100 / 100 câu'), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox.shrink());
    },
  );

  testWidgets(
    'question grid reaches Part 6 blanks and long Part 7 documents on mobile',
    (tester) async {
      await mobile(tester);
      final server = ToeicTestServer();
      addTearDown(server.api.dispose);
      await tester.pumpWidget(
        MaterialApp(
          theme: toeicFlowTheme(),
          home: AttemptScreen(
            api: server.api,
            attemptId: 'attempt-1',
            resumed: true,
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('questionGridButton')));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('questionNav131')));
      await tester.tap(find.byKey(const Key('questionNav131')));
      await tester.pumpAndSettle();
      expect(find.textContaining('_____ (131) _____'), findsOneWidget);
      await tester.tap(find.byKey(const Key('questionGridButton')));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('questionNav147')));
      await tester.tap(find.byKey(const Key('questionNav147')));
      await tester.pumpAndSettle();
      expect(find.text('Part 7 Multi-document'), findsOneWidget);
      expect(find.byType(ReadingTable), findsOneWidget);
      await tester.ensureVisible(find.byType(ReadingTable));
      await tester.pumpAndSettle();
      expect(find.text('Cedar Hall'), findsOneWidget);
      expect(find.text('Vuốt ngang để xem đầy đủ bảng'), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox.shrink());
    },
  );

  testWidgets(
    'expired offline submission attempts once and leaves explicit retry',
    (tester) async {
      await mobile(tester);
      final server = ToeicTestServer(expired: true, failSubmit: true);
      addTearDown(server.api.dispose);
      await tester.pumpWidget(
        MaterialApp(
          theme: toeicFlowTheme(),
          home: AttemptScreen(
            api: server.api,
            attemptId: 'attempt-1',
            resumed: true,
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.pump(const Duration(seconds: 4));
      expect(server.submitCalls, 1);
      expect(find.textContaining('Chưa xác nhận được kết quả'), findsOneWidget);
      expect(find.text('Xem kết quả'), findsOneWidget);
      await tester.tap(find.byKey(const Key('optionq101-o0')));
      expect(server.saves, isEmpty);
      server.failSubmit = false;
      await tester.tap(find.byKey(const Key('submitExamButton')));
      await tester.pumpAndSettle();
      expect(server.submitCalls, 2);
      expect(find.byKey(const Key('resultPercentage')), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox.shrink());
    },
  );

  testWidgets('submit waits for in-flight answer save before grade request', (
    tester,
  ) async {
    await mobile(tester);
    final server = ToeicTestServer()..saveGate = Completer<void>();
    addTearDown(server.api.dispose);
    await tester.pumpWidget(
      MaterialApp(
        theme: toeicFlowTheme(),
        home: AttemptScreen(
          api: server.api,
          attemptId: 'attempt-1',
          resumed: false,
        ),
      ),
    );
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('optionq101-o0')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('submitExamButton')));
    // The save spinner intentionally stays active while the gate is closed.
    // Do not advance the fake clock to the API timeout with pumpAndSettle.
    await tester.pump(const Duration(milliseconds: 300));
    await tester.tap(find.byKey(const Key('confirmSubmitButton')));
    await tester.pump();
    expect(server.submitCalls, 0);
    server.saveGate!.complete();
    await tester.pumpAndSettle();
    expect(server.submitCalls, 1);
    expect(find.text('1.00%'), findsOneWidget);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets('stale refresh preserves an optimistic answer still saving', (
    tester,
  ) async {
    final server = ToeicTestServer();
    await openAttempt(tester, server);
    server.attemptGate = Completer<void>();
    await refreshAttempt(tester);
    expect(server.attemptCalls, 2);
    server.saveGate = Completer<void>();
    await tester.tap(find.byKey(const Key('optionq101-o0')));
    await tester.pump();
    expect(selected(tester, 'A'), isTrue);
    server.attemptGate!.complete();
    await tester.pump();
    await tester.pump();
    expect(selected(tester, 'A'), isTrue);
    expect(find.text('Đang lưu đáp án…'), findsOneWidget);
    server.saveGate!.complete();
    await tester.pumpAndSettle();
    expect(server.answers['q101'], 'q101-o0');
    expect(selected(tester, 'A'), isTrue);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets('stale refresh cannot overwrite newer completed answer saves', (
    tester,
  ) async {
    final server = ToeicTestServer();
    await openAttempt(tester, server);
    server.attemptGate = Completer<void>();
    await refreshAttempt(tester);
    await tester.tap(find.byKey(const Key('optionq101-o0')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('optionq101-o1')));
    await tester.pumpAndSettle();
    expect(server.answers['q101'], 'q101-o1');
    server.attemptGate!.complete();
    await tester.pumpAndSettle();
    expect(selected(tester, 'A'), isFalse);
    expect(selected(tester, 'B'), isTrue);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  for (final rejected in [false, true]) {
    testWidgets(
      '${rejected ? 'rejected' : 'failed'} save rolls back the current refreshed question',
      (tester) async {
        final server = ToeicTestServer()..answers['q101'] = 'q101-o0';
        await openAttempt(tester, server);
        server.attemptGate = Completer<void>();
        await refreshAttempt(tester);
        server.saveGate = Completer<void>();
        await tester.tap(find.byKey(const Key('optionq101-o1')));
        await tester.pump();
        server.attemptGate!.complete();
        await tester.pump();
        await tester.pump();
        expect(selected(tester, 'B'), isTrue);
        server.failSave = !rejected;
        server.rejectSave = rejected;
        server.saveGate!.complete();
        await tester.pumpAndSettle();
        expect(server.answers['q101'], 'q101-o0');
        expect(selected(tester, 'A'), isTrue);
        expect(selected(tester, 'B'), isFalse);
        expect(
          find.textContaining(
            rejected
                ? 'Máy chủ chưa ghi nhận lựa chọn'
                : 'Chưa lưu được đáp án',
          ),
          findsOneWidget,
        );
        server.failSave = false;
        server.rejectSave = false;
        await tester.tap(find.byKey(const Key('optionq101-o1')));
        await tester.pumpAndSettle();
        expect(selected(tester, 'B'), isTrue);
        expect(server.answers['q101'], 'q101-o1');
        expect(tester.takeException(), isNull);
        await tester.pumpWidget(const SizedBox.shrink());
      },
    );
  }

  testWidgets('manual submit refuses a save that failed while waiting', (
    tester,
  ) async {
    final server = ToeicTestServer()
      ..saveGate = Completer<void>()
      ..failSave = true;
    await openAttempt(tester, server);
    await tester.tap(find.byKey(const Key('optionq101-o0')));
    await tester.pump();
    await confirmSubmit(tester);
    expect(server.submitCalls, 0);
    server.saveGate!.complete();
    await tester.pumpAndSettle();
    expect(server.submitCalls, 0);
    expect(find.byKey(const Key('resultPercentage')), findsNothing);
    expect(find.textContaining('Chưa nộp bài vì'), findsOneWidget);
    await tester.tap(find.byKey(const Key('submitExamButton')));
    await tester.pumpAndSettle();
    expect(
      find.textContaining('Có đáp án chưa lưu thành công'),
      findsOneWidget,
    );
    await tester.tap(find.text('Tiếp tục làm'));
    await tester.pumpAndSettle();
    server.failSave = false;
    await tester.tap(find.byKey(const Key('optionq101-o0')));
    await tester.pumpAndSettle();
    await confirmSubmit(tester);
    await tester.pumpAndSettle();
    expect(server.submitCalls, 1);
    expect(find.text('1.00%'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets(
    'expiry still finalizes saved answers after a pending save fails',
    (tester) async {
      final server = ToeicTestServer();
      await openAttempt(tester, server);
      server.expired = true;
      server.attemptGate = Completer<void>();
      await refreshAttempt(tester);
      server.saveGate = Completer<void>();
      server.failSave = true;
      await tester.tap(find.byKey(const Key('optionq101-o0')));
      await tester.pump();
      server.attemptGate!.complete();
      await tester.pump();
      await tester.pump();
      expect(server.submitCalls, 0);
      server.saveGate!.complete();
      await tester.pumpAndSettle();
      expect(server.submitCalls, 1);
      expect(server.answers['q101'], isNull);
      expect(find.text('0.00%'), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox.shrink());
    },
  );

  testWidgets('late in-progress refresh cannot replace the submitted result', (
    tester,
  ) async {
    final server = ToeicTestServer();
    await openAttempt(tester, server);
    server.attemptGate = Completer<void>();
    await refreshAttempt(tester);
    await tester.tap(find.byKey(const Key('optionq101-o0')));
    await tester.pumpAndSettle();
    await confirmSubmit(tester);
    await tester.pumpAndSettle();
    expect(find.text('1.00%'), findsOneWidget);
    server.attemptGate!.complete();
    await tester.pumpAndSettle();
    expect(find.text('1.00%'), findsOneWidget);
    expect(find.byKey(const Key('optionq101-o0')), findsNothing);
    expect(server.submitCalls, 1);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets('server finalization closes an open manual confirmation', (
    tester,
  ) async {
    final server = ToeicTestServer();
    await openAttempt(tester, server);
    server.submitted = true;
    server.attemptGate = Completer<void>();
    await refreshAttempt(tester);
    await tester.tap(find.byKey(const Key('submitExamButton')));
    await tester.pump(const Duration(milliseconds: 300));
    expect(find.byKey(const Key('confirmSubmitButton')), findsOneWidget);
    server.attemptGate!.complete();
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('confirmSubmitButton')), findsNothing);
    expect(find.byKey(const Key('resultPercentage')), findsOneWidget);
    expect(server.submitCalls, 0);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets('finalized save refreshes result after an older GET completes', (
    tester,
  ) async {
    final server = ToeicTestServer();
    await openAttempt(tester, server);
    server.attemptGate = Completer<void>();
    await refreshAttempt(tester);
    server.saveGate = Completer<void>();
    await tester.tap(find.byKey(const Key('optionq101-o0')));
    await tester.pump();
    server.expired = true;
    server.omitFinalizedSaveResult = true;
    server.saveGate!.complete();
    await tester.pump();
    await tester.pump();
    server.attemptGate!.complete();
    await tester.pumpAndSettle();
    expect(server.attemptCalls, 3);
    expect(server.submitCalls, 0);
    expect(find.byKey(const Key('resultPercentage')), findsOneWidget);
    expect(find.text('0.00%'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox.shrink());
  });
}
