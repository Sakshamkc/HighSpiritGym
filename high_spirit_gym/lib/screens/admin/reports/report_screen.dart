import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/models/report.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';

class ReportScreen extends StatefulWidget {
  const ReportScreen({super.key});

  @override
  State<ReportScreen> createState() => _ReportScreenState();
}

class _ReportScreenState extends State<ReportScreen> {
  RevenueReport? _report;
  List<dynamic>? _monthlyData;
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
      final monthlyResp = await auth.api.get('/report/monthly',
          query: {'year': DateTime.now().year.toString()});
      setState(() {
        _report = RevenueReport.fromJson(resp['data'] ?? resp);
        _monthlyData = monthlyResp['data']?['months'] as List? ?? [];
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  String _formatAmount(dynamic amount) {
    final val = (amount is num) ? amount.toDouble() : 0.0;
    if (val >= 100000) {
      return '${(val / 1000).toStringAsFixed(1)}k';
    }
    return val.toStringAsFixed(0);
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error.isNotEmpty
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.error_outline, size: 64,
                            color: Colors.grey[400]),
                        const SizedBox(height: 16),
                        Text(_error, textAlign: TextAlign.center),
                        const SizedBox(height: 16),
                        ElevatedButton.icon(
                          onPressed: _loadReport,
                          icon: const Icon(Icons.refresh),
                          label: const Text('Retry'),
                        ),
                      ],
                    ),
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
                        // Header
                        Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(10),
                              decoration: BoxDecoration(
                                gradient: AppTheme.primaryGradient,
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: const Icon(Icons.analytics,
                                  color: Colors.white, size: 24),
                            ),
                            const SizedBox(width: 12),
                            const Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text('Revenue Report',
                                      style: TextStyle(
                                          fontSize: 20,
                                          fontWeight: FontWeight.bold)),
                                  Text('Financial overview & analytics',
                                      style: TextStyle(
                                          color: Colors.grey, fontSize: 13)),
                                ],
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 20),

                        // Revenue summary cards
                        _buildRevenueSummary(isDark),
                        const SizedBox(height: 20),

                        // Monthly chart
                        _buildMonthlyChart(isDark),
                        const SizedBox(height: 20),

                        // Category breakdown
                        _buildCategoryBreakdown(isDark),
                        const SizedBox(height: 20),

                        // Quick stats
                        _buildQuickStats(isDark),
                        const SizedBox(height: 20),
                      ],
                    ),
                  ),
                ),
    );
  }

  Widget _buildRevenueSummary(bool isDark) {
    final r = _report!;
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
                  Text('Total Revenue',
                      style: TextStyle(
                          color: Colors.white.withOpacity(0.8), fontSize: 14)),
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: Colors.white.withOpacity(0.2),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          r.revenueGrowth >= 0
                              ? Icons.trending_up
                              : Icons.trending_down,
                          color: Colors.white,
                          size: 14,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          '${r.revenueGrowth.toStringAsFixed(1)}%',
                          style: const TextStyle(
                              color: Colors.white,
                              fontSize: 12,
                              fontWeight: FontWeight.w600),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Text(
                'Rs. ${r.totalRevenue.toStringAsFixed(0)}',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 32,
                  fontWeight: FontWeight.bold,
                  letterSpacing: 1,
                ),
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  _miniInfo('Collected', 'Rs. ${_formatAmount(r.totalCollected)}',
                      Icons.check_circle_outline),
                  const SizedBox(width: 24),
                  _miniInfo('Due', 'Rs. ${_formatAmount(r.totalDue)}',
                      Icons.pending_outlined),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),

        // Stat row
        Row(
          children: [
            Expanded(
              child: _statTile(
                'Today',
                'Rs. ${_formatAmount(r.todayRevenue)}',
                Icons.today,
                AppTheme.successGradient,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: _statTile(
                'This Month',
                'Rs. ${_formatAmount(r.thisMonthRevenue)}',
                Icons.calendar_month,
                AppTheme.infoGradient,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: _statTile(
                'Last Month',
                'Rs. ${_formatAmount(r.lastMonthRevenue)}',
                Icons.history,
                AppTheme.warningGradient,
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _miniInfo(String label, String value, IconData icon) {
    return Row(
      children: [
        Icon(icon, color: Colors.white70, size: 16),
        const SizedBox(width: 6),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label,
                style: TextStyle(
                    color: Colors.white.withOpacity(0.7), fontSize: 11)),
            Text(value,
                style: const TextStyle(
                    color: Colors.white,
                    fontSize: 14,
                    fontWeight: FontWeight.w600)),
          ],
        ),
      ],
    );
  }

  Widget _statTile(
      String title, String value, IconData icon, Gradient gradient) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        gradient: gradient,
        borderRadius: BorderRadius.circular(14),
        boxShadow: [
          BoxShadow(
            color: (gradient as LinearGradient).colors.first.withOpacity(0.25),
            blurRadius: 8,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: Colors.white, size: 18),
          const SizedBox(height: 8),
          Text(value,
              style: const TextStyle(
                  color: Colors.white,
                  fontSize: 15,
                  fontWeight: FontWeight.bold)),
          const SizedBox(height: 2),
          Text(title,
              style: TextStyle(
                  color: Colors.white.withOpacity(0.8), fontSize: 11)),
        ],
      ),
    );
  }

  Widget _buildMonthlyChart(bool isDark) {
    if (_monthlyData == null || _monthlyData!.isEmpty) {
      return const SizedBox.shrink();
    }

    double maxVal = 0;
    for (var d in _monthlyData!) {
      final total = (d['total'] as num?)?.toDouble() ?? 0;
      if (total > maxVal) maxVal = total;
    }
    if (maxVal == 0) maxVal = 100000;

    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: isDark ? Colors.grey[900] : Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(isDark ? 0.3 : 0.06),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text('Monthly Revenue',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color:
                      AppTheme.primaryColor.withOpacity(isDark ? 0.2 : 0.08),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  '${DateTime.now().year}',
                  style: TextStyle(
                    color: AppTheme.primaryColor,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),
          SizedBox(
            height: 220,
            child: BarChart(
              BarChartData(
                alignment: BarChartAlignment.spaceAround,
                maxY: maxVal * 1.2,
                barTouchData: BarTouchData(
                  enabled: true,
                  touchTooltipData: BarTouchTooltipData(
                    getTooltipItem: (group, gIdx, rod, rIdx) {
                      final idx = group.x.toInt();
                      if (idx < 0 || idx >= _monthlyData!.length) return null;
                      final d = _monthlyData![idx];
                      return BarTooltipItem(
                        '${d['monthName']}\nRs. ${(d['total'] as num?)?.toStringAsFixed(0) ?? '0'}',
                        const TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.bold,
                            fontSize: 12),
                      );
                    },
                  ),
                ),
                titlesData: FlTitlesData(
                  bottomTitles: AxisTitles(
                    sideTitles: SideTitles(
                      showTitles: true,
                      reservedSize: 28,
                      getTitlesWidget: (value, meta) {
                        final idx = value.toInt();
                        if (idx < 0 || idx >= _monthlyData!.length) {
                          return const SizedBox();
                        }
                        final name = _monthlyData![idx]['monthName'] ?? '';
                        return SideTitleWidget(
                          axisSide: meta.axisSide,
                          child: Text(
                            name.toString().length > 3
                                ? name.toString().substring(0, 3)
                                : name.toString(),
                            style: TextStyle(
                                fontSize: 10,
                                color: isDark ? Colors.grey[400] : Colors.grey[600]),
                          ),
                        );
                      },
                    ),
                  ),
                  leftTitles: AxisTitles(
                    sideTitles: SideTitles(
                      showTitles: true,
                      reservedSize: 50,
                      getTitlesWidget: (value, meta) {
                        if (value == 0) return const SizedBox();
                        return Text(
                          '${(value / 1000).toStringAsFixed(0)}k',
                          style: TextStyle(
                              fontSize: 10,
                              color: isDark ? Colors.grey[500] : Colors.grey[400]),
                        );
                      },
                    ),
                  ),
                  topTitles:
                      const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                  rightTitles:
                      const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                ),
                borderData: FlBorderData(show: false),
                gridData: FlGridData(
                  show: true,
                  drawVerticalLine: false,
                  horizontalInterval: maxVal > 0 ? maxVal * 1.2 / 4 : 25000,
                  getDrawingHorizontalLine: (value) => FlLine(
                    color: isDark
                        ? Colors.grey[800]!
                        : Colors.grey[200]!,
                    strokeWidth: 1,
                  ),
                ),
                barGroups: _monthlyData!.asMap().entries.map((e) {
                  final d = e.value;
                  final total = (d['total'] as num?)?.toDouble() ?? 0;
                  return BarChartGroupData(
                    x: e.key,
                    barRods: [
                      BarChartRodData(
                        toY: total,
                        gradient: total > 0
                            ? AppTheme.primaryGradient
                            : const LinearGradient(
                                colors: [Colors.grey, Colors.grey]),
                        width: 14,
                        borderRadius: const BorderRadius.vertical(
                            top: Radius.circular(6)),
                      ),
                    ],
                  );
                }).toList(),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildCategoryBreakdown(bool isDark) {
    final r = _report!;
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: isDark ? Colors.grey[900] : Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(isDark ? 0.3 : 0.06),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Revenue by Category',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
          const SizedBox(height: 16),
          _categoryItem('Gym Membership', r.gymRevenue, r.gymDue,
              AppTheme.primaryColor, Icons.fitness_center, r.totalRevenue),
          const SizedBox(height: 12),
          _categoryItem('Locker Rental', r.lockerRevenue, r.lockerDue,
              AppTheme.infoColor, Icons.lock_outline, r.totalRevenue),
          const SizedBox(height: 12),
          _categoryItem('Boxing', r.boxingRevenue, r.boxingDue,
              AppTheme.warningColor, Icons.sports_mma, r.totalRevenue),
        ],
      ),
    );
  }

  Widget _categoryItem(String label, double revenue, double due, Color color,
      IconData icon, double totalRevenue) {
    final pct = totalRevenue > 0 ? (revenue / totalRevenue) : 0.0;
    return Row(
      children: [
        Container(
          width: 42,
          height: 42,
          decoration: BoxDecoration(
            color: color.withOpacity(0.1),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Icon(icon, color: color, size: 20),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(label,
                      style: const TextStyle(
                          fontWeight: FontWeight.w600, fontSize: 14)),
                  Text('Rs. ${revenue.toStringAsFixed(0)}',
                      style: TextStyle(
                          fontWeight: FontWeight.w700,
                          fontSize: 14,
                          color: color)),
                ],
              ),
              const SizedBox(height: 6),
              ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: LinearProgressIndicator(
                  value: pct,
                  backgroundColor: color.withOpacity(0.08),
                  valueColor: AlwaysStoppedAnimation(color),
                  minHeight: 6,
                ),
              ),
              const SizedBox(height: 4),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('${(pct * 100).toStringAsFixed(1)}%',
                      style: TextStyle(fontSize: 11, color: Colors.grey[500])),
                  if (due > 0)
                    Text('Due: Rs. ${due.toStringAsFixed(0)}',
                        style: const TextStyle(
                            fontSize: 11, color: AppTheme.dangerColor)),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildQuickStats(bool isDark) {
    final r = _report!;
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: isDark ? Colors.grey[900] : Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(isDark ? 0.3 : 0.06),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Quick Stats',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
          const SizedBox(height: 16),
          Row(
            children: [
              Expanded(
                child: _quickStatItem('Total Members',
                    '${r.totalGymMembers}', Icons.people, AppTheme.primaryColor),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _quickStatItem('Boxing Members',
                    '${r.totalBoxingMembers}', Icons.sports_mma, AppTheme.warningColor),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: _quickStatItem('Total Lockers',
                    '${r.totalLockers}', Icons.lock, AppTheme.infoColor),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _quickStatItem('Active Lockers',
                    '${r.activeLockers}', Icons.lock_open, AppTheme.successColor),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: _quickStatItem("Today's Txns",
                    '${r.todayTransactions}', Icons.receipt_long, AppTheme.secondaryColor),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _quickStatItem('Growth',
                    '${r.revenueGrowth.toStringAsFixed(1)}%', 
                    r.revenueGrowth >= 0 ? Icons.trending_up : Icons.trending_down, 
                    r.revenueGrowth >= 0 ? AppTheme.successColor : AppTheme.dangerColor),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _quickStatItem(
      String label, String value, IconData icon, Color color) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: color.withOpacity(0.06),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: color.withOpacity(0.12)),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: color.withOpacity(0.12),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(icon, color: color, size: 18),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(value,
                    style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: color)),
                Text(label,
                    style: TextStyle(
                        fontSize: 11, color: Colors.grey[500]),
                    overflow: TextOverflow.ellipsis),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
