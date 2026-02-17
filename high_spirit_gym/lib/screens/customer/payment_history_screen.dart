import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/models/membership.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';

class PaymentHistoryScreen extends StatefulWidget {
  const PaymentHistoryScreen({super.key});

  @override
  State<PaymentHistoryScreen> createState() => _PaymentHistoryScreenState();
}

class _PaymentHistoryScreenState extends State<PaymentHistoryScreen> {
  List<Membership> _payments = [];
  bool _isLoading = true;
  int _totalPaid = 0;
  int _totalDue = 0;

  @override
  void initState() {
    super.initState();
    _loadPayments();
  }

  Future<void> _loadPayments() async {
    final auth = context.read<AuthProvider>();
    final customerId = auth.user?.customerId;
    if (customerId == null) {
      setState(() => _isLoading = false);
      return;
    }

    try {
      final resp = await auth.api.get('/memberships/customer/$customerId');
      final list = resp['data'] as List? ?? [];
      final payments = list.map((e) => Membership.fromJson(e)).toList();

      setState(() {
        _payments = payments;
        _totalPaid = payments.fold(0, (sum, m) => sum + m.paidPrice);
        _totalDue = payments.fold(0, (sum, m) => sum + m.dueAmount);
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Payment History')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              children: [
                // Summary Cards
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    children: [
                      Expanded(
                        child: _summaryCard(
                          'Total Paid',
                          'Rs. $_totalPaid',
                          AppTheme.successGradient,
                          Icons.check_circle_outline,
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: _summaryCard(
                          'Total Due',
                          'Rs. $_totalDue',
                          AppTheme.dangerGradient,
                          Icons.warning_amber_outlined,
                        ),
                      ),
                    ],
                  ),
                ),

                // Payment List
                Expanded(
                  child: _payments.isEmpty
                      ? const Center(child: Text('No payment records'))
                      : ListView.builder(
                          padding: const EdgeInsets.symmetric(horizontal: 16),
                          itemCount: _payments.length,
                          itemBuilder: (context, index) {
                            final p = _payments[index];
                            return Card(
                              margin: const EdgeInsets.only(bottom: 8),
                              child: ListTile(
                                leading: Container(
                                  width: 44,
                                  height: 44,
                                  decoration: BoxDecoration(
                                    color: AppTheme.primaryColor.withOpacity(0.1),
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  child: const Icon(Icons.receipt,
                                      color: AppTheme.primaryColor),
                                ),
                                title: Text(
                                  p.planName ?? 'Payment',
                                  style: const TextStyle(fontWeight: FontWeight.w600),
                                ),
                                subtitle: Text(
                                  p.startDate.toString().substring(0, 10),
                                  style: TextStyle(
                                      fontSize: 12, color: Colors.grey[600]),
                                ),
                                trailing: Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  crossAxisAlignment: CrossAxisAlignment.end,
                                  children: [
                                    Text(
                                      'Rs. ${p.paidPrice}',
                                      style: const TextStyle(
                                        fontWeight: FontWeight.bold,
                                        color: AppTheme.successColor,
                                      ),
                                    ),
                                    if (p.dueAmount > 0)
                                      Text(
                                        'Due: Rs. ${p.dueAmount}',
                                        style: const TextStyle(
                                            fontSize: 12,
                                            color: AppTheme.dangerColor),
                                      ),
                                  ],
                                ),
                              ),
                            );
                          },
                        ),
                ),
              ],
            ),
    );
  }

  Widget _summaryCard(
      String title, String value, Gradient gradient, IconData icon) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: gradient,
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: Colors.white, size: 24),
          const SizedBox(height: 8),
          Text(title,
              style: TextStyle(color: Colors.white.withOpacity(0.8), fontSize: 12)),
          const SizedBox(height: 2),
          Text(value,
              style: const TextStyle(
                  color: Colors.white,
                  fontSize: 18,
                  fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }
}
