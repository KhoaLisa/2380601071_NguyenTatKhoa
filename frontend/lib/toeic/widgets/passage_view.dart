import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:html/parser.dart' as html;
import '../api/toeic_api.dart';
import '../models/exam_models.dart';
import '../theme/toeic_theme.dart';
import 'flow_widgets.dart';

class PassageView extends StatelessWidget {
  const PassageView({
    super.key,
    required this.group,
    required this.api,
    required this.questionNumbers,
  });
  final PassageGroup group;
  final ToeicApi api;
  final List<int> questionNumbers;

  @override
  Widget build(BuildContext context) {
    final blocks = [...group.contentBlocks]
      ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
    String? lastDocument;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(
                  Icons.article_outlined,
                  color: FlowColors.forest,
                  size: 20,
                ),
                SizedBox(width: 8),
                Text(
                  'BÀI ĐỌC',
                  style: TextStyle(
                    fontSize: 11,
                    letterSpacing: 1.2,
                    color: FlowColors.secondary,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Text(
              group.title.isEmpty ? 'Bài đọc Part ${group.part}' : group.title,
              style: const TextStyle(
                fontSize: 19,
                fontWeight: FontWeight.w700,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'Dành cho câu ${questionNumbers.join(', ')}',
              style: const TextStyle(fontSize: 12, color: FlowColors.secondary),
            ),
            const Divider(height: 30),
            for (final block in blocks) ...[
              if (block.documentKey.isNotEmpty &&
                  block.documentKey != lastDocument) ...[
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: FlowPill(
                    label: 'Tài liệu ${lastDocument = block.documentKey}',
                  ),
                ),
              ],
              Padding(
                padding: const EdgeInsets.only(bottom: 18),
                child: PassageContentBlock(block: block, api: api),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class PassageContentBlock extends StatelessWidget {
  const PassageContentBlock({
    super.key,
    required this.block,
    required this.api,
  });
  final ContentBlock block;
  final ToeicApi api;

  @override
  Widget build(BuildContext context) {
    if (block.type == 2) {
      if (block.contentUrl == null) {
        return const Text(
          'Bài đọc thiếu tài nguyên ảnh.',
          style: TextStyle(color: FlowColors.error),
        );
      }
      return _PassageImage(api: api, path: block.contentUrl!);
    }
    if (block.type == 3) return ReadingTable(content: block.textContent ?? '');
    final text = (block.textContent ?? '').replaceAllMapped(
      RegExp(r'\{\{BLANK:(\d+)\}\}'),
      (m) => '_____ (${m[1]}) _____',
    );
    return SelectableText(
      text,
      style: const TextStyle(fontSize: 16, height: 1.8, color: FlowColors.text),
    );
  }
}

class _PassageImage extends StatefulWidget {
  const _PassageImage({required this.api, required this.path});
  final ToeicApi api;
  final String path;
  @override
  State<_PassageImage> createState() => _PassageImageState();
}

class _PassageImageState extends State<_PassageImage> {
  late Future<Uint8List> _image;
  @override
  void initState() {
    super.initState();
    _image = widget.api.loadImage(widget.path);
  }

  @override
  void didUpdateWidget(covariant _PassageImage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.path != widget.path) {
      _image = widget.api.loadImage(widget.path);
    }
  }

  @override
  Widget build(BuildContext context) => FutureBuilder<Uint8List>(
    future: _image,
    builder: (context, snapshot) {
      if (snapshot.hasError) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Không tải được ảnh bài đọc.',
              style: TextStyle(color: FlowColors.error),
            ),
            TextButton.icon(
              onPressed: () => setState(() {
                _image = widget.api.loadImage(widget.path);
              }),
              icon: const Icon(Icons.refresh_rounded),
              label: const Text('Tải lại ảnh'),
            ),
          ],
        );
      }
      if (!snapshot.hasData) {
        return const SizedBox(
          height: 130,
          child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
        );
      }
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(10),
            child: Image.memory(
              snapshot.data!,
              width: double.infinity,
              fit: BoxFit.contain,
              errorBuilder: (_, error, trace) => const Text(
                'Dữ liệu ảnh không hợp lệ.',
                style: TextStyle(color: FlowColors.error),
              ),
            ),
          ),
          TextButton.icon(
            onPressed: () => showDialog<void>(
              context: context,
              builder: (context) => Dialog.fullscreen(
                child: Scaffold(
                  appBar: AppBar(title: const Text('Ảnh bài đọc')),
                  body: InteractiveViewer(
                    minScale: .5,
                    maxScale: 6,
                    child: Center(
                      child: Image.memory(snapshot.data!, fit: BoxFit.contain),
                    ),
                  ),
                ),
              ),
            ),
            icon: const Icon(Icons.zoom_in_rounded, size: 18),
            label: const Text('Phóng to ảnh'),
          ),
        ],
      );
    },
  );
}

/// Backend emits sanitized HTML. Parse it as data; never run HTML/JavaScript.
class ReadingTable extends StatelessWidget {
  const ReadingTable({super.key, required this.content});
  final String content;

  @override
  Widget build(BuildContext context) {
    final document = html.parseFragment(content);
    final rows = document
        .querySelectorAll('tr')
        .map(
          (row) => row.children
              .where((cell) => cell.localName == 'td' || cell.localName == 'th')
              .toList(),
        )
        .where((row) => row.isNotEmpty)
        .toList();
    if (rows.isEmpty) {
      return SelectableText(
        document.text ?? '',
        style: const TextStyle(height: 1.6),
      );
    }
    final columns = rows.fold<int>(
      0,
      (count, row) => row.length > count ? row.length : count,
    );
    return LayoutBuilder(
      builder: (context, constraints) {
        final width = (columns * 145.0).clamp(constraints.maxWidth, 1800.0);
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (width > constraints.maxWidth)
              const Padding(
                padding: EdgeInsets.only(bottom: 8),
                child: Text(
                  'Vuốt ngang để xem đầy đủ bảng',
                  style: TextStyle(fontSize: 11, color: FlowColors.secondary),
                ),
              ),
            SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: SizedBox(
                width: width,
                child: Table(
                  border: TableBorder.all(
                    color: FlowColors.border,
                    borderRadius: BorderRadius.circular(6),
                  ),
                  defaultVerticalAlignment: TableCellVerticalAlignment.middle,
                  children: [
                    for (final row in rows)
                      TableRow(
                        decoration: BoxDecoration(
                          color: row.any((c) => c.localName == 'th')
                              ? FlowColors.soft
                              : FlowColors.cream,
                        ),
                        children: List.generate(
                          columns,
                          (index) => Padding(
                            padding: const EdgeInsets.all(11),
                            child: SelectableText(
                              index < row.length ? row[index].text : '',
                              style: TextStyle(
                                fontSize: 14,
                                height: 1.5,
                                fontWeight:
                                    index < row.length &&
                                        row[index].localName == 'th'
                                    ? FontWeight.w700
                                    : FontWeight.w400,
                              ),
                            ),
                          ),
                        ),
                      ),
                  ],
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}
