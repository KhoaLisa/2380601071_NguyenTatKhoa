import 'dart:math';
import 'package:shared_preferences/shared_preferences.dart';

/// A random capability identifying this demo installation. Never an admin credential.
class DemoIdentityStore {
  Future<String>? _identity;

  Future<String> getInstallationId() => _identity ??= _readOrCreate();

  Future<String> _readOrCreate() async {
    final preferences = await SharedPreferences.getInstance();
    const key = 'toeicflow.demo.installation.v1';
    final existing = preferences.getString(key);
    if (existing != null && RegExp(r'^[a-f0-9]{64}$').hasMatch(existing)) {
      return existing;
    }
    final random = Random.secure();
    final value = List.generate(
      32,
      (_) => random.nextInt(256).toRadixString(16).padLeft(2, '0'),
    ).join();
    if (!await preferences.setString(key, value)) {
      throw StateError(
        'Không lưu được phiên thiết bị. Hãy cho phép bộ nhớ cục bộ để khôi phục bài thi.',
      );
    }
    return value;
  }
}
