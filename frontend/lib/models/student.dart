class Student {
  const Student({
    required this.id,
    required this.studentCode,
    required this.fullName,
    required this.className,
    required this.email,
    required this.dateOfBirth,
  });

  final int id;
  final String studentCode;
  final String fullName;
  final String className;
  final String email;
  final DateTime? dateOfBirth;

  factory Student.fromJson(Map<String, dynamic> json) => Student(
        id: (json['id'] as num).toInt(),
        studentCode: json['studentCode'] as String? ?? '',
        fullName: json['fullName'] as String? ?? '',
        className: json['className'] as String? ?? '',
        email: json['email'] as String? ?? '',
        dateOfBirth: DateTime.tryParse(json['dateOfBirth'] as String? ?? ''),
      );
}
