import 'package:flutter/material.dart';

import 'toeic/api/toeic_api.dart';
import 'toeic/screens/exam_list_screen.dart';
import 'toeic/theme/toeic_theme.dart';

void main() => runApp(const ToeicReadingApp());

class ToeicReadingApp extends StatefulWidget {
  const ToeicReadingApp({super.key, this.api});

  final ToeicApi? api;

  @override
  State<ToeicReadingApp> createState() => _ToeicReadingAppState();
}

class _ToeicReadingAppState extends State<ToeicReadingApp> {
  late final ToeicApi _api;

  @override
  void initState() {
    super.initState();
    _api = widget.api ?? ToeicApi();
  }

  @override
  void dispose() {
    if (widget.api == null) _api.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'TOEICFlow',
      theme: toeicFlowTheme(),
      home: ExamListScreen(api: _api),
    );
  }
}
