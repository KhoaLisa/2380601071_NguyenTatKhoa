import 'package:flutter/material.dart';

abstract final class FlowColors {
  static const forest = Color(0xFF244D3C);
  static const deep = Color(0xFF193C30);
  static const sage = Color(0xFF8FAF96);
  static const soft = Color(0xFFD9E7DB);
  static const ivory = Color(0xFFF7F5ED);
  static const cream = Color(0xFFFFFEFA);
  static const gold = Color(0xFFE3B76B);
  static const text = Color(0xFF20352C);
  static const secondary = Color(0xFF6C7C72);
  static const success = Color(0xFF368660);
  static const error = Color(0xFFC75D53);
  static const border = Color(0xFFE0E6DB);
}

ThemeData toeicFlowTheme() {
  final scheme = ColorScheme.fromSeed(
    seedColor: FlowColors.forest,
    primary: FlowColors.forest,
    secondary: FlowColors.sage,
    surface: FlowColors.cream,
    error: FlowColors.error,
    onSurface: FlowColors.text,
  );
  return ThemeData(
    useMaterial3: true,
    fontFamily: 'Inter',
    colorScheme: scheme,
    scaffoldBackgroundColor: FlowColors.ivory,
    appBarTheme: const AppBarTheme(
      backgroundColor: FlowColors.ivory,
      foregroundColor: FlowColors.forest,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      scrolledUnderElevation: 0,
    ),
    cardTheme: CardThemeData(
      color: FlowColors.cream,
      elevation: 0,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: const BorderSide(color: FlowColors.border),
      ),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        minimumSize: const Size(48, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(13)),
        textStyle: const TextStyle(fontWeight: FontWeight.w600),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(48, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(13)),
        side: const BorderSide(color: FlowColors.border),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: FlowColors.cream,
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(13)),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(13),
        borderSide: const BorderSide(color: FlowColors.border),
      ),
    ),
    navigationBarTheme: const NavigationBarThemeData(
      backgroundColor: FlowColors.cream,
      indicatorColor: FlowColors.soft,
      elevation: 0,
      labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
    ),
    dividerTheme: const DividerThemeData(color: FlowColors.border),
  );
}
