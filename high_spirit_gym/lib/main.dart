import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';
import 'package:high_spirit_gym/providers/theme_provider.dart';
import 'package:high_spirit_gym/screens/splash_screen.dart';
import 'package:high_spirit_gym/screens/login_screen.dart';
import 'package:high_spirit_gym/screens/admin/admin_home.dart';
import 'package:high_spirit_gym/screens/customer/customer_home.dart';
import 'package:high_spirit_gym/classes/http_override.dart';
import 'dart:io';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  HttpOverrides.global = MyHttpOverrides();
  runApp(const HighSpiritApp());
}

class HighSpiritApp extends StatelessWidget {
  const HighSpiritApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => ThemeProvider()),
        ChangeNotifierProvider(create: (_) => AuthProvider()),
      ],
      child: Consumer<ThemeProvider>(
        builder: (context, themeProvider, _) {
          return MaterialApp(
            title: 'High Spirit Gym',
            debugShowCheckedModeBanner: false,
            theme: AppTheme.lightTheme,
            darkTheme: AppTheme.darkTheme,
            themeMode: themeProvider.themeMode,
            home: const AuthWrapper(),
            routes: {
              '/login': (context) => const LoginScreen(),
              '/admin': (context) => const AdminHome(),
              '/customer': (context) => const CustomerHome(),
            },
          );
        },
      ),
    );
  }
}

class AuthWrapper extends StatelessWidget {
  const AuthWrapper({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<AuthProvider>(
      builder: (context, auth, _) {
        if (auth.isLoading) {
          return const SplashScreen();
        }
        if (!auth.isAuthenticated) {
          return const LoginScreen();
        }
        if (auth.isAdmin) {
          return const AdminHome();
        }
        return const CustomerHome();
      },
    );
  }
}
