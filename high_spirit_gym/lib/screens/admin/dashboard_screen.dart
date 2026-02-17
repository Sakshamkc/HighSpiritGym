import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/models/dashboard_stats.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';
import 'package:high_spirit_gym/widgets/stat_card.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  DashboardStats? _stats;
  Map<String, dynamic>? _revenue;
  List<dynamic>? _monthlyData;
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _loadDashboard();
  }

  Future<void> _loadDashboard() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });
    try {
      final auth = context.read<AuthProvider>();
      final dashResp = await auth.api.get('/dashboard');
      final revenueResp = await auth.api.get('/report/revenue');
      final monthlyResp =
          await auth.api.get('/report/monthly', query: {'year': DateTime.now().year.toString()});

      setState(() {
        _stats = DashboardStats.fromJson(dashResp['data']);
        _revenue = revenueResp['data'];
        _monthlyData = monthlyResp['data']?['months'] as List? ?? [];
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _isLoading = false;
        _errorMessage = e.toString();
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_stats == null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 64, color: Colors.grey),
            const SizedBox(height: 16),
            Text('Failed to load dashboard: ${_errorMessage ?? "Unknown error"}', textAlign: TextAlign.center),
            const SizedBox(height: 16),
            ElevatedButton(onPressed: _loadDashboard, child: const Text('Retry')),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadDashboard,
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Revenue Overview
            _buildRevenueCards(),
            const SizedBox(height: 20),

            // Gym Stats
            const Text('Gym Members',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
            const SizedBox(height: 12),
            _buildGymStats(),
            const SizedBox(height: 20),

            // Monthly Revenue Chart
            const Text('Monthly Revenue',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
            const SizedBox(height: 12),
            _buildRevenueChart(),
            const SizedBox(height: 20),

            // Locker Stats
            const Text('Lockers',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
            const SizedBox(height: 12),
            _buildLockerStats(),
            const SizedBox(height: 20),

            // Boxing Stats
            const Text('Boxing',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
            const SizedBox(height: 12),
            _buildBoxingStats(),
            const SizedBox(height: 20),
          ],
        ),
      ),
    );
  }

  String _fmtAmt(dynamic val) {
    final n = (val is num) ? val.toDouble() : 0.0;
    if (n >= 100000) return '${(n / 1000).toStringAsFixed(1)}k';
    return n.toStringAsFixed(0);
  }

  Widget _buildRevenueCards() {
    final todayRev = _revenue?['todayRevenue'] ?? 0;
    final thisMonth = _revenue?['thisMonthRevenue'] ?? 0;
    final totalDue = _revenue?['totalDue'] ?? 0;
    final growth = (_revenue?['revenueGrowth'] as num?)?.toDouble() ?? 0;

    return Column(
      children: [
        // Main revenue card
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            gradient: AppTheme.primaryGradient,
            borderRadius: BorderRadius.circular(20),
            boxShadow: [
              BoxShadow(
                color: AppTheme.primaryColor.withOpacity(0.3),
                blurRadius: 12,
                offset: const Offset(0, 6),
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('Total Revenue', style: TextStyle(color: Colors.white.withOpacity(0.8), fontSize: 14)),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: Colors.white.withOpacity(0.2),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(growth >= 0 ? Icons.trending_up : Icons.trending_down, color: Colors.white, size: 14),
                        const SizedBox(width: 4),
                        Text('${growth.toStringAsFixed(1)}%', style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.w600)),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Text(
                'Rs. ${_fmtAmt(_revenue?['totalRevenue'] ?? 0)}',
                style: const TextStyle(color: Colors.white, fontSize: 32, fontWeight: FontWeight.bold, letterSpacing: 1),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: StatCard(
                title: 'Today',
                value: 'Rs. ${_fmtAmt(todayRev)}',
                icon: Icons.today,
                gradient: AppTheme.primaryGradient,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: StatCard(
                title: 'This Month',
                value: 'Rs. ${_fmtAmt(thisMonth)}',
                icon: Icons.calendar_month,
                gradient: AppTheme.successGradient,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: StatCard(
                title: 'Total Due',
                value: 'Rs. ${_fmtAmt(totalDue)}',
                icon: Icons.warning_amber,
                gradient: AppTheme.dangerGradient,
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildGymStats() {
    final s = _stats!;
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 12,
      crossAxisSpacing: 12,
      childAspectRatio: 1.6,
      children: [
        StatCard(
          title: 'Total Members',
          value: '${s.gymTotal}',
          icon: Icons.people,
          gradient: AppTheme.primaryGradient,
        ),
        StatCard(
          title: 'Active',
          value: '${s.gymActive}',
          icon: Icons.check_circle,
          gradient: AppTheme.successGradient,
        ),
        StatCard(
          title: 'Expired',
          value: '${s.gymExpired}',
          icon: Icons.cancel,
          gradient: AppTheme.dangerGradient,
        ),
        StatCard(
          title: 'Expiring Soon',
          value: '${s.gymExpiringSoon}',
          icon: Icons.access_time,
          gradient: AppTheme.warningGradient,
        ),
      ],
    );
  }

  Widget _buildRevenueChart() {
    if (_monthlyData == null || _monthlyData!.isEmpty) {
      return const Card(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Center(child: Text('No revenue data')),
        ),
      );
    }

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: SizedBox(
          height: 220,
          child: BarChart(
            BarChartData(
              alignment: BarChartAlignment.spaceAround,
              maxY: _getMaxRevenue(),
              barTouchData: BarTouchData(
                touchTooltipData: BarTouchTooltipData(
                  getTooltipItem: (group, groupIndex, rod, rodIndex) {
                    return BarTooltipItem(
                      'Rs. ${rod.toY.toInt()}',
                      const TextStyle(
                          color: Colors.white, fontWeight: FontWeight.bold),
                    );
                  },
                ),
              ),
              titlesData: FlTitlesData(
                bottomTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    getTitlesWidget: (value, meta) {
                      final months = ['J', 'F', 'M', 'A', 'M', 'J', 'J', 'A', 'S', 'O', 'N', 'D'];
                      if (value.toInt() < months.length) {
                        return Text(months[value.toInt()],
                            style: const TextStyle(fontSize: 10));
                      }
                      return const Text('');
                    },
                  ),
                ),
                leftTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    reservedSize: 50,
                    getTitlesWidget: (value, meta) {
                      if (value == 0) return const Text('');
                      return Text('${(value / 1000).toStringAsFixed(0)}k',
                          style: const TextStyle(fontSize: 10));
                    },
                  ),
                ),
                topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                rightTitles:
                    const AxisTitles(sideTitles: SideTitles(showTitles: false)),
              ),
              borderData: FlBorderData(show: false),
              gridData: FlGridData(
                show: true,
                drawVerticalLine: false,
                horizontalInterval: _getMaxRevenue() / 4,
              ),
              barGroups: _monthlyData!.asMap().entries.map((e) {
                final d = e.value;
                final total = (d['total'] as num?)?.toDouble() ?? 0;
                return BarChartGroupData(
                  x: e.key,
                  barRods: [
                    BarChartRodData(
                      toY: total,
                      gradient: AppTheme.primaryGradient,
                      width: 14,
                      borderRadius: const BorderRadius.only(
                        topLeft: Radius.circular(4),
                        topRight: Radius.circular(4),
                      ),
                    ),
                  ],
                );
              }).toList(),
            ),
          ),
        ),
      ),
    );
  }

  double _getMaxRevenue() {
    if (_monthlyData == null || _monthlyData!.isEmpty) return 100000;
    double max = 0;
    for (var d in _monthlyData!) {
      final total = (d['total'] as num?)?.toDouble() ?? 0;
      if (total > max) max = total;
    }
    return max * 1.2;
  }

  Widget _buildLockerStats() {
    final s = _stats!;
    return Row(
      children: [
        Expanded(
          child: _miniStatCard('Gents', '${s.lockerGentsOccupied}/${s.lockerGentsTotal}',
              Icons.male, AppTheme.infoGradient),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: _miniStatCard('Ladies', '${s.lockerLadiesOccupied}/${s.lockerLadiesTotal}',
              Icons.female, AppTheme.purpleGradient),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: _miniStatCard('Due', 'Rs.${s.lockerTotalDue}', Icons.warning_amber,
              AppTheme.dangerGradient),
        ),
      ],
    );
  }

  Widget _buildBoxingStats() {
    final s = _stats!;
    return Row(
      children: [
        Expanded(
          child: _miniStatCard(
              'Total', '${s.boxingTotal}', Icons.sports_mma, AppTheme.primaryGradient),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: _miniStatCard(
              'With Due', '${s.boxingWithDue}', Icons.money_off, AppTheme.warningGradient),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: _miniStatCard(
              'Due Amt', 'Rs.${s.boxingTotalDue}', Icons.warning, AppTheme.dangerGradient),
        ),
      ],
    );
  }

  Widget _miniStatCard(
      String title, String value, IconData icon, Gradient gradient) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        gradient: gradient,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        children: [
          Icon(icon, color: Colors.white, size: 20),
          const SizedBox(height: 6),
          Text(value,
              style: const TextStyle(
                  color: Colors.white,
                  fontSize: 16,
                  fontWeight: FontWeight.bold)),
          Text(title,
              style: TextStyle(
                  color: Colors.white.withOpacity(0.8), fontSize: 11)),
        ],
      ),
    );
  }
}
