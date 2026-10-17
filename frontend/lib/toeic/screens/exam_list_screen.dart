import 'package:flutter/material.dart';
import '../api/toeic_api.dart';
import '../models/exam_models.dart';
import '../theme/toeic_theme.dart';
import '../widgets/flow_widgets.dart';
import 'attempt_screen.dart';
import 'instructions_screen.dart';

class ExamListScreen extends StatefulWidget {
  const ExamListScreen({super.key, required this.api});
  final ToeicApi api;
  @override
  State<ExamListScreen> createState() => _ExamListScreenState();
}

class _ExamListScreenState extends State<ExamListScreen>
    with WidgetsBindingObserver {
  List<PublishedExam> _exams = [];
  List<AttemptSummary> _attempts = [];
  String? _error;
  bool _loading = true;
  bool _opening = false;
  int _tab = 0;
  String _search = '';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _reload();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed && !_loading && !_opening) _reload();
  }

  Future<void> _reload() async {
    if (mounted) {
      setState(() {
        _loading = true;
        _error = null;
      });
    }
    try {
      final data = await Future.wait<Object>([
        widget.api.getPublishedExams(),
        widget.api.getMyAttempts(),
      ]);
      if (!mounted) return;
      setState(() {
        _exams = data[0] as List<PublishedExam>;
        _attempts = data[1] as List<AttemptSummary>;
      });
    } on Object catch (error) {
      if (mounted) setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _openExam(PublishedExam exam) async {
    if (_opening) return;
    setState(() => _opening = true);
    try {
      await Navigator.of(context).push(
        MaterialPageRoute<void>(
          builder: (_) => InstructionsScreen(api: widget.api, exam: exam),
        ),
      );
      if (mounted) await _reload();
    } finally {
      if (mounted) setState(() => _opening = false);
    }
  }

  Future<void> _openAttempt(AttemptSummary attempt) async {
    if (_opening) return;
    setState(() => _opening = true);
    try {
      await Navigator.of(context).push(
        MaterialPageRoute<void>(
          builder: (_) => AttemptScreen(
            api: widget.api,
            attemptId: attempt.attemptId,
            resumed: true,
          ),
        ),
      );
      if (mounted) await _reload();
    } finally {
      if (mounted) setState(() => _opening = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const FlowBrand(),
      actions: [
        IconButton(
          onPressed: _loading ? null : _reload,
          tooltip: 'Tải lại dữ liệu',
          icon: const Icon(Icons.refresh_rounded),
        ),
      ],
    ),
    bottomNavigationBar: NavigationBar(
      selectedIndex: _tab,
      onDestinationSelected: (value) => setState(() => _tab = value),
      destinations: const [
        NavigationDestination(
          icon: Icon(Icons.library_books_outlined),
          selectedIcon: Icon(Icons.library_books_rounded),
          label: 'Đề thi',
        ),
        NavigationDestination(
          icon: Icon(Icons.pending_actions_outlined),
          selectedIcon: Icon(Icons.pending_actions_rounded),
          label: 'Đang làm',
        ),
        NavigationDestination(
          icon: Icon(Icons.insights_outlined),
          selectedIcon: Icon(Icons.insights_rounded),
          label: 'Kết quả',
        ),
      ],
    ),
    body: SafeArea(
      top: false,
      child: RefreshIndicator(
        onRefresh: _reload,
        child: LayoutBuilder(
          builder: (context, constraints) => SingleChildScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 1000),
                child: Padding(
                  padding: EdgeInsets.all(constraints.maxWidth > 650 ? 28 : 16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      FlowHero(
                        title: _tab == 0
                            ? 'Sẵn sàng chinh phục\nTOEIC Reading?'
                            : _tab == 1
                            ? 'Tiếp tục hành trình\ncủa bạn.'
                            : 'Mỗi bài thi,\nmột bước tiến mới.',
                        description: _tab == 0
                            ? 'Chọn đề thi và bắt đầu thử sức. Giữ tâm trí thư thái, tập trung vào từng câu hỏi.'
                            : _tab == 1
                            ? 'Khôi phục bài đang làm cùng các đáp án đã lưu. Thời gian tiếp tục theo hạn nộp của bài.'
                            : 'Xem lại kết quả đã được ghi nhận. Kiên trì luyện tập để tiến bộ mỗi ngày.',
                      ),
                      const SizedBox(height: 26),
                      Text(
                        _tab == 0
                            ? 'Đề thi TOEIC Reading'
                            : _tab == 1
                            ? 'Bài thi đang làm'
                            : 'Kết quả của bạn',
                        style: const TextStyle(
                          fontSize: 21,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(height: 14),
                      if (_loading)
                        const Padding(
                          padding: EdgeInsets.all(40),
                          child: Center(child: CircularProgressIndicator()),
                        )
                      else if (_error != null)
                        FlowMessage(
                          icon: Icons.cloud_off_rounded,
                          title: 'Chưa tải được dữ liệu',
                          detail: _error!,
                          onRetry: _reload,
                        )
                      else if (_tab == 0)
                        ..._examList()
                      else
                        ..._historyList(),
                      const SizedBox(height: 28),
                      const Center(
                        child: Icon(Icons.spa_outlined, color: FlowColors.sage),
                      ),
                      const SizedBox(height: 9),
                      const Center(
                        child: Text(
                          'HỌC TẬP BỀN BỈ MỖI NGÀY',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontSize: 11,
                            letterSpacing: 1.2,
                            fontWeight: FontWeight.w600,
                            color: FlowColors.secondary,
                          ),
                        ),
                      ),
                      const SizedBox(height: 16),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    ),
  );

  List<Widget> _examList() {
    if (_exams.isEmpty) {
      return [
        FlowMessage(
          icon: Icons.menu_book_rounded,
          title: 'Chưa có đề được xuất bản',
          detail:
              'Đề thi sẽ xuất hiện tại đây sau khi giảng viên hoàn tất kiểm tra và xuất bản.',
          onRetry: _reload,
        ),
      ];
    }
    final visible = _exams
        .where(
          (e) => '${e.title} ${e.code}'.toLowerCase().contains(
            _search.toLowerCase(),
          ),
        )
        .toList();
    return [
      TextField(
        onChanged: (value) => setState(() => _search = value),
        decoration: const InputDecoration(
          hintText: 'Tìm tên hoặc mã đề',
          prefixIcon: Icon(Icons.search_rounded),
        ),
      ),
      const SizedBox(height: 14),
      Text(
        '${visible.length} đề thi',
        style: const TextStyle(fontSize: 12, color: FlowColors.secondary),
      ),
      const SizedBox(height: 12),
      if (visible.isEmpty)
        const FlowMessage(
          icon: Icons.search_off_rounded,
          title: 'Không tìm thấy đề',
          detail: 'Thử tên hoặc mã đề khác.',
        ),
      for (final exam in visible)
        Padding(
          padding: const EdgeInsets.only(bottom: 14),
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      FlowPill(
                        label: exam.canStart
                            ? 'Sẵn sàng làm bài'
                            : 'Chưa khả dụng',
                      ),
                      const Spacer(),
                      const Icon(
                        Icons.menu_book_outlined,
                        color: FlowColors.sage,
                      ),
                    ],
                  ),
                  const SizedBox(height: 15),
                  Text(
                    exam.title,
                    style: const TextStyle(
                      fontSize: 19,
                      height: 1.4,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 5),
                  Text(
                    exam.code,
                    style: const TextStyle(
                      fontSize: 12,
                      color: FlowColors.secondary,
                    ),
                  ),
                  const SizedBox(height: 17),
                  Wrap(
                    spacing: 16,
                    runSpacing: 10,
                    children: [
                      _Meta(
                        icon: Icons.quiz_outlined,
                        text: '${exam.questionCount} câu hỏi',
                      ),
                      _Meta(
                        icon: Icons.schedule_rounded,
                        text: '${exam.durationMinutes} phút',
                      ),
                      const _Meta(
                        icon: Icons.layers_outlined,
                        text: 'Part 5, 6, 7',
                      ),
                    ],
                  ),
                  const SizedBox(height: 20),
                  Align(
                    alignment: Alignment.centerRight,
                    child: FilledButton.icon(
                      onPressed: exam.canStart && !_opening
                          ? () => _openExam(exam)
                          : null,
                      icon: const Icon(Icons.arrow_forward_rounded, size: 18),
                      label: const Text('Xem đề thi'),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
    ];
  }

  List<Widget> _historyList() {
    final visible = _attempts
        .where((a) => _tab == 1 ? a.inProgress : !a.inProgress)
        .toList();
    if (visible.isEmpty) {
      return [
        FlowMessage(
          icon: _tab == 1
              ? Icons.pending_actions_rounded
              : Icons.insights_rounded,
          title: _tab == 1 ? 'Chưa có bài đang làm' : 'Chưa có kết quả',
          detail: _tab == 1
              ? 'Bắt đầu một đề thi để luyện đọc hôm nay.'
              : 'Kết quả sẽ được lưu tại đây sau khi bạn nộp bài.',
        ),
      ];
    }
    return [
      for (final attempt in visible)
        Padding(
          padding: const EdgeInsets.only(bottom: 14),
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  FlowPill(
                    label: attempt.inProgress
                        ? 'Đang làm bài'
                        : attempt.status == 2
                        ? 'Đã hết thời gian'
                        : 'Đã nộp bài',
                  ),
                  const SizedBox(height: 14),
                  Text(
                    attempt.examTitle,
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Bắt đầu: ${_date(attempt.startedAtUtc)}',
                    style: const TextStyle(
                      fontSize: 12,
                      color: FlowColors.secondary,
                    ),
                  ),
                  if (attempt.result != null)
                    Padding(
                      padding: const EdgeInsets.only(top: 10),
                      child: Text(
                        '${attempt.result!.correctPercentage.toStringAsFixed(2)}% đúng • ${attempt.result!.correctCount}/${attempt.result!.totalQuestions} câu',
                        style: const TextStyle(
                          fontWeight: FontWeight.w700,
                          color: FlowColors.success,
                        ),
                      ),
                    ),
                  const SizedBox(height: 16),
                  FilledButton.icon(
                    onPressed: _opening ? null : () => _openAttempt(attempt),
                    icon: Icon(
                      attempt.inProgress
                          ? Icons.play_arrow_rounded
                          : Icons.insights_rounded,
                    ),
                    label: Text(
                      attempt.inProgress ? 'Tiếp tục làm bài' : 'Xem kết quả',
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
    ];
  }

  static String _date(DateTime date) {
    final local = date.toLocal();
    return '${local.day.toString().padLeft(2, '0')}/${local.month.toString().padLeft(2, '0')}/${local.year} ${local.hour.toString().padLeft(2, '0')}:${local.minute.toString().padLeft(2, '0')}';
  }
}

class _Meta extends StatelessWidget {
  const _Meta({required this.icon, required this.text});
  final IconData icon;
  final String text;
  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      Icon(icon, size: 16, color: FlowColors.secondary),
      const SizedBox(width: 5),
      Text(
        text,
        style: const TextStyle(fontSize: 12, color: FlowColors.secondary),
      ),
    ],
  );
}
