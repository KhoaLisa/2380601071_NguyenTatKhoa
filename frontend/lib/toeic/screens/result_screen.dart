import 'package:flutter/material.dart';
import '../models/exam_models.dart';
import '../theme/toeic_theme.dart';
import '../widgets/flow_widgets.dart';

class ResultScreen extends StatelessWidget {
  const ResultScreen({
    super.key,
    required this.result,
    this.examTitle,
    this.startedAtUtc,
  });
  final AttemptResult result;
  final String? examTitle;
  final DateTime? startedAtUtc;

  @override
  Widget build(BuildContext context) {
    final sum =
        result.correctCount + result.incorrectCount + result.skippedCount;
    final time = result.submittedAtUtc.toLocal();
    final duration = startedAtUtc == null
        ? null
        : result.submittedAtUtc.difference(startedAtUtc!);
    return Scaffold(
      appBar: AppBar(
        title: const FlowBrand(subtitle: 'Kết quả'),
        automaticallyImplyLeading: false,
      ),
      body: SafeArea(
        top: false,
        child: SingleChildScrollView(
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 680),
              child: Padding(
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Container(
                      padding: const EdgeInsets.all(20),
                      decoration: BoxDecoration(
                        color: FlowColors.soft,
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          FlowPill(
                            label: result.status == 2
                                ? 'Đã ghi nhận khi hết giờ'
                                : 'Đã ghi nhận kết quả',
                            color: FlowColors.cream,
                          ),
                          const SizedBox(height: 14),
                          const Text(
                            'Kết quả bài thi',
                            style: TextStyle(
                              fontSize: 25,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          if (examTitle != null)
                            Padding(
                              padding: const EdgeInsets.only(top: 8),
                              child: Text(
                                examTitle!,
                                style: const TextStyle(
                                  height: 1.5,
                                  color: FlowColors.secondary,
                                ),
                              ),
                            ),
                          const SizedBox(height: 8),
                          Text(
                            'Hoàn tất ${time.day}/${time.month}/${time.year} • ${time.hour.toString().padLeft(2, '0')}:${time.minute.toString().padLeft(2, '0')}',
                            style: const TextStyle(
                              fontSize: 12,
                              color: FlowColors.secondary,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 18),
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(22),
                        child: Column(
                          children: [
                            const SizedBox(height: 12),
                            SizedBox.square(
                              dimension: 185,
                              child: Stack(
                                alignment: Alignment.center,
                                children: [
                                  SizedBox.square(
                                    dimension: 174,
                                    child: CircularProgressIndicator(
                                      value: (result.correctPercentage / 100)
                                          .clamp(0.0, 1.0),
                                      strokeWidth: 12,
                                      strokeCap: StrokeCap.round,
                                      backgroundColor: FlowColors.soft,
                                      color: FlowColors.forest,
                                    ),
                                  ),
                                  Column(
                                    mainAxisSize: MainAxisSize.min,
                                    children: [
                                      Text(
                                        '${result.correctPercentage.toStringAsFixed(2)}%',
                                        key: const Key('resultPercentage'),
                                        style: const TextStyle(
                                          fontSize: 31,
                                          fontWeight: FontWeight.w800,
                                          color: FlowColors.forest,
                                        ),
                                      ),
                                      const SizedBox(height: 5),
                                      const Text(
                                        'TỶ LỆ ĐÚNG',
                                        style: TextStyle(
                                          fontSize: 10,
                                          letterSpacing: 1.4,
                                          color: FlowColors.secondary,
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                            const SizedBox(height: 26),
                            const Text(
                              'Bạn đã hoàn thành bài thi.',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontSize: 17,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            const SizedBox(height: 8),
                            const Text(
                              'Dành một chút thời gian nhìn lại hành trình và tiếp tục tiến bộ mỗi ngày.',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontSize: 13,
                                color: FlowColors.secondary,
                                height: 1.6,
                              ),
                            ),
                            const SizedBox(height: 24),
                            Row(
                              children: [
                                Expanded(
                                  child: _Score(
                                    label: 'Đúng',
                                    value: result.correctCount,
                                    color: FlowColors.success,
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: _Score(
                                    label: 'Sai',
                                    value: result.incorrectCount,
                                    color: FlowColors.error,
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: _Score(
                                    label: 'Chưa làm',
                                    value: result.skippedCount,
                                    color: FlowColors.secondary,
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 18),
                            Text(
                              '$sum / ${result.totalQuestions} câu',
                              style: TextStyle(
                                fontSize: 13,
                                color: sum == result.totalQuestions
                                    ? FlowColors.secondary
                                    : FlowColors.error,
                              ),
                            ),
                            if (sum != result.totalQuestions)
                              const Padding(
                                padding: EdgeInsets.only(top: 8),
                                child: Text(
                                  'Số liệu kết quả chưa khớp. Hãy tải lại bài thi để kiểm tra.',
                                  textAlign: TextAlign.center,
                                  style: TextStyle(color: FlowColors.error),
                                ),
                              ),
                            if (duration != null) ...[
                              const Divider(height: 30),
                              Row(
                                children: [
                                  const Icon(
                                    Icons.timelapse_rounded,
                                    size: 18,
                                    color: FlowColors.sage,
                                  ),
                                  const SizedBox(width: 8),
                                  const Expanded(
                                    child: Text(
                                      'Thời gian làm bài',
                                      style: TextStyle(fontSize: 12),
                                    ),
                                  ),
                                  Text(
                                    '${duration.inMinutes} phút ${duration.inSeconds.remainder(60)} giây',
                                    style: const TextStyle(
                                      fontSize: 12,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),
                    FilledButton.icon(
                      key: const Key('backToExamsButton'),
                      onPressed: () => Navigator.of(context).pop(),
                      icon: const Icon(Icons.arrow_back_rounded, size: 18),
                      label: const Text('Về danh sách đề thi'),
                    ),
                    const SizedBox(height: 24),
                    const Icon(Icons.spa_outlined, color: FlowColors.sage),
                    const SizedBox(height: 10),
                    const Text(
                      'Bền bỉ từng bước. Vững vàng mỗi ngày.',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: FlowColors.secondary,
                        fontSize: 12,
                      ),
                    ),
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

class _Score extends StatelessWidget {
  const _Score({required this.label, required this.value, required this.color});
  final String label;
  final int value;
  final Color color;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 16),
    decoration: BoxDecoration(
      color: color.withValues(alpha: .08),
      borderRadius: BorderRadius.circular(12),
    ),
    child: Column(
      children: [
        Text(label, style: TextStyle(fontSize: 11, color: color)),
        const SizedBox(height: 9),
        Text(
          '$value',
          style: TextStyle(
            fontSize: 27,
            fontWeight: FontWeight.w800,
            color: color,
          ),
        ),
        const SizedBox(height: 3),
        const Text(
          'câu',
          style: TextStyle(fontSize: 10, color: FlowColors.secondary),
        ),
      ],
    ),
  );
}
