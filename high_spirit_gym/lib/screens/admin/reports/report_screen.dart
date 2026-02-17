import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/models/report.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';
import 'package:high_spirit_gym/widgets/stat_card.dart';

class ReportScreen extends StatefulWidget {
  const ReportScreen({super.key});

  @override
  State<ReportScreen> createState() => _ReportScreenState();
}

class _ReportScreenState extends State<ReportScreen> {
  RevenueReport? _report;
  bool _isLoading = true;
  String _error = '';

  @override
  void initState() {
    super.initState();
    _loadReport();
  }

  Future<void> _loadReport() async {
    setState(() {
      _isLoading = true;
      _error = '';
    });
    try {
      final auth = context.read<AuthProvider>();
      final resp = await auth.api.get('/report/revenue');
      setState(() {
        _report = RevenueReport.fromJson(resp);
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Reports')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error.isNotEmpty
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(_error),
                      const SizedBox(height: 12),
                      ElevatedButton(
                          onPressed: _loadReport, child: const Text('Retry')),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _loadReport,
                  child: SingleChildScrollView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        _buildRevenueCards(),
                        const SizedBox(height: 20),
                        _buildMonthlyChart(),
                        const SizedBox(height: 20),
                        _buildCategoryBreakdown(),
                        const SizedBox(height: 20),
                        _buildRecentTransactions(),
                      ],
                    ),
                  ),
                ),
    );
  }

  Widget _buildRevenueCards() {
    final r = _report!;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('Revenue Overview',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: StatCard(
                title: 'Total Revenue',
                value: 'Rs. ${r.totalRevenue}',
                icon: Icons.account_balance_wallet,
                gradient: AppTheme.primaryGradient,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: StatCard(
                title: 'This Month',
                value: 'Rs. ${r.monthlyRevenue}',
                icon: Icons.calendar_month,
                gradient: AppTheme.successGradient,
              ),
            ),
          ],
        ),
        const SizedBox(height: 10),
        Row(
          children: [
            Expanded(
              child: StatCard(
                title: 'Total Due',
                value: 'Rs. ${r.totalDue}',
                icon: Icons.warning_amber,
                gradient: AppTheme.dangerGradient,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: StatCard(
                title: 'Cash',
                value: 'Rs. ${r.totalCash}',
                icon: Icons.payments,
                gradient: AppTheme.infoGradient,
              ),
            ),
          ],
        ),
        const SizedBox(height: 10),
        Row(
          children: [
            Expanded(
              child: StatCard(
                title: 'eSewa',
                value: 'Rs. ${r.totalEsewa}',
                icon: Icons.phone_android,
                gradient: AppTheme.successGradient,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: StatCard(
                title: 'Today',
                value: 'Rs. ${r.todayRevenue}',
                icon: Icons.today,
                gradient: AppTheme.warningGradient,
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildMonthlyChart() {
    final months = _report!.monthlyBreakdown;
    if (months.isEmpty) return const SizedBox.shrink();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Monthly Revenue',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            SizedBox(
              height: 220,
              child: BarChart(
                BarChartData(
                  alignment: BarChartAlignment.spaceAround,
                  maxY: months
                          .map((m) => m.totalRevenue)
                          .reduce((a, b) => a > b ? a : b) *
                      1.2,
                  barTouchData: BarTouchData(
                    enabled: true,
                    touchTooltipData: BarTouchTooltipData(
                      getTooltipItem: (group, gIdx, rod, rIdx) {
                        final m = months[group.x.toInt()];
                        return BarTooltipItem(
                          '${m.monthName}\nRs. ${m.totalRevenue.toStringAsFixed(0)}',
                          const TextStyle(
                              color: Colors.white,
                              fontWeight: FontWeight.bold,
                              fontSize: 12),
                        );
                      },
                    ),
                  ),
                  titlesData: FlTitlesData(
                    show: true,
                    bottomTitles: AxisTitles(
                      sideTitles: SideTitles(
                        showTitles: true,
                        reservedSize: 28,
                        getTitlesWidget: (value, meta) {
                          final idx = value.toInt();
                          if (idx < 0 || idx >= months.length) {
                            return const SizedBox();
                          }
                          final name = months[idx].monthName;
                          return SideTitleWidget(
                            meta: meta,
                            child: Text(
                              name.length > 3 ? name.substring(0, 3) : name,
                              style: const TextStyle(fontSize: 10),
                            ),
                          );
                        },
                      ),
                    ),
                    leftTitles: AxisTitles(
                      sideTitles: SideTitles(
                        showTitles: true,
                        reservedSize: 42,
                        getTitlesWidget: (value, meta) {
                          if (value == 0) return const SizedBox();
                          return Text(
                            '${(value / 1000).toStringAsFixed(0)}k',
                            style: const TextStyle(fontSize: 10),
                          );
                        },
                      ),
                    ),
                    topTitles: const AxisTitles(
                        sideTitles: SideTitles(showTitles: false)),
                    rightTitles: const AxisTitles(
                        sideTitles: SideTitles(showTitles: false)),
                  ),
                  borderData: FlBorderData(show: false),
                  barGroups: months.asMap().entries.map((entry) {
                    return BarChartGroupData(
                      x: entry.key,
                      barRods: [
                        BarChartRodData(
                          toY: entry.value.totalRevenue,
                          gradient: AppTheme.primaryGradient,
                          width: 16,
                          borderRadius:
                              const BorderRadius.vertical(top: Radius.circular(6)),
                        ),
                      ],
                    );
                  }).toList(),
                  gridData: const FlGridData(show: false),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildCategoryBreakdown() {
    final r = _report!;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Category Breakdown',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 12),
            _categoryRow('Gym Membership', r.gymRevenue, AppTheme.primaryColor),
            _categoryRow('Locker', r.lockerRevenue, AppTheme.infoColor),
            _categoryRow('Boxing', r.boxingRevenue, AppTheme.warningColor),
          ],
        ),
      ),
    );
  }

  Widget _categoryRow(String label, double amount, Color color) {
    final total = _report!.totalRevenue;
    final pct = total > 0 ? (amount / total) : 0.0;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Container(
                    width: 12,
                    height: 12,
                    decoration: BoxDecoration(
                        color: color, borderRadius: BorderRadius.circular(3)),
                  ),
                  const SizedBox(width: 8),
                  Text(label),
                ],
              ),
              Text('Rs. ${amount.toStringAsFixed(0)}',
                  style: const TextStyle(fontWeight: FontWeight.w600)),
            ],
          ),
          const SizedBox(height: 4),
          ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: pct,
              backgroundColor: color.withOpacity(0.1),
              valueColor: AlwaysStoppedAnimation(color),
              minHeight: 6,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildRecentTransactions() {
    final transactions = _report!.recentTransactions;
    if (transactions.isEmpty) return const SizedBox.shrink();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Recent Transactions',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 12),
            ...transactions.map((t) => ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: CircleAvatar(
                    backgroundColor: t.type == 'Gym'
                        ? AppTheme.primaryColor.withOpacity(0.1)
                        : t.type == 'Locker'
                            ? AppTheme.infoColor.withOpacity(0.1)
                            : AppTheme.warningColor.withOpacity(0.1),
                    child: Icon(
                      t.type == 'Gym'
                          ? Icons.fitness_center
                          : t.type == 'Locker'
                              ? Icons.lock
                              : Icons.sports_mma,
                      color: t.type == 'Gym'
                          ? AppTheme.primaryColor
                          : t.type == 'Locker'
                              ? AppTheme.infoColor
                              : AppTheme.warningColor,
                      size: 20,
                    ),
                  ),
                  title: Text(t.customerName,
                      style: const TextStyle(
                          fontWeight: FontWeight.w500, fontSize: 14)),
                  subtitle: Text(
                    '${t.type} • ${t.date?.toString().substring(0, 10) ?? ''}',
                    style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                  ),
                  trailing: Text('Rs. ${t.amount.toStringAsFixed(0)}',
                      style: const TextStyle(
                          fontWeight: FontWeight.bold, fontSize: 14)),
                )),
          ],
        ),
      ),
    );
  }
}
