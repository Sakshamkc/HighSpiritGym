import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/models/locker.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';

class LockerListScreen extends StatefulWidget {
  const LockerListScreen({super.key});

  @override
  State<LockerListScreen> createState() => _LockerListScreenState();
}

class _LockerListScreenState extends State<LockerListScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  List<GymLocker> _lockers = [];
  bool _isLoading = true;
  String _gender = '';

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    _tabController.addListener(() {
      if (!_tabController.indexIsChanging) {
        final genders = ['', 'Gents', 'Ladies'];
        _gender = genders[_tabController.index];
        _loadLockers();
      }
    });
    _loadLockers();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  Future<void> _loadLockers() async {
    setState(() => _isLoading = true);
    try {
      final auth = context.read<AuthProvider>();
      final query = <String, String>{};
      if (_gender.isNotEmpty) query['gender'] = _gender;

      final resp = await auth.api.get('/locker', query: query);
      final list = resp['data'] as List? ?? [];
      setState(() {
        _lockers = list.map((e) => GymLocker.fromJson(e)).toList();
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  Color _statusColor(GymLocker l) {
    if (l.isExpired) return AppTheme.dangerColor;
    if (l.isExpiringSoon) return AppTheme.warningColor;
    return AppTheme.successColor;
  }

  String _statusText(GymLocker l) {
    if (l.isExpired) return 'Expired';
    if (l.isExpiringSoon) return '${l.daysRemaining}d left';
    return 'Active';
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        TabBar(
          controller: _tabController,
          labelColor: AppTheme.primaryColor,
          tabs: const [
            Tab(text: 'All'),
            Tab(text: 'Gents'),
            Tab(text: 'Ladies'),
          ],
        ),

        Expanded(
          child: _isLoading
              ? const Center(child: CircularProgressIndicator())
              : _lockers.isEmpty
                  ? const Center(child: Text('No lockers found'))
                  : RefreshIndicator(
                      onRefresh: _loadLockers,
                      child: GridView.builder(
                        padding: const EdgeInsets.all(12),
                        gridDelegate:
                            const SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: 2,
                          childAspectRatio: 0.85,
                          mainAxisSpacing: 10,
                          crossAxisSpacing: 10,
                        ),
                        itemCount: _lockers.length,
                        itemBuilder: (ctx, index) {
                          final l = _lockers[index];
                          return _LockerCard(
                            locker: l,
                            statusColor: _statusColor(l),
                            statusText: _statusText(l),
                            onTap: () => _showDetail(l),
                          );
                        },
                      ),
                    ),
        ),
      ],
    );
  }

  void _showDetail(GymLocker l) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => DraggableScrollableSheet(
        initialChildSize: 0.65,
        maxChildSize: 0.9,
        minChildSize: 0.4,
        expand: false,
        builder: (_, controller) => SingleChildScrollView(
          controller: controller,
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  decoration: BoxDecoration(
                    color: Colors.grey[300],
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Center(
                child: Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: _statusColor(l).withOpacity(0.1),
                    shape: BoxShape.circle,
                  ),
                  child: Text(
                    '#${l.lockerNumber}',
                    style: TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                      color: _statusColor(l),
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 12),
              Center(
                child: Text(l.assignedTo ?? 'Unassigned',
                    style: const TextStyle(
                        fontSize: 18, fontWeight: FontWeight.bold)),
              ),
              const SizedBox(height: 20),
              _detailRow('Locker Number', l.lockerNumber),
              _detailRow('Gender', l.gender),
              _detailRow('Phone', l.assignedPhone ?? 'N/A'),
              _detailRow('Start Date',
                  l.startDate?.toString().substring(0, 10) ?? 'N/A'),
              _detailRow('End Date',
                  l.endDate?.toString().substring(0, 10) ?? 'N/A'),
              _detailRow('Status', _statusText(l)),
              _detailRow('Monthly Rate', 'Rs. ${l.monthlyRate.toStringAsFixed(0)}'),
              _detailRow('Total Amount', 'Rs. ${l.totalAmount.toStringAsFixed(0)}'),
              _detailRow('Paid', 'Rs. ${l.paidAmount.toStringAsFixed(0)}'),
              _detailRow('Due', 'Rs. ${l.dueAmount.toStringAsFixed(0)}'),
              if (l.remarks != null && l.remarks!.isNotEmpty)
                _detailRow('Remarks', l.remarks!),
            ],
          ),
        ),
      ),
    );
  }

  Widget _detailRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: Colors.grey[600])),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }
}

class _LockerCard extends StatelessWidget {
  final GymLocker locker;
  final Color statusColor;
  final String statusText;
  final VoidCallback onTap;

  const _LockerCard({
    required this.locker,
    required this.statusColor,
    required this.statusText,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Card(
        elevation: 2,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        child: Container(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: statusColor.withOpacity(0.3)),
          ),
          padding: const EdgeInsets.all(12),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                  color: statusColor.withOpacity(0.1),
                  shape: BoxShape.circle,
                ),
                child: Center(
                  child: Text(
                    '#${locker.lockerNumber}',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      color: statusColor,
                      fontSize: 14,
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 8),
              Text(
                locker.assignedTo ?? 'Empty',
                style: const TextStyle(
                    fontWeight: FontWeight.w600, fontSize: 13),
                textAlign: TextAlign.center,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
              const SizedBox(height: 4),
              Text(
                locker.gender,
                style: TextStyle(fontSize: 11, color: Colors.grey[500]),
              ),
              const Spacer(),
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
                decoration: BoxDecoration(
                  color: statusColor.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  statusText,
                  style: TextStyle(
                    color: statusColor,
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              if (locker.dueAmount > 0) ...[
                const SizedBox(height: 4),
                Text(
                  'Due: Rs.${locker.dueAmount.toStringAsFixed(0)}',
                  style: const TextStyle(
                    color: AppTheme.dangerColor,
                    fontSize: 11,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
