import 'dart:async';
import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import '../models/exam_models.dart';
import 'demo_identity_store.dart';

class ApiException implements Exception {
  const ApiException(
    this.message, {
    this.statusCode,
    this.requestOutcomeUnknown = false,
  });
  final String message;
  final int? statusCode;
  // A transport timeout/disconnect does not prove that a write was rolled back.
  final bool requestOutcomeUnknown;
  @override
  String toString() => message;
}

class ToeicApi {
  ToeicApi({
    http.Client? client,
    String? baseUrl,
    Future<String> Function()? installationId,
    Duration requestTimeout = const Duration(seconds: 20),
    Duration startTimeout = const Duration(seconds: 45),
  }) : _client = client ?? http.Client(),
       _requestTimeout = requestTimeout,
       _startTimeout = startTimeout,
       _installationId =
           installationId ?? DemoIdentityStore().getInstallationId,
       baseUrl = (baseUrl ?? _defaultBaseUrl).replaceFirst(RegExp(r'/$'), '');
  final http.Client _client;
  final Future<String> Function() _installationId;
  final String baseUrl;
  final Duration _requestTimeout;
  final Duration _startTimeout;
  String? _accessToken;
  DateTime? _expiresAt;
  Future<void>? _session;
  final Map<String, Future<Uint8List>> _images = {};
  final Map<String, Future<StartExamResult>> _starts = {};
  static const _configuredUrl = String.fromEnvironment('API_URL');
  static String get _defaultBaseUrl {
    if (_configuredUrl.isNotEmpty) return _configuredUrl;
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5000';
    }
    return 'http://localhost:5000';
  }

  Map<String, String> get _headers => {
    'Accept': 'application/json',
    'Content-Type': 'application/json; charset=utf-8',
    if (_accessToken != null) 'Authorization': 'Bearer $_accessToken',
  };

  Future<void> _ensureSession() {
    if (_accessToken != null &&
        (_expiresAt == null || DateTime.now().isBefore(_expiresAt!))) {
      return Future.value();
    }
    return _session ??= _bootstrap().whenComplete(() => _session = null);
  }

  Future<void> _bootstrap() async {
    final installationId = await _installationId();
    final result = await _send(
      'POST',
      '/api/demo/student-session',
      body: {'installationId': installationId},
      needsSession: false,
    );
    final data = result as Map<String, dynamic>;
    final token = data['accessToken'] as String?;
    if (token == null || token.isEmpty) {
      throw const ApiException('Máy chủ chưa cấp phiên học viên.');
    }
    _accessToken = token;
    _expiresAt = DateTime.now().add(
      Duration(
        seconds: ((data['expireInSeconds'] as num?)?.toInt() ?? 3600) - 30,
      ),
    );
  }

  // Compatibility with the pre-existing, disconnected login screen.
  Future<void> authenticate(String username, String password) async {
    final result =
        await _send(
              'POST',
              '/api/TokenAuth/Authenticate',
              body: {
                'usernameOrEmailAddress': username.trim(),
                'password': password,
                'rememberClient': true,
              },
              needsSession: false,
            )
            as Map<String, dynamic>;
    _accessToken = result['accessToken'] as String?;
    _expiresAt = null;
    if (_accessToken == null) {
      throw const ApiException('Máy chủ không cấp access token.');
    }
  }

  void logout() {
    _accessToken = null;
    _expiresAt = null;
    _images.clear();
  }

  Future<List<PublishedExam>> getPublishedExams() async {
    final result =
        await _send('GET', '/api/services/app/StudentExam/GetPublishedExams')
            as Map<String, dynamic>;
    return (result['items'] as List<dynamic>? ?? [])
        .map((item) => PublishedExam.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<List<AttemptSummary>> getMyAttempts() async {
    final result =
        await _send('GET', '/api/services/app/StudentExam/GetMyAttempts')
            as Map<String, dynamic>;
    return (result['items'] as List<dynamic>? ?? [])
        .map((item) => AttemptSummary.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<StartExamResult> startExam(String examId) => _starts.putIfAbsent(
    examId,
    () => _startOrRecover(examId).whenComplete(() {
      _starts.remove(examId);
    }),
  );

  Future<StartExamResult> _startOrRecover(String examId) async {
    try {
      return StartExamResult.fromJson(
        await _send(
              'POST',
              '/api/services/app/StudentExam/Start',
              body: {'examId': examId},
              timeout: _startTimeout,
            )
            as Map<String, dynamic>,
      );
    } on ApiException catch (error) {
      if (!error.requestOutcomeUnknown) rethrow;
      // Do not blindly resend a POST whose outcome is unknown. The server may
      // have committed while its response was lost. Read our own history once.
      try {
        final attempts = await getMyAttempts();
        final active = attempts
            .where((a) => a.examId == examId && a.inProgress)
            .firstOrNull;
        if (active != null) {
          return StartExamResult(
            attemptId: active.attemptId,
            startedAtUtc: active.startedAtUtc,
            dueAtUtc: active.dueAtUtc,
            resumedExistingAttempt: true,
          );
        }
      } on ApiException {
        // Keep the original uncertainty explicit if recovery is also offline.
      }
      throw const ApiException(
        'Chưa xác nhận được lượt thi. Hãy tải lại danh sách và kiểm tra mục '
        'Đang làm để tiếp tục; không tạo lại thời gian làm bài.',
        requestOutcomeUnknown: true,
      );
    }
  }

  Future<ExamAttempt> getAttempt(String attemptId) async =>
      ExamAttempt.fromJson(
        await _send(
              'GET',
              '/api/services/app/StudentExam/GetAttempt',
              query: {'attemptId': attemptId},
            )
            as Map<String, dynamic>,
      );

  Future<SaveAnswerResult> saveAnswer({
    required String attemptId,
    required String questionId,
    required String? selectedOptionId,
  }) async {
    final result = await _send(
      'POST',
      '/api/services/app/StudentExam/SaveAnswer',
      body: {
        'attemptId': attemptId,
        'questionId': questionId,
        'selectedOptionId': selectedOptionId,
      },
    );
    return SaveAnswerResult.fromJson(result as Map<String, dynamic>);
  }

  Future<AttemptResult> submit(String attemptId) async =>
      AttemptResult.fromJson(
        await _send(
              'POST',
              '/api/services/app/StudentExam/Submit',
              body: {'attemptId': attemptId},
            )
            as Map<String, dynamic>,
      );

  Future<Uint8List> loadImage(String relativePath) => _images.putIfAbsent(
    relativePath,
    () async {
      try {
        await _ensureSession();
        final uri = Uri.parse(baseUrl).resolve(relativePath);
        if (uri.origin != Uri.parse(baseUrl).origin) {
          throw const ApiException('Địa chỉ ảnh không thuộc máy chủ bài thi.');
        }
        var response = await _client
            .get(uri, headers: _headers)
            .timeout(_requestTimeout);
        if (response.statusCode == 401) {
          _accessToken = null;
          await _ensureSession();
          response = await _client
              .get(uri, headers: _headers)
              .timeout(_requestTimeout);
        }
        if (response.statusCode != 200) {
          throw const ApiException('Không tải được ảnh bài đọc.');
        }
        return response.bodyBytes;
      } on Object {
        _images.remove(relativePath);
        rethrow;
      }
    },
  );

  Future<dynamic> _send(
    String method,
    String path, {
    Map<String, dynamic>? body,
    Map<String, String>? query,
    bool needsSession = true,
    bool retry = true,
    Duration? timeout,
  }) async {
    try {
      if (needsSession) await _ensureSession();
      final uri = Uri.parse('$baseUrl$path').replace(queryParameters: query);
      final response =
          await (method == 'GET'
                  ? _client.get(uri, headers: _headers)
                  : _client.post(
                      uri,
                      headers: _headers,
                      body: jsonEncode(body ?? {}),
                    ))
              .timeout(timeout ?? _requestTimeout);
      if (response.statusCode == 401 && needsSession && retry) {
        _accessToken = null;
        return await _send(
          method,
          path,
          body: body,
          query: query,
          retry: false,
          timeout: timeout,
        );
      }
      return _unwrap(response);
    } on TimeoutException {
      throw const ApiException(
        'Máy chủ phản hồi quá lâu. Kiểm tra kết nối rồi thử lại.',
        requestOutcomeUnknown: true,
      );
    } on http.ClientException {
      throw const ApiException(
        'Không kết nối được máy chủ. Kiểm tra mạng và địa chỉ API.',
        requestOutcomeUnknown: true,
      );
    } on FormatException {
      throw const ApiException('Dữ liệu máy chủ không đúng định dạng.');
    }
  }

  dynamic _unwrap(http.Response response) {
    Map<String, dynamic>? envelope;
    try {
      final decoded = jsonDecode(utf8.decode(response.bodyBytes));
      if (decoded is Map<String, dynamic>) envelope = decoded;
    } on FormatException {
      /* Handle non-JSON error pages below. */
    }
    if (response.statusCode < 200 ||
        response.statusCode >= 300 ||
        envelope?['success'] == false) {
      final error = envelope?['error'] as Map<String, dynamic>?;
      final validation = error?['validationErrors'] as List<dynamic>?;
      final details = (validation ?? [])
          .map((e) => (e as Map<String, dynamic>)['message'])
          .whereType<String>()
          .join('\n');
      throw ApiException(
        details.isNotEmpty
            ? details
            : error?['details'] as String? ??
                  error?['message'] as String? ??
                  (response.statusCode == 404 &&
                          response.request?.url.path.contains('/api/demo/') ==
                              true
                      ? 'Phiên demo học viên chưa được bật trên máy chủ phát triển.'
                      : 'Máy chủ trả lỗi ${response.statusCode}.'),
        statusCode: response.statusCode,
      );
    }
    if (envelope == null || !envelope.containsKey('result')) {
      throw const ApiException('Phản hồi máy chủ không đúng hợp đồng API.');
    }
    return envelope['result'];
  }

  void dispose() => _client.close();
}
