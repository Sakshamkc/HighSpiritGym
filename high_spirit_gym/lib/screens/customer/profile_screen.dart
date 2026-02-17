import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  Map<String, dynamic>? _customer;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  Future<void> _loadProfile() async {
    final auth = context.read<AuthProvider>();
    final customerId = auth.user?.customerId;
    if (customerId == null) {
      setState(() => _isLoading = false);
      return;
    }

    try {
      final resp = await auth.api.get('/customers/$customerId');
      setState(() {
        _customer = resp['data'];
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('My Profile')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _customer == null
              ? const Center(child: Text('Profile not found'))
              : SingleChildScrollView(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      // Profile Header
                      _buildProfileHeader(),
                      const SizedBox(height: 20),
                      // Details Card
                      _buildDetailCard(),
                    ],
                  ),
                ),
    );
  }

  Widget _buildProfileHeader() {
    final photo = _customer?['photoBase64'];
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
            radius: 50,
            backgroundColor: Colors.white,
            backgroundImage: photo != null && photo.toString().isNotEmpty
                ? MemoryImage(base64Decode(photo))
                : null,
            child: photo == null || photo.toString().isEmpty
                ? const Icon(Icons.person, size: 50, color: AppTheme.primaryColor)
                : null,
          ),
          const SizedBox(height: 12),
          Text(
            _customer?['fullName'] ?? '',
            style: const TextStyle(
                color: Colors.white, fontSize: 22, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 4),
          Text(
            _customer?['phone'] ?? '',
            style: TextStyle(color: Colors.white.withOpacity(0.8), fontSize: 14),
          ),
        ],
      ),
    );
  }

  Widget _buildDetailCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Personal Information',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
            const Divider(height: 24),
            _detailRow(Icons.email_outlined, 'Email', _customer?['email'] ?? 'N/A'),
            _detailRow(Icons.location_on_outlined, 'Address', _customer?['address'] ?? 'N/A'),
            _detailRow(Icons.wc_outlined, 'Gender', _customer?['gender'] ?? 'N/A'),
            _detailRow(Icons.bloodtype_outlined, 'Blood Group', _customer?['bloodGroup'] ?? 'N/A'),
            _detailRow(Icons.monitor_weight_outlined, 'Weight', '${_customer?['weightKG'] ?? 'N/A'} kg'),
            _detailRow(Icons.height_outlined, 'Height', _customer?['height'] ?? 'N/A'),
            _detailRow(Icons.work_outline, 'Occupation', _customer?['occupation'] ?? 'N/A'),
            _detailRow(Icons.cake_outlined, 'Date of Birth',
                _customer?['dateOfBirth']?.toString().substring(0, 10) ?? 'N/A'),
            _detailRow(Icons.schedule_outlined, 'Shift', _customer?['shift'] ?? 'N/A'),
          ],
        ),
      ),
    );
  }

  Widget _detailRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          Icon(icon, size: 20, color: AppTheme.primaryColor),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: TextStyle(fontSize: 12, color: Colors.grey[600])),
                Text(value, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w500)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
