import 'package:flutter/material.dart';
import '../api/toeic_api.dart';
import '../models/exam_models.dart';
import '../theme/toeic_theme.dart';
import '../widgets/flow_widgets.dart';
import 'attempt_screen.dart';

class InstructionsScreen extends StatefulWidget {
  const InstructionsScreen({super.key, required this.api, required this.exam});
  final ToeicApi api;
  final PublishedExam exam;
  @override
  State<InstructionsScreen> createState() => _InstructionsScreenState();
}

class _InstructionsScreenState extends State<InstructionsScreen> {
  bool _starting = false;
  String? _error;
  Future<void> _start() async {
    if (_starting) return;
    setState(() {
      _starting = true;
      _error = null;
    });
    try {
      final result = await widget.api.startExam(widget.exam.id);
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute<void>(
          builder: (_) => AttemptScreen(
            api: widget.api,
            attemptId: result.attemptId,
            resumed: result.resumedExistingAttempt,
          ),
        ),
      );
    } on Object catch (error) {
      if (mounted) {
        setState(() {
          _starting = false;
          _error = error.toString();
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final exam = widget.exam;
    return Scaffold(
      appBar: AppBar(
        title: const Text(
          'Thông tin bài thi',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
        ),
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 10, 16, 14),
          child: FilledButton.icon(
            key: const Key('startExamButton'),
            onPressed: !_starting && exam.canStart ? _start : null,
            icon: _starting
                ? const SizedBox.square(
                    dimension: 18,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Colors.white,
                    ),
                  )
                : const Icon(Icons.arrow_forward_rounded),
            label: Text(
              _starting
                  ? 'Đang chuẩn bị bài thi…'
                  : 'Bắt đầu / tiếp tục bài thi',
            ),
          ),
        ),
      ),
      body: SafeArea(
        top: false,
        child: SingleChildScrollView(
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 760),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    FlowHero(
                      title: exam.title,
                      description:
                          'Đọc kỹ hướng dẫn và chuẩn bị một không gian yên tĩnh trước khi bắt đầu.',
                    ),
                    const SizedBox(height: 20),
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(20),
                        child: Row(
                          children: [
                            Expanded(
                              child: _Fact(
                                value: '${exam.questionCount}',
                                label: 'Câu hỏi',
                                icon: Icons.quiz_outlined,
                              ),
                            ),
                            Expanded(
                              child: _Fact(
                                value: '${exam.durationMinutes}',
                                label: 'Phút',
                                icon: Icons.schedule_rounded,
                              ),
                            ),
                            const Expanded(
                              child: _Fact(
                                value: '3',
                                label: 'Phần thi',
                                icon: Icons.layers_outlined,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),
                    const Text(
                      'Cấu trúc đề thi',
                      style: TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 12),
                    for (final part
                        in exam.parts.isEmpty
                            ? const [
                                ExamPart(
                                  partNumber: 5,
                                  directions:
                                      'Incomplete Sentences • Câu 101–130',
                                  questionCount: 30,
                                ),
                                ExamPart(
                                  partNumber: 6,
                                  directions: 'Text Completion • Câu 131–146',
                                  questionCount: 16,
                                ),
                                ExamPart(
                                  partNumber: 7,
                                  directions:
                                      'Reading Comprehension • Câu 147–200',
                                  questionCount: 54,
                                ),
                              ]
                            : exam.parts)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: Card(
                          child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Row(
                                  children: [
                                    FlowPill(label: 'PART ${part.partNumber}'),
                                    const Spacer(),
                                    Text(
                                      '${part.questionCount} câu',
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w600,
                                        color: FlowColors.secondary,
                                      ),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 12),
                                Text(
                                  part.directions.isEmpty
                                      ? 'Chọn một đáp án đúng cho mỗi câu hỏi.'
                                      : part.directions,
                                  style: const TextStyle(height: 1.65),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),
                    if (exam.directions.isNotEmpty) ...[
                      const SizedBox(height: 12),
                      Card(
                        child: Padding(
                          padding: const EdgeInsets.all(20),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Hướng dẫn của đề',
                                style: TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              const SizedBox(height: 12),
                              SelectableText(
                                exam.directions,
                                style: const TextStyle(height: 1.6),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ],
                    const SizedBox(height: 12),
                    const Card(
                      child: Padding(
                        padding: EdgeInsets.all(20),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Lưu ý khi làm bài',
                              style: TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            SizedBox(height: 15),
                            Text(
                              '• Chọn một đáp án cho mỗi câu. Bạn có thể đổi hoặc hủy lựa chọn trước khi nộp.\n\n• Mỗi đáp án được lưu ngay khi kết nối thành công. Kiểm tra thông báo lưu để tránh mất lựa chọn khi mất mạng.\n\n• Thời gian bắt đầu khi máy chủ tạo lượt thi. Thoát ứng dụng không dừng đồng hồ.\n\n• Hết giờ, bài thi bị khóa và hệ thống ghi nhận những đáp án đã lưu.\n\n• Mở lại trên cùng thiết bị và cùng trình duyệt để khôi phục bài đang làm.',
                              style: TextStyle(
                                height: 1.65,
                                color: FlowColors.secondary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    if (_error != null)
                      Padding(
                        padding: const EdgeInsets.only(top: 16),
                        child: Text(
                          _error!,
                          style: const TextStyle(color: FlowColors.error),
                        ),
                      ),
                    const SizedBox(height: 25),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Fact extends StatelessWidget {
  const _Fact({required this.value, required this.label, required this.icon});
  final String value;
  final String label;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Column(
    children: [
      Icon(icon, color: FlowColors.forest),
      const SizedBox(height: 10),
      Text(
        value,
        style: const TextStyle(
          fontSize: 26,
          fontWeight: FontWeight.w800,
          color: FlowColors.forest,
        ),
      ),
      const SizedBox(height: 4),
      Text(
        label,
        style: const TextStyle(fontSize: 12, color: FlowColors.secondary),
      ),
    ],
  );
}
