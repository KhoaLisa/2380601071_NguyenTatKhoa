import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import '../models/student.dart';

class StudentApi {
  StudentApi({http.Client? client, String? baseUrl})
      : _client = client ?? http.Client(),
        baseUrl = baseUrl ?? _defaultBaseUrl;

  final http.Client _client;
  final String baseUrl;

  static const String _configuredUrl = String.fromEnvironment('API_URL');

  static String get _defaultBaseUrl {
    if (_configuredUrl.isNotEmpty) return _configuredUrl;
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5000';
    }
    return 'http://localhost:5000';
  }

  Future<List<Student>> getStudents() async {
    final response = await _client
        .get(Uri.parse('$baseUrl/api/students'))
        .timeout(const Duration(seconds: 10));
    if (response.statusCode != 200) {
      throw Exception('Máy chủ trả về lỗi ${response.statusCode}.');
    }
    final data = jsonDecode(utf8.decode(response.bodyBytes));
    if (data is! List) throw const FormatException('Dữ liệu API không hợp lệ.');
    return data
        .map((item) => Student.fromJson(item as Map<String, dynamic>))
        .toList(growable: false);
  }

  void dispose() => _client.close();
}
