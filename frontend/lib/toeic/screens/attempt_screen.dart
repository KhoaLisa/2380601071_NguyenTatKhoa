import 'dart:async';
import 'package:flutter/material.dart';
import '../api/toeic_api.dart';
import '../models/exam_models.dart';
import '../theme/toeic_theme.dart';
import '../widgets/flow_widgets.dart';
import '../widgets/passage_view.dart';
import 'result_screen.dart';

class AttemptScreen extends StatefulWidget {
  const AttemptScreen({
    super.key,
    required this.api,
    required this.attemptId,
    required this.resumed,
  });
  final ToeicApi api;
  final String attemptId;
  final bool resumed;
  @override
  State<AttemptScreen> createState() => _AttemptScreenState();
}

class _AttemptScreenState extends State<AttemptScreen>
    with WidgetsBindingObserver {
  ExamAttempt? _attempt;
  AttemptResult? _result;
  String? _error;
  String? _saveError;
  Timer? _timer;
  final Stopwatch _clock = Stopwatch();
  Duration _remainingAtSync = Duration.zero;
  Duration _remaining = Duration.zero;
  int _questionIndex = 0;
  int _ticks = 0;
  bool _syncing = false;
  bool _submitting = false;
  bool _confirming = false;
  bool _autoSubmitAttempted = false;
  bool _allowLeave = false;
  bool _serverFinalized = false;
  bool _needsResultRefresh = false;
  int _saveFailureVersion = 0;
  final Map<String, Future<void>> _pendingSaves = {};
  final Map<String, int> _answerRevisions = {};
  final Set<String> _failedSaveQuestionIds = {};
  final ScrollController _mobileScroll = ScrollController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _load();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _timer?.cancel();
    _clock.stop();
    _mobileScroll.dispose();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed && _result == null) {
      _load(silent: true);
    }
  }

  Future<void> _load({bool silent = false}) async {
    if (_syncing ||
        _submitting ||
        _result != null ||
        _pendingSaves.isNotEmpty) {
      return;
    }
    _syncing = true;
    final revisionsAtRequest = Map<String, int>.of(_answerRevisions);
    if (!silent && mounted) setState(() => _error = null);
    try {
      final attempt = await widget.api.getAttempt(widget.attemptId);
      if (!mounted || _result != null) return;
      if (_serverFinalized && attempt.inProgress) return;
      final currentId = _attempt?.questions.elementAtOrNull(_questionIndex)?.id;
      _serverFinalized =
          _serverFinalized || !attempt.inProgress || attempt.result != null;
      if (attempt.result != null) {
        setState(() {
          _attempt = attempt;
          _error = null;
        });
        _showResult(attempt.result!);
        return;
      }
      if (!attempt.inProgress) {
        _timer?.cancel();
        _clock.stop();
        throw const ApiException(
          'Lượt thi đã kết thúc nhưng máy chủ chưa trả kết quả. Hãy tải lại.',
        );
      }
      if (attempt.questions.isEmpty) {
        throw const ApiException('Bài thi không có câu hỏi khả dụng.');
      }
      // A background GET may have read SQL before a later answer save. Keep
      // local mutations made since that request, even if the save has already
      // completed by the time the stale snapshot arrives.
      final currentQuestions = {
        for (final question in _attempt?.questions ?? <ExamQuestion>[])
          question.id: question,
      };
      for (final question in attempt.questions) {
        final current = currentQuestions[question.id];
        if (current != null &&
            ((_answerRevisions[question.id] ?? 0) !=
                    (revisionsAtRequest[question.id] ?? 0) ||
                _pendingSaves.containsKey(question.id))) {
          question.selectedOptionId = current.selectedOptionId;
        }
      }
      setState(() {
        _attempt = attempt;
        final newIndex = attempt.questions.indexWhere((q) => q.id == currentId);
        _questionIndex = newIndex < 0 ? 0 : newIndex;
        _remainingAtSync = attempt.dueAtUtc.difference(attempt.serverNowUtc);
        _clock
          ..reset()
          ..start();
        _updateRemaining();
        _error = null;
        if (silent && _pendingSaves.isEmpty && _failedSaveQuestionIds.isEmpty) {
          _saveError = null;
        }
      });
      _timer ??= Timer.periodic(const Duration(seconds: 1), (_) {
        if (!mounted || _result != null) return;
        setState(_updateRemaining);
        _ticks++;
        if (_remaining <= Duration.zero && !_autoSubmitAttempted) {
          _autoSubmitAttempted = true;
          _submit(automatic: true);
        } else if (_ticks % 30 == 0 && _remaining > Duration.zero) {
          _load(silent: true);
        }
      });
      if (_remaining <= Duration.zero && !_autoSubmitAttempted) {
        _autoSubmitAttempted = true;
        unawaited(_submit(automatic: true));
      }
    } on Object catch (error) {
      if (mounted && _result == null) {
        setState(() {
          if (silent && _attempt != null && !_serverFinalized) {
            _saveError =
                'Chưa đồng bộ được máy chủ. Các đáp án chỉ được ghi nhận sau khi lưu thành công.';
          } else {
            _error = error.toString();
          }
        });
      }
    } finally {
      _syncing = false;
      if (mounted &&
          _needsResultRefresh &&
          _pendingSaves.isEmpty &&
          !_submitting &&
          _result == null) {
        _needsResultRefresh = false;
        unawaited(_load(silent: true));
      }
    }
  }

  void _updateRemaining() {
    final next = _remainingAtSync - _clock.elapsed;
    _remaining = next.isNegative ? Duration.zero : next;
  }

  Future<void> _choose(ExamQuestion question, String? optionId) async {
    if (_submitting ||
        _serverFinalized ||
        _result != null ||
        _remaining <= Duration.zero ||
        _pendingSaves.containsKey(question.id)) {
      return;
    }
    final current = _currentQuestion(question.id);
    if (current == null) return;
    final previous = current.selectedOptionId;
    final revision = (_answerRevisions[question.id] ?? 0) + 1;
    setState(() {
      _answerRevisions[question.id] = revision;
      current.selectedOptionId = optionId;
      _saveError = null;
    });
    final saving = _save(question.id, optionId, previous, revision);
    setState(() {
      _pendingSaves[question.id] = saving;
    });
    await saving;
    if (mounted) {
      setState(() {
        _pendingSaves.remove(question.id);
      });
      if (_needsResultRefresh &&
          _pendingSaves.isEmpty &&
          !_syncing &&
          !_submitting &&
          _result == null) {
        _needsResultRefresh = false;
        unawaited(_load(silent: true));
      }
    }
  }

  ExamQuestion? _currentQuestion(String questionId) =>
      _attempt?.questions.where((q) => q.id == questionId).firstOrNull;

  void _saveFailed(
    String questionId,
    String? previous,
    int revision,
    String message,
  ) {
    if (!mounted || _result != null || _serverFinalized) return;
    if (_answerRevisions[questionId] != revision) return;
    setState(() {
      _currentQuestion(questionId)?.selectedOptionId = previous;
      // A rollback is also a local mutation: an older GET must not undo it.
      _answerRevisions[questionId] = revision + 1;
      _failedSaveQuestionIds.add(questionId);
      _saveFailureVersion++;
      _saveError = message;
    });
  }

  Future<void> _save(
    String questionId,
    String? optionId,
    String? previous,
    int revision,
  ) async {
    try {
      final saved = await widget.api.saveAnswer(
        attemptId: widget.attemptId,
        questionId: questionId,
        selectedOptionId: optionId,
      );
      if (!mounted || _result != null) return;
      if (saved.isFinalized) {
        _serverFinalized = true;
        if (saved.result != null) {
          _showResult(saved.result!);
        } else {
          _needsResultRefresh = true;
        }
      } else if (!saved.accepted) {
        _saveFailed(
          questionId,
          previous,
          revision,
          'Máy chủ chưa ghi nhận lựa chọn. Hãy tải lại bài để kiểm tra.',
        );
      } else if (_answerRevisions[questionId] == revision) {
        setState(() {
          _currentQuestion(questionId)?.selectedOptionId = optionId;
          _failedSaveQuestionIds.remove(questionId);
        });
      }
    } on Object catch (error) {
      _saveFailed(
        questionId,
        previous,
        revision,
        'Chưa lưu được đáp án: $error',
      );
    }
  }

  void _showResult(AttemptResult result) {
    if (!mounted || _result != null) return;
    _timer?.cancel();
    _clock.stop();
    if (_confirming) Navigator.of(context).pop(false);
    setState(() {
      _result = result;
      _serverFinalized = true;
      _submitting = false;
    });
  }

  Future<void> _submit({bool automatic = false}) async {
    if (_submitting ||
        _result != null ||
        _attempt == null ||
        (!automatic && _confirming)) {
      return;
    }
    final failureVersionAtConfirmation = _saveFailureVersion;
    if (!automatic) {
      _confirming = true;
      final attempt = _attempt!;
      final answered = attempt.questions
          .where((q) => q.selectedOptionId != null)
          .length;
      final confirmed = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Hoàn tất bài thi?'),
          content: Text(
            'Tổng số: ${attempt.questions.length} câu\nĐã trả lời: $answered câu\nChưa trả lời: ${attempt.questions.length - answered} câu\n\nBạn có thể tiếp tục kiểm tra bài trước khi nộp.'
            '${_failedSaveQuestionIds.isEmpty ? '' : '\n\nCó đáp án chưa lưu thành công. Nếu tiếp tục, chỉ những đáp án máy chủ đã lưu được chấm.'}',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Tiếp tục làm'),
            ),
            FilledButton(
              key: const Key('confirmSubmitButton'),
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Xác nhận nộp'),
            ),
          ],
        ),
      );
      _confirming = false;
      if (!mounted || confirmed != true || _result != null || _submitting) {
        return;
      }
    } else if (_confirming) {
      Navigator.of(context).pop(false);
      _confirming = false;
    }
    if (!mounted || _result != null || _submitting) return;
    setState(() => _submitting = true);
    // Manual and automatic submission wait for requests already sent. A late
    // answer is still rejected by the server deadline, never by a local timer.
    await Future.wait([..._pendingSaves.values]);
    if (!mounted || _result != null) return;
    if (!automatic &&
        _remaining > Duration.zero &&
        _saveFailureVersion != failureVersionAtConfirmation) {
      setState(() {
        _submitting = false;
        _saveError =
            'Chưa nộp bài vì có đáp án vừa lưu thất bại. Hãy kiểm tra và chọn lại, hoặc xác nhận nộp lại với các đáp án đã được máy chủ lưu.';
      });
      return;
    }
    try {
      final result = await widget.api.submit(widget.attemptId);
      _showResult(result);
    } on Object catch (error) {
      if (mounted) {
        setState(
          () => _saveError = 'Chưa xác nhận được kết quả nộp bài: $error',
        );
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  Future<void> _leave() async {
    if (_submitting || _confirming) return;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Tạm rời bài thi?'),
        content: Text(
          _pendingSaves.isNotEmpty
              ? 'Đáp án đang được lưu. Hãy chờ lưu xong trước khi rời bài.'
              : 'Các đáp án đã lưu sẽ được khôi phục khi quay lại. Đồng hồ vẫn tiếp tục chạy.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Ở lại'),
          ),
          if (_pendingSaves.isEmpty)
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Về danh sách'),
            ),
        ],
      ),
    );
    if (mounted && confirmed == true) {
      setState(() => _allowLeave = true);
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) Navigator.of(context).pop();
      });
    }
  }

  void _goTo(int index) {
    setState(() => _questionIndex = index);
    if (_mobileScroll.hasClients) _mobileScroll.jumpTo(0);
  }

  Future<void> _navigator() async {
    final attempt = _attempt!;
    final index = await showModalBottomSheet<int>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (context) => SafeArea(
        child: SizedBox(
          height: MediaQuery.sizeOf(context).height * .72,
          child: SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(20, 4, 20, 24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Bản đồ câu hỏi',
                  style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Xanh: đã trả lời • Viền đậm: câu hiện tại',
                  style: TextStyle(fontSize: 12, color: FlowColors.secondary),
                ),
                for (final part in [5, 6, 7]) ...[
                  const SizedBox(height: 22),
                  Text(
                    'PART $part',
                    style: const TextStyle(
                      fontWeight: FontWeight.w700,
                      color: FlowColors.forest,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      for (var i = 0; i < attempt.questions.length; i++)
                        if (attempt.questions[i].part == part)
                          Semantics(
                            label:
                                'Câu ${attempt.questions[i].originalNumber}, ${attempt.questions[i].selectedOptionId == null ? 'chưa trả lời' : 'đã trả lời'}',
                            selected: i == _questionIndex,
                            child: SizedBox(
                              width: 48,
                              height: 44,
                              child: OutlinedButton(
                                key: Key(
                                  'questionNav${attempt.questions[i].originalNumber}',
                                ),
                                style: OutlinedButton.styleFrom(
                                  padding: EdgeInsets.zero,
                                  minimumSize: Size.zero,
                                  backgroundColor:
                                      attempt.questions[i].selectedOptionId ==
                                          null
                                      ? FlowColors.cream
                                      : FlowColors.soft,
                                  side: BorderSide(
                                    color: i == _questionIndex
                                        ? FlowColors.forest
                                        : FlowColors.border,
                                    width: i == _questionIndex ? 2 : 1,
                                  ),
                                ),
                                onPressed: () => Navigator.pop(context, i),
                                child: Text(
                                  '${attempt.questions[i].originalNumber}',
                                  style: const TextStyle(fontSize: 12),
                                ),
                              ),
                            ),
                          ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
    if (mounted && index != null) _goTo(index);
  }

  @override
  Widget build(BuildContext context) {
    if (_result != null) {
      return ResultScreen(
        result: _result!,
        examTitle: _attempt?.examTitle,
        startedAtUtc: _attempt?.startedAtUtc,
      );
    }
    if (_error != null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Bài thi')),
        body: Center(
          child: SingleChildScrollView(
            child: FlowMessage(
              icon: Icons.cloud_off_rounded,
              title: 'Chưa mở được bài thi',
              detail: _error!,
              onRetry: _load,
            ),
          ),
        ),
      );
    }
    final attempt = _attempt;
    if (attempt == null || attempt.questions.isEmpty) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    final question = attempt.questions[_questionIndex];
    final group = attempt.groups
        .where((g) => g.id == question.passageGroupId)
        .firstOrNull;
    final answered = attempt.questions
        .where((q) => q.selectedOptionId != null)
        .length;
    final warning = _remaining <= const Duration(minutes: 5);
    final expired = _remaining <= Duration.zero;
    return PopScope(
      canPop: _allowLeave,
      onPopInvokedWithResult: (didPop, result) {
        if (!didPop) _leave();
      },
      child: Scaffold(
        appBar: AppBar(
          title: Text(
            attempt.examTitle,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700),
          ),
          actions: [
            Container(
              margin: const EdgeInsets.only(right: 12),
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
              decoration: BoxDecoration(
                color: warning
                    ? FlowColors.error.withValues(alpha: .10)
                    : FlowColors.soft,
                borderRadius: BorderRadius.circular(30),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    Icons.timer_outlined,
                    size: 17,
                    color: warning ? FlowColors.error : FlowColors.forest,
                  ),
                  const SizedBox(width: 5),
                  Text(
                    _format(_remaining),
                    key: const Key('examTimer'),
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w800,
                      color: warning ? FlowColors.error : FlowColors.forest,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        body: SafeArea(
          top: false,
          bottom: false,
          child: Column(
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 5, 10, 12),
                child: Row(
                  children: [
                    FlowPill(label: 'PART ${question.part}'),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        '$answered/${attempt.questions.length} đã trả lời',
                        style: const TextStyle(
                          fontSize: 12,
                          color: FlowColors.secondary,
                        ),
                      ),
                    ),
                    IconButton(
                      key: const Key('questionGridButton'),
                      onPressed: _navigator,
                      tooltip: 'Bản đồ câu hỏi',
                      icon: const Icon(
                        Icons.grid_view_rounded,
                        color: FlowColors.forest,
                      ),
                    ),
                  ],
                ),
              ),
              LinearProgressIndicator(
                value: answered / attempt.questions.length,
                minHeight: 3,
                backgroundColor: FlowColors.soft,
              ),
              if (widget.resumed)
                const _Notice(
                  message: 'Đã khôi phục bài thi và các đáp án đã lưu.',
                  icon: Icons.cloud_done_outlined,
                ),
              if (warning)
                _Notice(
                  message: expired
                      ? 'Đã hết giờ. Lựa chọn đã khóa; hệ thống đang ghi nhận kết quả.'
                      : 'Còn không quá 5 phút. Hãy kiểm tra các câu chưa trả lời.',
                  icon: Icons.timer_outlined,
                  error: true,
                ),
              if (_saveError != null)
                _Notice(
                  message: _saveError!,
                  icon: Icons.cloud_off_rounded,
                  error: true,
                ),
              if (_pendingSaves.isNotEmpty)
                const _Notice(
                  message: 'Đang lưu đáp án…',
                  icon: Icons.sync_rounded,
                ),
              Expanded(
                child: LayoutBuilder(
                  builder: (context, constraints) {
                    final passage = group == null
                        ? null
                        : PassageView(
                            group: group,
                            api: widget.api,
                            questionNumbers: attempt.questions
                                .where((q) => q.passageGroupId == group.id)
                                .map((q) => q.originalNumber)
                                .toList(),
                          );
                    final pane = _QuestionPane(
                      question: question,
                      saving: _pendingSaves.containsKey(question.id),
                      enabled: !_submitting && !expired && !_serverFinalized,
                      onChoose: (value) => _choose(question, value),
                    );
                    if (constraints.maxWidth >= 900 && passage != null) {
                      return Center(
                        child: ConstrainedBox(
                          constraints: const BoxConstraints(maxWidth: 1400),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(
                                flex: 6,
                                child: SingleChildScrollView(
                                  padding: const EdgeInsets.all(20),
                                  child: passage,
                                ),
                              ),
                              Expanded(
                                flex: 5,
                                child: SingleChildScrollView(
                                  key: ValueKey(question.id),
                                  padding: const EdgeInsets.fromLTRB(
                                    0,
                                    20,
                                    20,
                                    20,
                                  ),
                                  child: pane,
                                ),
                              ),
                            ],
                          ),
                        ),
                      );
                    }
                    return ListView(
                      controller: _mobileScroll,
                      padding: const EdgeInsets.all(16),
                      children: [
                        if (passage != null) ...[
                          passage,
                          const SizedBox(height: 16),
                        ],
                        pane,
                      ],
                    );
                  },
                ),
              ),
            ],
          ),
        ),
        bottomNavigationBar: SafeArea(
          child: Container(
            decoration: const BoxDecoration(
              color: FlowColors.cream,
              border: Border(top: BorderSide(color: FlowColors.border)),
            ),
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            child: Row(
              children: [
                IconButton.outlined(
                  onPressed: _questionIndex == 0
                      ? null
                      : () => _goTo(_questionIndex - 1),
                  icon: const Icon(Icons.chevron_left_rounded),
                  tooltip: 'Câu trước',
                ),
                const SizedBox(width: 8),
                Text(
                  '${_questionIndex + 1}/${attempt.questions.length}',
                  style: const TextStyle(
                    fontSize: 12,
                    color: FlowColors.secondary,
                  ),
                ),
                const SizedBox(width: 12),
                if (_questionIndex < attempt.questions.length - 1) ...[
                  IconButton.outlined(
                    onPressed: () => _goTo(_questionIndex + 1),
                    icon: const Icon(Icons.chevron_right_rounded),
                    tooltip: 'Câu sau',
                  ),
                  const SizedBox(width: 8),
                ],
                Expanded(
                  child: FilledButton.icon(
                    key: const Key('submitExamButton'),
                    onPressed: _submitting
                        ? null
                        : () => _submit(automatic: expired),
                    icon: _submitting
                        ? const SizedBox.square(
                            dimension: 16,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : const Icon(Icons.check_rounded, size: 18),
                    label: Text(expired ? 'Xem kết quả' : 'Nộp bài'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  static String _format(Duration value) =>
      '${value.inMinutes.toString().padLeft(2, '0')}:${value.inSeconds.remainder(60).toString().padLeft(2, '0')}';
}

class _Notice extends StatelessWidget {
  const _Notice({
    required this.message,
    required this.icon,
    this.error = false,
  });
  final String message;
  final IconData icon;
  final bool error;
  @override
  Widget build(BuildContext context) => Container(
    width: double.infinity,
    color: error
        ? FlowColors.error.withValues(alpha: .08)
        : FlowColors.soft.withValues(alpha: .6),
    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(
          icon,
          size: 16,
          color: error ? FlowColors.error : FlowColors.forest,
        ),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            message,
            style: TextStyle(
              fontSize: 11,
              height: 1.5,
              color: error ? FlowColors.error : FlowColors.forest,
            ),
          ),
        ),
      ],
    ),
  );
}

class _QuestionPane extends StatelessWidget {
  const _QuestionPane({
    required this.question,
    required this.saving,
    required this.enabled,
    required this.onChoose,
  });
  final ExamQuestion question;
  final bool saving;
  final bool enabled;
  final ValueChanged<String?> onChoose;

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text(
                'Câu ${question.originalNumber}',
                style: const TextStyle(
                  fontSize: 21,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const Spacer(),
              if (saving)
                const SizedBox.square(
                  dimension: 17,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              else if (question.selectedOptionId != null)
                const Icon(
                  Icons.cloud_done_outlined,
                  size: 20,
                  color: FlowColors.success,
                ),
            ],
          ),
          const SizedBox(height: 17),
          SelectableText(
            question.prompt.isEmpty
                ? 'Chọn đáp án phù hợp cho chỗ trống ${question.originalNumber}.'
                : question.prompt,
            style: const TextStyle(fontSize: 17, height: 1.65),
          ),
          const SizedBox(height: 24),
          for (final option in question.options)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: Semantics(
                checked: question.selectedOptionId == option.id,
                enabled: enabled && !saving,
                label: '${option.key}. ${option.text}',
                child: Material(
                  color: question.selectedOptionId == option.id
                      ? FlowColors.soft
                      : FlowColors.cream,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(13),
                    side: BorderSide(
                      color: question.selectedOptionId == option.id
                          ? FlowColors.forest
                          : FlowColors.border,
                      width: question.selectedOptionId == option.id ? 1.7 : 1,
                    ),
                  ),
                  child: InkWell(
                    key: Key('option${option.id}'),
                    borderRadius: BorderRadius.circular(13),
                    onTap: enabled && !saving
                        ? () => onChoose(option.id)
                        : null,
                    child: Padding(
                      padding: const EdgeInsets.all(15),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            width: 27,
                            height: 27,
                            alignment: Alignment.center,
                            decoration: BoxDecoration(
                              shape: BoxShape.circle,
                              color: question.selectedOptionId == option.id
                                  ? FlowColors.forest
                                  : FlowColors.ivory,
                            ),
                            child: Text(
                              option.key,
                              style: TextStyle(
                                fontWeight: FontWeight.w700,
                                color: question.selectedOptionId == option.id
                                    ? FlowColors.cream
                                    : FlowColors.secondary,
                              ),
                            ),
                          ),
                          const SizedBox(width: 13),
                          Expanded(
                            child: Text(
                              option.text,
                              style: const TextStyle(fontSize: 15, height: 1.6),
                            ),
                          ),
                          if (question.selectedOptionId == option.id)
                            const Padding(
                              padding: EdgeInsets.only(left: 5, top: 3),
                              child: Icon(
                                Icons.check_rounded,
                                size: 18,
                                color: FlowColors.forest,
                              ),
                            ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            ),
          Align(
            alignment: Alignment.centerRight,
            child: TextButton.icon(
              key: const Key('clearAnswerButton'),
              onPressed: enabled && !saving && question.selectedOptionId != null
                  ? () => onChoose(null)
                  : null,
              icon: const Icon(Icons.remove_circle_outline, size: 18),
              label: const Text('Hủy lựa chọn'),
            ),
          ),
        ],
      ),
    ),
  );
}
