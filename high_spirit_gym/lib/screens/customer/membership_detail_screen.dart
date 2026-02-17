import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/models/membership.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';

class MembershipDetailScreen extends StatefulWidget {
  const MembershipDetailScreen({super.key});

  @override
  State<MembershipDetailScreen> createState() => _MembershipDetailScreenState();
}

class _MembershipDetailScreenState extends State<MembershipDetailScreen> {
  List<Membership> _memberships = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadMemberships();
  }

  Future<void> _loadMemberships() async {
    final auth = context.read<AuthProvider>();
    final customerId = auth.user?.customerId;
    if (customerId == null) {
      setState(() => _isLoading = false);
      return;
    }

    try {
      final resp = await auth.api.get('/memberships/customer/$customerId');
      final list = resp['data'] as List? ?? [];
      setState(() {
        _memberships = list.map((e) => Membership.fromJson(e)).toList();
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('My Memberships')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _memberships.isEmpty
              ? const Center(child: Text('No membership records'))
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: _memberships.length,
                  itemBuilder: (context, index) {
                    final m = _memberships[index];
                    final isCurrent = m.isActive && !m.isExpired;
                    return Card(
                      margin: const EdgeInsets.only(bottom: 12),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(16),
                        side: isCurrent
                            ? const BorderSide(color: AppTheme.successColor, width: 2)
                            : BorderSide.none,
                      ),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Expanded(
                                  child: Text(
                                    m.planName ?? 'Unknown Plan',
                                    style: const TextStyle(
                                        fontSize: 16, fontWeight: FontWeight.w600),
                                  ),
                                ),
                                Container(
                                  padding: const EdgeInsets.symmetric(
                                      horizontal: 10, vertical: 4),
                                  decoration: BoxDecoration(
                                    color: isCurrent
                                        ? AppTheme.successColor.withOpacity(0.1)
                                        : AppTheme.dangerColor.withOpacity(0.1),
                                    borderRadius: BorderRadius.circular(20),
                                  ),
                                  child: Text(
                                    isCurrent ? 'Active' : 'Expired',
                                    style: TextStyle(
                                      fontSize: 12,
                                      fontWeight: FontWeight.w600,
                                      color: isCurrent
                                          ? AppTheme.successColor
                                          : AppTheme.dangerColor,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            const Divider(height: 20),
                            _row('Duration', '${m.duration} months'),
                            _row('Start', m.startDate.toString().substring(0, 10)),
                            _row('Expires', m.expireDate.toString().substring(0, 10)),
                            _row('Total', 'Rs. ${m.totalPrice}'),
                            _row('Paid', 'Rs. ${m.paidPrice}'),
                            if (m.dueAmount > 0)
                              _row('Due', 'Rs. ${m.dueAmount}',
                                  valueColor: AppTheme.dangerColor),
                            if (isCurrent) ...[
                              const SizedBox(height: 8),
                              LinearProgressIndicator(
                                value: _membershipProgress(m),
                                backgroundColor: Colors.grey[200],
                                valueColor: AlwaysStoppedAnimation(
                                  _membershipProgress(m) > 0.8
                                      ? AppTheme.dangerColor
                                      : AppTheme.successColor,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                '${m.daysRemaining} days remaining',
                                style: TextStyle(
                                    fontSize: 12, color: Colors.grey[600]),
                              ),
                            ],
                          ],
                        ),
                      ),
                    );
                  },
                ),
    );
  }

  double _membershipProgress(Membership m) {
    final total = m.expireDate.difference(m.startDate).inDays;
    final elapsed = DateTime.now().difference(m.startDate).inDays;
    if (total <= 0) return 1;
    return (elapsed / total).clamp(0, 1);
  }

  Widget _row(String label, String value, {Color? valueColor}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: Colors.grey[600], fontSize: 13)),
          Text(value,
              style: TextStyle(
                  fontWeight: FontWeight.w600,
                  fontSize: 13,
                  color: valueColor)),
        ],
      ),
    );
  }
}
