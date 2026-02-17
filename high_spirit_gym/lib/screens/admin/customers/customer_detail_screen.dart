import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/models/customer.dart';
import 'package:high_spirit_gym/models/membership.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';

class CustomerDetailScreen extends StatefulWidget {
  final int customerId;
  const CustomerDetailScreen({super.key, required this.customerId});

  @override
  State<CustomerDetailScreen> createState() => _CustomerDetailScreenState();
}

class _CustomerDetailScreenState extends State<CustomerDetailScreen> {
  Customer? _customer;
  List<Membership> _memberships = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    try {
      final auth = context.read<AuthProvider>();
      final custResp = await auth.api.get('/customers/${widget.customerId}');
      final memResp =
          await auth.api.get('/memberships/customer/${widget.customerId}');

      setState(() {
        _customer = Customer.fromJson(custResp['data']);
        _memberships = (memResp['data'] as List? ?? [])
            .map((e) => Membership.fromJson(e))
            .toList();
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _deleteCustomer() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete Member'),
        content: const Text('Are you sure you want to delete this member?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Cancel')),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(backgroundColor: AppTheme.dangerColor),
            child: const Text('Delete'),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    try {
      final auth = context.read<AuthProvider>();
      await auth.api.delete('/customers/${widget.customerId}');
      if (mounted) Navigator.pop(context);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Error: $e')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_customer?.fullName ?? 'Member Details'),
        actions: [
          IconButton(
            icon: const Icon(Icons.delete_outline),
            onPressed: _deleteCustomer,
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _customer == null
              ? const Center(child: Text('Member not found'))
              : SingleChildScrollView(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      _buildHeader(),
                      const SizedBox(height: 16),
                      _buildInfoCard(),
                      const SizedBox(height: 16),
                      _buildMembershipHistory(),
                    ],
                  ),
                ),
    );
  }

  Widget _buildHeader() {
    final c = _customer!;
    final photo = c.photoBase64;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        gradient: AppTheme.primaryGradient,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        children: [
          CircleAvatar(
            radius: 45,
            backgroundColor: Colors.white,
            backgroundImage: photo != null && photo.isNotEmpty
                ? MemoryImage(base64Decode(photo))
                : null,
            child: photo == null || photo.isEmpty
                ? Text(c.fullName.isNotEmpty ? c.fullName[0] : '?',
                    style: const TextStyle(
                        fontSize: 36,
                        color: AppTheme.primaryColor,
                        fontWeight: FontWeight.bold))
                : null,
          ),
          const SizedBox(height: 12),
          Text(c.fullName,
              style: const TextStyle(
                  color: Colors.white,
                  fontSize: 20,
                  fontWeight: FontWeight.bold)),
          const SizedBox(height: 4),
          Text(c.phone ?? '',
              style: TextStyle(color: Colors.white.withOpacity(0.8))),
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
            decoration: BoxDecoration(
              color: c.isActive && !c.isExpired
                  ? Colors.green.withOpacity(0.3)
                  : Colors.red.withOpacity(0.3),
              borderRadius: BorderRadius.circular(20),
            ),
            child: Text(
              c.statusText,
              style: const TextStyle(
                  color: Colors.white, fontWeight: FontWeight.w600),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildInfoCard() {
    final c = _customer!;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Details',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
            const Divider(height: 20),
            _row('Email', c.email ?? 'N/A'),
            _row('Address', c.address ?? 'N/A'),
            _row('Gender', c.gender ?? 'N/A'),
            _row('Blood Group', c.bloodGroup ?? 'N/A'),
            _row('Weight', '${c.weightKG ?? 'N/A'} kg'),
            _row('Height', c.height ?? 'N/A'),
            _row('Occupation', c.occupation ?? 'N/A'),
            _row('Shift', c.shift ?? 'N/A'),
            _row('Joined', c.joinDate.toString().substring(0, 10)),
            _row('DOB', c.dateOfBirth?.toString().substring(0, 10) ?? 'N/A'),
            if (c.remarks != null && c.remarks!.isNotEmpty) _row('Remarks', c.remarks!),
            const Divider(height: 20),
            _row('Plan', c.currentPlan ?? 'None'),
            _row('Paid', 'Rs. ${c.paidPrice ?? 0}'),
            _row('Due', 'Rs. ${c.dueAmount ?? 0}',
                valueColor: (c.dueAmount ?? 0) > 0 ? AppTheme.dangerColor : null),
            _row('Expires',
                c.membershipExpire?.toString().substring(0, 10) ?? 'N/A'),
          ],
        ),
      ),
    );
  }

  Widget _buildMembershipHistory() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Membership History',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
            const Divider(height: 20),
            if (_memberships.isEmpty)
              const Center(child: Padding(padding: EdgeInsets.all(16), child: Text('No history'))),
            ..._memberships.map((m) => Container(
                  margin: const EdgeInsets.only(bottom: 8),
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    border: Border.all(color: Colors.grey[300]!),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(m.planName ?? 'N/A',
                              style: const TextStyle(fontWeight: FontWeight.w600)),
                          Text(
                            m.isActive && !m.isExpired ? 'Active' : 'Expired',
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: m.isActive && !m.isExpired
                                  ? AppTheme.successColor
                                  : AppTheme.dangerColor,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Text(
                        '${m.startDate.toString().substring(0, 10)} → ${m.expireDate.toString().substring(0, 10)}  |  Rs.${m.paidPrice}',
                        style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                      ),
                    ],
                  ),
                )),
          ],
        ),
      ),
    );
  }

  Widget _row(String label, String value, {Color? valueColor}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: Colors.grey[600], fontSize: 13)),
          Flexible(
            child: Text(value,
                style: TextStyle(
                    fontWeight: FontWeight.w500,
                    fontSize: 13,
                    color: valueColor),
                textAlign: TextAlign.end),
          ),
        ],
      ),
    );
  }
}
