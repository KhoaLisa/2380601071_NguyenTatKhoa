import 'package:flutter/material.dart';

import '../models/student.dart';
import '../services/student_api.dart';

class StudentListScreen extends StatefulWidget {
  const StudentListScreen({super.key, this.api});

  final StudentApi? api;

  @override
  State<StudentListScreen> createState() => _StudentListScreenState();
}

class _StudentListScreenState extends State<StudentListScreen> {
  late final StudentApi _api;
  late Future<List<Student>> _students;

  @override
  void initState() {
    super.initState();
    _api = widget.api ?? StudentApi();
    _students = _api.getStudents();
  }

  @override
  void dispose() {
    if (widget.api == null) _api.dispose();
    super.dispose();
  }

  Future<void> _reload() async {
    final request = _api.getStudents();
    setState(() => _students = request);
    await request;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: const Color(0xFF3659E3),
        foregroundColor: Colors.white,
        title: const Text('Danh sách sinh viên',
            style: TextStyle(fontWeight: FontWeight.w700)),
        actions: [
          IconButton(
            tooltip: 'Tải lại',
            onPressed: _reload,
            icon: const Icon(Icons.refresh_rounded),
          ),
        ],
      ),
      body: FutureBuilder<List<Student>>(
        future: _students,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) return _ErrorView(onRetry: _reload);

          final students = snapshot.data ?? const <Student>[];
          if (students.isEmpty) {
            return RefreshIndicator(
              onRefresh: _reload,
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                children: const [
                  SizedBox(height: 180),
                  Icon(Icons.school_outlined, size: 72, color: Colors.grey),
                  SizedBox(height: 16),
                  Center(child: Text('Chưa có sinh viên nào')),
                ],
              ),
            );
          }

          return RefreshIndicator(
            onRefresh: _reload,
            child: ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: students.length + 1,
              separatorBuilder: (_, index) =>
                  SizedBox(height: index == 0 ? 16 : 10),
              itemBuilder: (context, index) {
                if (index == 0) return _Summary(count: students.length);
                return _StudentCard(student: students[index - 1]);
              },
            ),
          );
        },
      ),
    );
  }
}

class _Summary extends StatelessWidget {
  const _Summary({required this.count});

  final int count;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFF3659E3), Color(0xFF6C7FF2)],
        ),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Row(
        children: [
          const Icon(Icons.groups_rounded, color: Colors.white, size: 40),
          const SizedBox(width: 14),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('$count sinh viên',
                  style: const TextStyle(
                      color: Colors.white,
                      fontSize: 20,
                      fontWeight: FontWeight.w700)),
              const Text('Dữ liệu từ Microsoft SQL Server',
                  style: TextStyle(color: Color(0xFFDDE3FF))),
            ],
          ),
        ],
      ),
    );
  }
}

class _StudentCard extends StatelessWidget {
  const _StudentCard({required this.student});

  final Student student;

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0,
      color: Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: const BorderSide(color: Color(0xFFE8EAF2)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            CircleAvatar(
              radius: 25,
              backgroundColor: const Color(0xFFE5E9FF),
              child: Text(
                student.fullName.isEmpty
                    ? '?'
                    : student.fullName[0].toUpperCase(),
                style: const TextStyle(
                  color: Color(0xFF3659E3),
                  fontWeight: FontWeight.w800,
                  fontSize: 19,
                ),
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(student.fullName,
                      style: const TextStyle(
                          fontSize: 16, fontWeight: FontWeight.w700)),
                  const SizedBox(height: 7),
                  Text('${student.studentCode}  •  ${student.className}',
                      style: const TextStyle(color: Color(0xFF596174))),
                  if (student.email.isNotEmpty) ...[
                    const SizedBox(height: 5),
                    Text(student.email,
                        style: const TextStyle(
                            color: Color(0xFF7A8293), fontSize: 13)),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.onRetry});

  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off_rounded,
                size: 70, color: Color(0xFF7A8293)),
            const SizedBox(height: 16),
            const Text('Không thể kết nối đến máy chủ',
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
            const SizedBox(height: 8),
            const Text('Hãy kiểm tra API đang chạy rồi thử lại.',
                textAlign: TextAlign.center,
                style: TextStyle(color: Color(0xFF7A8293))),
            const SizedBox(height: 20),
            FilledButton.icon(
              onPressed: onRetry,
              icon: const Icon(Icons.refresh_rounded),
              label: const Text('Thử lại'),
            ),
          ],
        ),
      ),
    );
  }
}
