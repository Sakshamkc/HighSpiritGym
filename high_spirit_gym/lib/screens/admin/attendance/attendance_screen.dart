import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/models/attendance.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';

class AttendanceScreen extends StatefulWidget {
  const AttendanceScreen({super.key});

  @override
  State<AttendanceScreen> createState() => _AttendanceScreenState();
}

class _AttendanceScreenState extends State<AttendanceScreen> {
  List<Attendance> _todayList = [];
  int _todayCount = 0;
  int _monthCount = 0;
  int _currentlyIn = 0;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() => _isLoading = true);
    try {
      final auth = context.read<AuthProvider>();
      final results = await Future.wait([
        auth.api.get('/attendance/today'),
        auth.api.get('/attendance/stats'),
      ]);

      final todayResp = results[0];
      final statsResp = results[1];

      final todayData = todayResp['data'] as List? ?? [];
      final statsData = statsResp['data'] ?? statsResp;
      setState(() {
        _todayList = todayData.map((e) => Attendance.fromJson(e)).toList();
        _todayCount = statsData['todayCheckIns'] ?? statsData['TodayCheckIns'] ?? 0;
        _monthCount = statsData['monthCheckIns'] ?? statsData['MonthCheckIns'] ?? 0;
        _currentlyIn = statsData['currentlyInGym'] ?? statsData['CurrentlyInGym'] ?? 0;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Attendance')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _loadData,
              child: SingleChildScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _buildStatsRow(),
                    const SizedBox(height: 20),
                    const Text("Today's Attendance",
                        style: TextStyle(
                            fontSize: 18, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 12),
                    _todayList.isEmpty
                        ? Container(
                            width: double.infinity,
                            padding: const EdgeInsets.all(40),
                            decoration: BoxDecoration(
                              color: Colors.grey[100],
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Column(
                              children: [
                                Icon(Icons.event_busy,
                                    size: 48, color: Colors.grey[400]),
                                const SizedBox(height: 8),
                                Text('No check-ins today',
                                    style: TextStyle(color: Colors.grey[600])),
                              ],
                            ),
                          )
                        : ListView.builder(
                            shrinkWrap: true,
                            physics: const NeverScrollableScrollPhysics(),
                            itemCount: _todayList.length,
                            itemBuilder: (ctx, index) {
                              final a = _todayList[index];
                              return _AttendanceCard(attendance: a);
                            },
                          ),
                  ],
                ),
              ),
            ),
    );
  }

  Widget _buildStatsRow() {
    return Row(
      children: [
        Expanded(
          child: _StatMini(
            label: 'Today',
            value: '$_todayCount',
            icon: Icons.today,
            color: AppTheme.primaryColor,
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _StatMini(
            label: 'This Month',
            value: '$_monthCount',
            icon: Icons.calendar_month,
            color: AppTheme.infoColor,
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _StatMini(
            label: 'In Gym',
            value: '$_currentlyIn',
            icon: Icons.person_pin_circle,
            color: AppTheme.successColor,
          ),
        ),
      ],
    );
  }
}

class _StatMini extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color color;

  const _StatMini({
    required this.label,
    required this.value,
    required this.icon,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: color.withOpacity(0.08),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withOpacity(0.2)),
      ),
      child: Column(
        children: [
          Icon(icon, color: color, size: 24),
          const SizedBox(height: 6),
          Text(value,
              style: TextStyle(
                  fontSize: 22, fontWeight: FontWeight.bold, color: color)),
          const SizedBox(height: 2),
          Text(label,
              style: TextStyle(fontSize: 11, color: Colors.grey[600])),
        ],
      ),
    );
  }
}

class _AttendanceCard extends StatelessWidget {
  final Attendance attendance;

  const _AttendanceCard({required this.attendance});

  @override
  Widget build(BuildContext context) {
    final a = attendance;
    final isIn = a.isCheckedIn;

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor:
              isIn ? AppTheme.successColor.withOpacity(0.1) : Colors.grey[100],
          child: Icon(
            isIn ? Icons.login : Icons.logout,
            color: isIn ? AppTheme.successColor : Colors.grey,
            size: 20,
          ),
        ),
        title: Text(
          a.customerName,
          style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
        ),
        subtitle: Text(
          'In: ${_fmt(a.checkInTime)}${a.checkOutTime != null ? ' • Out: ${_fmt(a.checkOutTime!)}' : ' • Still in gym'}',
          style: TextStyle(fontSize: 12, color: Colors.grey[600]),
        ),
        trailing: isIn
            ? Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: AppTheme.successColor.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Text('IN',
                    style: TextStyle(
                        color: AppTheme.successColor,
                        fontWeight: FontWeight.bold,
                        fontSize: 11)),
              )
            : Text(a.durationText,
                style: TextStyle(fontSize: 12, color: Colors.grey[600])),
      ),
    );
  }

  String _fmt(DateTime dt) {
    final h = dt.hour.toString().padLeft(2, '0');
    final m = dt.minute.toString().padLeft(2, '0');
    return '$h:$m';
  }
}
