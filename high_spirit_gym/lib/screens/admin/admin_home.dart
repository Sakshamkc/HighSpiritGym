import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';
import 'package:high_spirit_gym/providers/theme_provider.dart';
import 'package:high_spirit_gym/screens/admin/dashboard_screen.dart';
import 'package:high_spirit_gym/screens/admin/customers/customer_list_screen.dart';
import 'package:high_spirit_gym/screens/admin/boxing/boxing_list_screen.dart';
import 'package:high_spirit_gym/screens/admin/lockers/locker_list_screen.dart';
import 'package:high_spirit_gym/screens/admin/reports/report_screen.dart';
import 'package:high_spirit_gym/screens/admin/attendance/attendance_screen.dart';
import 'package:high_spirit_gym/screens/admin/schedule/schedule_manage_screen.dart';
import 'package:high_spirit_gym/screens/customer/qr_scanner_screen.dart';

class AdminHome extends StatefulWidget {
  const AdminHome({super.key});

  @override
  State<AdminHome> createState() => _AdminHomeState();
}

class _AdminHomeState extends State<AdminHome> {
  int _currentIndex = 0;

  final _screens = const [
    DashboardScreen(),
    CustomerListScreen(),
    BoxingListScreen(),
    LockerListScreen(),
    ReportScreen(),
  ];

  @override
  Widget build(BuildContext context) {
    final theme = context.watch<ThemeProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('High Spirit Gym'),
        actions: [
          IconButton(
            icon: Icon(theme.isDark ? Icons.light_mode : Icons.dark_mode),
            onPressed: theme.toggleTheme,
          ),
        ],
      ),
      drawer: _buildDrawer(),
      body: IndexedStack(
        index: _currentIndex,
        children: _screens,
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (i) => setState(() => _currentIndex = i),
        destinations: const [
          NavigationDestination(
              icon: Icon(Icons.dashboard_outlined),
              selectedIcon: Icon(Icons.dashboard),
              label: 'Dashboard'),
          NavigationDestination(
              icon: Icon(Icons.people_outline),
              selectedIcon: Icon(Icons.people),
              label: 'Members'),
          NavigationDestination(
              icon: Icon(Icons.sports_mma_outlined),
              selectedIcon: Icon(Icons.sports_mma),
              label: 'Boxing'),
          NavigationDestination(
              icon: Icon(Icons.lock_outline),
              selectedIcon: Icon(Icons.lock),
              label: 'Lockers'),
          NavigationDestination(
              icon: Icon(Icons.bar_chart_outlined),
              selectedIcon: Icon(Icons.bar_chart),
              label: 'Reports'),
        ],
      ),
    );
  }

  Widget _buildDrawer() {
    final auth = context.read<AuthProvider>();
    return Drawer(
      child: ListView(
        padding: EdgeInsets.zero,
        children: [
          DrawerHeader(
            decoration: const BoxDecoration(gradient: AppTheme.primaryGradient),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                const CircleAvatar(
                  radius: 30,
                  backgroundColor: Colors.white,
                  child:
                      Icon(Icons.admin_panel_settings, color: AppTheme.primaryColor, size: 30),
                ),
                const SizedBox(height: 10),
                Text(
                  auth.user?.username ?? 'Admin',
                  style: const TextStyle(
                      color: Colors.white,
                      fontSize: 18,
                      fontWeight: FontWeight.w600),
                ),
                Text(
                  'Super Admin',
                  style: TextStyle(
                      color: Colors.white.withOpacity(0.8), fontSize: 13),
                ),
              ],
            ),
          ),
          _drawerItem(Icons.dashboard, 'Dashboard', () {
            setState(() => _currentIndex = 0);
            Navigator.pop(context);
          }),
          _drawerItem(Icons.people, 'Members', () {
            setState(() => _currentIndex = 1);
            Navigator.pop(context);
          }),
          _drawerItem(Icons.sports_mma, 'Boxing', () {
            setState(() => _currentIndex = 2);
            Navigator.pop(context);
          }),
          _drawerItem(Icons.lock, 'Lockers', () {
            setState(() => _currentIndex = 3);
            Navigator.pop(context);
          }),
          _drawerItem(Icons.bar_chart, 'Reports', () {
            setState(() => _currentIndex = 4);
            Navigator.pop(context);
          }),
          const Divider(),
          _drawerItem(Icons.qr_code_scanner, 'QR Scanner', () {
            Navigator.pop(context);
            Navigator.push(
                context, MaterialPageRoute(builder: (_) => const QrScannerScreen()));
          }),
          _drawerItem(Icons.fact_check, 'Attendance', () {
            Navigator.pop(context);
            Navigator.push(
                context, MaterialPageRoute(builder: (_) => const AttendanceScreen()));
          }),
          _drawerItem(Icons.calendar_month, 'Schedule', () {
            Navigator.pop(context);
            Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => const ScheduleManageScreen()));
          }),
          const Divider(),
          _drawerItem(Icons.logout, 'Logout', () async {
            await auth.logout();
            if (mounted) Navigator.pushReplacementNamed(context, '/login');
          }),
        ],
      ),
    );
  }

  Widget _drawerItem(IconData icon, String title, VoidCallback onTap) {
    return ListTile(
      leading: Icon(icon),
      title: Text(title),
      onTap: onTap,
    );
  }
}
