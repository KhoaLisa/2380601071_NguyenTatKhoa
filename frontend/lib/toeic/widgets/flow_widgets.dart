import 'package:flutter/material.dart';
import '../theme/toeic_theme.dart';

class FlowBrand extends StatelessWidget {
  const FlowBrand({super.key, this.subtitle});
  final String? subtitle;

  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      const Icon(Icons.eco_rounded, color: FlowColors.forest),
      const SizedBox(width: 7),
      Flexible(
        child: Text(
          'TOEICFlow',
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontSize: 19, fontWeight: FontWeight.w800),
        ),
      ),
      if (subtitle != null) ...[
        const SizedBox(width: 10),
        const Text('|', style: TextStyle(color: FlowColors.sage)),
        const SizedBox(width: 10),
        Flexible(
          child: Text(
            subtitle!,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 13, color: FlowColors.secondary),
          ),
        ),
      ],
    ],
  );
}

class FlowMessage extends StatelessWidget {
  const FlowMessage({
    super.key,
    required this.icon,
    required this.title,
    required this.detail,
    this.onRetry,
  });
  final IconData icon;
  final String title;
  final String detail;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 36),
    child: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          padding: const EdgeInsets.all(20),
          decoration: const BoxDecoration(
            color: FlowColors.soft,
            shape: BoxShape.circle,
          ),
          child: Icon(icon, size: 36, color: FlowColors.forest),
        ),
        const SizedBox(height: 18),
        Text(
          title,
          textAlign: TextAlign.center,
          style: const TextStyle(fontSize: 19, fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 8),
        Text(
          detail,
          textAlign: TextAlign.center,
          style: const TextStyle(color: FlowColors.secondary, height: 1.5),
        ),
        if (onRetry != null) ...[
          const SizedBox(height: 20),
          OutlinedButton.icon(
            onPressed: onRetry,
            icon: const Icon(Icons.refresh_rounded),
            label: const Text('Thử lại'),
          ),
        ],
      ],
    ),
  );
}

class FlowPill extends StatelessWidget {
  const FlowPill({
    super.key,
    required this.label,
    this.color = FlowColors.soft,
  });
  final String label;
  final Color color;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
    decoration: BoxDecoration(
      color: color,
      borderRadius: BorderRadius.circular(30),
    ),
    child: Text(
      label,
      style: const TextStyle(
        fontSize: 11,
        fontWeight: FontWeight.w600,
        color: FlowColors.forest,
      ),
    ),
  );
}

class FlowHero extends StatelessWidget {
  const FlowHero({super.key, required this.title, required this.description});
  final String title;
  final String description;
  @override
  Widget build(BuildContext context) => ClipRRect(
    borderRadius: BorderRadius.circular(22),
    child: ColoredBox(
      color: FlowColors.soft,
      child: Stack(
        children: [
          Positioned(
            right: -36,
            top: -28,
            child: Transform.rotate(
              angle: -.3,
              child: Icon(
                Icons.eco_rounded,
                size: 200,
                color: FlowColors.forest.withValues(alpha: .08),
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(23),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const FlowPill(
                  label: 'READING • PART 5, 6, 7',
                  color: FlowColors.cream,
                ),
                const SizedBox(height: 17),
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 25,
                    height: 1.3,
                    fontWeight: FontWeight.w800,
                    color: FlowColors.deep,
                  ),
                ),
                const SizedBox(height: 10),
                Text(
                  description,
                  style: const TextStyle(
                    height: 1.6,
                    color: FlowColors.secondary,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    ),
  );
}
