class PublishedExam {
  const PublishedExam({
    required this.id,
    required this.code,
    required this.title,
    required this.durationMinutes,
    required this.questionCount,
    required this.canStart,
    this.directions = '',
    this.parts = const [],
  });
  final String id;
  final String code;
  final String title;
  final int durationMinutes;
  final int questionCount;
  final bool canStart;
  final String directions;
  final List<ExamPart> parts;

  factory PublishedExam.fromJson(Map<String, dynamic> json) => PublishedExam(
    id: json['id'] as String,
    code: json['code'] as String? ?? '',
    title: json['title'] as String? ?? '',
    durationMinutes: json['durationMinutes'] as int? ?? 0,
    questionCount: json['questionCount'] as int? ?? 0,
    canStart: json['canStart'] as bool? ?? false,
    directions: json['directions'] as String? ?? '',
    parts: (json['parts'] as List<dynamic>? ?? [])
        .map((p) => ExamPart.fromJson(p as Map<String, dynamic>))
        .toList(),
  );
}

class ExamPart {
  const ExamPart({
    required this.partNumber,
    required this.directions,
    required this.questionCount,
  });
  final int partNumber;
  final String directions;
  final int questionCount;
  factory ExamPart.fromJson(Map<String, dynamic> json) => ExamPart(
    partNumber: (json['partNumber'] as num?)?.toInt() ?? 0,
    directions: json['directions'] as String? ?? '',
    questionCount: (json['questionCount'] as num?)?.toInt() ?? 0,
  );
}

class AttemptSummary {
  const AttemptSummary({
    required this.attemptId,
    required this.examId,
    required this.examTitle,
    required this.status,
    required this.startedAtUtc,
    required this.dueAtUtc,
    this.result,
  });
  final String attemptId;
  final String examId;
  final String examTitle;
  final int status;
  final DateTime startedAtUtc;
  final DateTime dueAtUtc;
  final AttemptResult? result;
  bool get inProgress => status == 0;
  factory AttemptSummary.fromJson(Map<String, dynamic> json) => AttemptSummary(
    attemptId: json['attemptId'] as String,
    examId: json['examId'] as String,
    examTitle: json['examTitle'] as String? ?? '',
    status: json['status'] as int? ?? 0,
    startedAtUtc: DateTime.parse(json['startedAtUtc'] as String).toUtc(),
    dueAtUtc: DateTime.parse(json['dueAtUtc'] as String).toUtc(),
    result: json['result'] == null
        ? null
        : AttemptResult.fromJson(json['result'] as Map<String, dynamic>),
  );
}

class StartExamResult {
  const StartExamResult({
    required this.attemptId,
    required this.startedAtUtc,
    required this.dueAtUtc,
    required this.resumedExistingAttempt,
  });
  final String attemptId;
  final DateTime startedAtUtc;
  final DateTime dueAtUtc;
  final bool resumedExistingAttempt;

  factory StartExamResult.fromJson(Map<String, dynamic> json) =>
      StartExamResult(
        attemptId: json['attemptId'] as String,
        startedAtUtc: DateTime.parse(json['startedAtUtc'] as String).toUtc(),
        dueAtUtc: DateTime.parse(json['dueAtUtc'] as String).toUtc(),
        resumedExistingAttempt:
            json['resumedExistingAttempt'] as bool? ?? false,
      );
}

class ExamAttempt {
  ExamAttempt({
    required this.attemptId,
    required this.examId,
    required this.examTitle,
    required this.serverNowUtc,
    required this.startedAtUtc,
    required this.dueAtUtc,
    required this.status,
    required this.groups,
    required this.questions,
    this.submittedAtUtc,
    this.result,
  });
  final String attemptId;
  final String examId;
  final String examTitle;
  final DateTime serverNowUtc;
  final DateTime startedAtUtc;
  final DateTime dueAtUtc;
  final DateTime? submittedAtUtc;
  final int status;
  final List<PassageGroup> groups;
  final List<ExamQuestion> questions;
  final AttemptResult? result;
  bool get inProgress => status == 0;

  factory ExamAttempt.fromJson(Map<String, dynamic> json) => ExamAttempt(
    attemptId: json['attemptId'] as String,
    examId: json['examId'] as String,
    examTitle: json['examTitle'] as String? ?? '',
    serverNowUtc: DateTime.parse(json['serverNowUtc'] as String).toUtc(),
    startedAtUtc: DateTime.parse(json['startedAtUtc'] as String).toUtc(),
    dueAtUtc: DateTime.parse(json['dueAtUtc'] as String).toUtc(),
    submittedAtUtc: json['submittedAtUtc'] == null
        ? null
        : DateTime.parse(json['submittedAtUtc'] as String).toUtc(),
    status: json['status'] as int? ?? 0,
    groups: (json['groups'] as List<dynamic>? ?? const [])
        .map((item) => PassageGroup.fromJson(item as Map<String, dynamic>))
        .toList(),
    questions: (json['questions'] as List<dynamic>? ?? const [])
        .map((item) => ExamQuestion.fromJson(item as Map<String, dynamic>))
        .toList(),
    result: json['result'] == null
        ? null
        : AttemptResult.fromJson(json['result'] as Map<String, dynamic>),
  );
}

class PassageGroup {
  const PassageGroup({
    required this.id,
    required this.title,
    required this.part,
    required this.displayOrder,
    required this.contentBlocks,
  });
  final String id;
  final String title;
  final int part;
  final int displayOrder;
  final List<ContentBlock> contentBlocks;
  factory PassageGroup.fromJson(Map<String, dynamic> json) => PassageGroup(
    id: json['id'] as String,
    title: json['title'] as String? ?? '',
    part: json['part'] as int? ?? 0,
    displayOrder: json['displayOrder'] as int? ?? 0,
    contentBlocks: (json['contentBlocks'] as List<dynamic>? ?? const [])
        .map((item) => ContentBlock.fromJson(item as Map<String, dynamic>))
        .toList(),
  );
}

class ContentBlock {
  const ContentBlock({
    required this.id,
    required this.documentKey,
    required this.displayOrder,
    required this.type,
    this.textContent,
    this.contentUrl,
    this.mediaType,
  });
  final String id;
  final String documentKey;
  final int displayOrder;
  final int type;
  final String? textContent;
  final String? contentUrl;
  final String? mediaType;
  factory ContentBlock.fromJson(Map<String, dynamic> json) => ContentBlock(
    id: json['id'] as String,
    documentKey: json['documentKey'] as String? ?? '',
    displayOrder: json['displayOrder'] as int? ?? 0,
    type: json['type'] as int? ?? 1,
    textContent: json['textContent'] as String?,
    contentUrl: json['contentUrl'] as String?,
    mediaType: json['mediaType'] as String?,
  );
}

class ExamQuestion {
  ExamQuestion({
    required this.id,
    required this.part,
    required this.originalNumber,
    required this.displayOrder,
    required this.prompt,
    required this.options,
    this.passageGroupId,
    this.selectedOptionId,
  });
  final String id;
  final String? passageGroupId;
  final int part;
  final int originalNumber;
  final int displayOrder;
  final String prompt;
  String? selectedOptionId;
  final List<QuestionOption> options;
  factory ExamQuestion.fromJson(Map<String, dynamic> json) => ExamQuestion(
    id: json['id'] as String,
    passageGroupId: json['passageGroupId'] as String?,
    part: json['part'] as int? ?? 0,
    originalNumber: json['originalNumber'] as int? ?? 0,
    displayOrder: json['displayOrder'] as int? ?? 0,
    prompt: json['prompt'] as String? ?? '',
    selectedOptionId: json['selectedOptionId'] as String?,
    options: (json['options'] as List<dynamic>? ?? const [])
        .map((item) => QuestionOption.fromJson(item as Map<String, dynamic>))
        .toList(),
  );
}

class QuestionOption {
  const QuestionOption({
    required this.id,
    required this.key,
    required this.text,
  });
  final String id;
  final String key;
  final String text;
  factory QuestionOption.fromJson(Map<String, dynamic> json) => QuestionOption(
    id: json['id'] as String,
    key: json['key'] as String? ?? '',
    text: json['text'] as String? ?? '',
  );
}

class AttemptResult {
  const AttemptResult({
    required this.attemptId,
    required this.status,
    required this.submittedAtUtc,
    required this.totalQuestions,
    required this.correctCount,
    required this.incorrectCount,
    required this.skippedCount,
    required this.correctPercentage,
  });
  final String attemptId;
  final int status;
  final DateTime submittedAtUtc;
  final int totalQuestions;
  final int correctCount;
  final int incorrectCount;
  final int skippedCount;
  final double correctPercentage;
  factory AttemptResult.fromJson(Map<String, dynamic> json) => AttemptResult(
    attemptId: json['attemptId'] as String,
    status: json['status'] as int? ?? 1,
    submittedAtUtc: DateTime.parse(json['submittedAtUtc'] as String).toUtc(),
    totalQuestions: json['totalQuestions'] as int? ?? 0,
    correctCount: json['correctCount'] as int? ?? 0,
    incorrectCount: json['incorrectCount'] as int? ?? 0,
    skippedCount: json['skippedCount'] as int? ?? 0,
    correctPercentage: (json['correctPercentage'] as num? ?? 0).toDouble(),
  );
}

class SaveAnswerResult {
  const SaveAnswerResult({
    required this.accepted,
    required this.isFinalized,
    this.result,
  });
  final bool accepted;
  final bool isFinalized;
  final AttemptResult? result;
  factory SaveAnswerResult.fromJson(Map<String, dynamic> json) =>
      SaveAnswerResult(
        accepted: json['accepted'] as bool? ?? false,
        isFinalized: json['isFinalized'] as bool? ?? false,
        result: json['result'] == null
            ? null
            : AttemptResult.fromJson(json['result'] as Map<String, dynamic>),
      );
}
