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

  final _titles = const [
    'Dashboard',
    'Members',
    'Boxing',
    'Lockers',
    'Reports',
  ];

  @override
  Widget build(BuildContext context) {
    final theme = context.watch<ThemeProvider>();
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: const Text('High Spirit Gym'),
        elevation: 0,
        actions: [
          IconButton(
            icon: Icon(theme.isDark ? Icons.light_mode : Icons.dark_mode),
            onPressed: theme.toggleTheme,
            tooltip: theme.isDark ? 'Light mode' : 'Dark mode',
          ),
        ],
      ),
      drawer: _buildDrawer(isDark),
      body: IndexedStack(
        index: _currentIndex,
        children: _screens,
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (i) => setState(() => _currentIndex = i),
        height: 65,
        animationDuration: const Duration(milliseconds: 400),
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

  Widget _buildDrawer(bool isDark) {
    final auth = context.read<AuthProvider>();
    final user = auth.user;
    final role = user?.role ?? 'User';
    final isAdmin = role == 'Admin';
    final roleLabel = isAdmin ? 'Super Admin' : role;
    final username = user?.username ?? 'User';
    final initial = username.isNotEmpty ? username[0].toUpperCase() : 'U';

    return Drawer(
      child: Column(
        children: [
          // Header
          Container(
            width: double.infinity,
            padding: EdgeInsets.only(
              top: MediaQuery.of(context).padding.top + 20,
              bottom: 20,
              left: 20,
              right: 20,
            ),
            decoration: const BoxDecoration(
              gradient: AppTheme.primaryGradient,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Avatar with role badge
                Stack(
                  children: [
                    CircleAvatar(
                      radius: 32,
                      backgroundColor: Colors.white.withOpacity(0.2),
                      child: Text(
                        initial,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 26,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    Positioned(
                      bottom: 0,
                      right: 0,
                      child: Container(
                        padding: const EdgeInsets.all(3),
                        decoration: BoxDecoration(
                          color: isAdmin
                              ? const Color(0xFFFFD700)
                              : AppTheme.successColor,
                          shape: BoxShape.circle,
                          border: Border.all(
                              color: Colors.white.withOpacity(0.3), width: 2),
                        ),
                        child: Icon(
                          isAdmin
                              ? Icons.shield
                              : Icons.person,
                          color: isAdmin ? Colors.black87 : Colors.white,
                          size: 12,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 14),
                Text(
                  username,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 18,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 4),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
                  decoration: BoxDecoration(
                    color: isAdmin
                        ? const Color(0xFFFFD700).withOpacity(0.3)
                        : Colors.white.withOpacity(0.2),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(
                      color: isAdmin
                          ? const Color(0xFFFFD700).withOpacity(0.5)
                          : Colors.white.withOpacity(0.3),
                    ),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        isAdmin ? Icons.admin_panel_settings : Icons.person,
                        color: isAdmin
                            ? const Color(0xFFFFD700)
                            : Colors.white70,
                        size: 14,
                      ),
                      const SizedBox(width: 4),
                      Text(
                        roleLabel,
                        style: TextStyle(
                          color: isAdmin
                              ? const Color(0xFFFFD700)
                              : Colors.white70,
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
                if (user?.email != null && user!.email!.isNotEmpty) ...[
                  const SizedBox(height: 6),
                  Text(
                    user.email!,
                    style: TextStyle(
                        color: Colors.white.withOpacity(0.7), fontSize: 12),
                  ),
                ],
              ],
            ),
          ),

          // Menu
          Expanded(
            child: ListView(
              padding: const EdgeInsets.symmetric(vertical: 8),
              children: [
                _buildSection('MAIN'),
                _drawerItem(Icons.dashboard, 'Dashboard', 0,
                    AppTheme.primaryColor),
                _drawerItem(Icons.people, 'Members', 1,
                    AppTheme.successColor),
                _drawerItem(Icons.sports_mma, 'Boxing', 2,
                    AppTheme.warningColor),
                _drawerItem(Icons.lock, 'Lockers', 3,
                    AppTheme.infoColor),
                _drawerItem(Icons.bar_chart, 'Reports', 4,
                    AppTheme.secondaryColor),

                const Padding(
                  padding: EdgeInsets.symmetric(horizontal: 16),
                  child: Divider(height: 24),
                ),

                _buildSection('TOOLS'),
                _drawerNavItem(Icons.qr_code_scanner, 'QR Scanner',
                    const QrScannerScreen(), AppTheme.infoColor),
                _drawerNavItem(Icons.fact_check, 'Attendance',
                    const AttendanceScreen(), AppTheme.successColor),
                _drawerNavItem(Icons.calendar_month, 'Schedule',
                    const ScheduleManageScreen(), AppTheme.warningColor),
              ],
            ),
          ),

          // Footer
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              border: Border(
                top: BorderSide(
                  color: isDark ? Colors.grey[800]! : Colors.grey[200]!,
                ),
              ),
            ),
            child: InkWell(
              onTap: () async {
                await auth.logout();
                if (mounted) Navigator.pushReplacementNamed(context, '/login');
              },
              borderRadius: BorderRadius.circular(12),
              child: Container(
                padding: const EdgeInsets.symmetric(vertical: 12),
                decoration: BoxDecoration(
                  color: AppTheme.dangerColor.withOpacity(0.08),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(
                      color: AppTheme.dangerColor.withOpacity(0.15)),
                ),
                child: const Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.logout, color: AppTheme.dangerColor, size: 18),
                    SizedBox(width: 8),
                    Text('Logout',
                        style: TextStyle(
                            color: AppTheme.dangerColor,
                            fontWeight: FontWeight.w600)),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSection(String title) {
    return Padding(
      padding: const EdgeInsets.only(left: 20, top: 8, bottom: 4),
      child: Text(
        title,
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.w700,
          color: Colors.grey[400],
          letterSpacing: 1.2,
        ),
      ),
    );
  }

  Widget _drawerItem(
      IconData icon, String title, int index, Color color) {
    final isSelected = _currentIndex == index;
    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 10, vertical: 2),
      decoration: BoxDecoration(
        color: isSelected
            ? color.withOpacity(0.1)
            : Colors.transparent,
        borderRadius: BorderRadius.circular(12),
      ),
      child: ListTile(
        leading: Icon(icon,
            color: isSelected ? color : Colors.grey[500],
            size: 22),
        title: Text(title,
            style: TextStyle(
              fontWeight: isSelected ? FontWeight.w600 : FontWeight.w500,
              color: isSelected ? color : null,
              fontSize: 14,
            )),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        onTap: () {
          setState(() => _currentIndex = index);
          Navigator.pop(context);
        },
      ),
    );
  }

  Widget _drawerNavItem(
      IconData icon, String title, Widget screen, Color color) {
    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 10, vertical: 2),
      child: ListTile(
        leading: Icon(icon, color: Colors.grey[500], size: 22),
        title: Text(title,
            style: const TextStyle(fontWeight: FontWeight.w500, fontSize: 14)),
        trailing: Icon(Icons.arrow_forward_ios, size: 14, color: Colors.grey[400]),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        onTap: () {
          Navigator.pop(context);
          Navigator.push(
              context, MaterialPageRoute(builder: (_) => screen));
        },
      ),
    );
  }
}
