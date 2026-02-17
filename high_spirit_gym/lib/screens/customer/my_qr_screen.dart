import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';

class MyQrScreen extends StatefulWidget {
  const MyQrScreen({super.key});

  @override
  State<MyQrScreen> createState() => _MyQrScreenState();
}

class _MyQrScreenState extends State<MyQrScreen>
    with SingleTickerProviderStateMixin {
  String? _qrToken;
  String? _customerName;
  Map<String, dynamic>? _customer;
  bool _isLoading = true;
  String? _error;
  late AnimationController _pulseController;
  late Animation<double> _pulseAnimation;

  @override
  void initState() {
    super.initState();
    _pulseController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 2000),
    )..repeat(reverse: true);
    _pulseAnimation = Tween<double>(begin: 0.97, end: 1.03).animate(
      CurvedAnimation(parent: _pulseController, curve: Curves.easeInOut),
    );
    _loadQrToken();
  }

  @override
  void dispose() {
    _pulseController.dispose();
    super.dispose();
  }

  Future<void> _loadQrToken() async {
    final auth = context.read<AuthProvider>();
    final customerId = auth.user?.customerId;
    if (customerId == null) {
      setState(() {
        _isLoading = false;
        _error = 'No customer profile linked.';
      });
      return;
    }

    try {
      // Fetch QR token and customer data in parallel
      final results = await Future.wait([
        auth.api.get('/attendance/my-qr'),
        auth.api.get('/customers/$customerId'),
      ]);

      final qrResp = results[0];
      final custResp = results[1];
      final qrData = qrResp['data'] ?? qrResp;

      setState(() {
        _qrToken = qrData['qrToken'] ?? qrData['QrToken'];
        _customerName = qrData['customerName'] ?? qrData['CustomerName'];
        _customer = custResp['data'];
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
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: const Text('My QR Code'),
        centerTitle: true,
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _buildError()
              : _qrToken == null
                  ? _buildNoProfile()
                  : SingleChildScrollView(
                      padding: const EdgeInsets.all(20),
                      child: Column(
                        children: [
                          const SizedBox(height: 10),
                          Text(
                            'Show this QR code at the gym entrance',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w500,
                              color: Colors.grey[600],
                            ),
                            textAlign: TextAlign.center,
                          ),
                          const SizedBox(height: 8),
                          Text(
                            'The scanner will automatically check you in',
                            style: TextStyle(fontSize: 13, color: Colors.grey[400]),
                            textAlign: TextAlign.center,
                          ),
                          const SizedBox(height: 30),

                          // QR Code Card
                          ScaleTransition(
                            scale: _pulseAnimation,
                            child: Container(
                              padding: const EdgeInsets.all(24),
                              decoration: BoxDecoration(
                                color: isDark ? Colors.grey[850] : Colors.white,
                                borderRadius: BorderRadius.circular(24),
                                boxShadow: [
                                  BoxShadow(
                                    color: AppTheme.primaryColor.withOpacity(0.15),
                                    blurRadius: 30,
                                    offset: const Offset(0, 15),
                                  ),
                                ],
                                border: Border.all(
                                  color: AppTheme.primaryColor.withOpacity(0.2),
                                  width: 2,
                                ),
                              ),
                              child: Column(
                                children: [
                                  Text(
                                    _customerName ?? 'Member',
                                    style: const TextStyle(
                                      fontSize: 20,
                                      fontWeight: FontWeight.bold,
                                    ),
                                    textAlign: TextAlign.center,
                                  ),
                                  const SizedBox(height: 4),
                                  Container(
                                    padding: const EdgeInsets.symmetric(
                                        horizontal: 12, vertical: 4),
                                    decoration: BoxDecoration(
                                      gradient: AppTheme.primaryGradient,
                                      borderRadius: BorderRadius.circular(20),
                                    ),
                                    child: const Text(
                                      'Member',
                                      style: TextStyle(
                                        color: Colors.white,
                                        fontSize: 12,
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                  ),
                                  const SizedBox(height: 20),

                                  // QR Code
                                  Container(
                                    padding: const EdgeInsets.all(16),
                                    decoration: BoxDecoration(
                                      color: Colors.white,
                                      borderRadius: BorderRadius.circular(16),
                                    ),
                                    child: QrImageView(
                                      data: 'HIGHSPIRIT-$_qrToken',
                                      version: QrVersions.auto,
                                      size: 220,
                                      eyeStyle: const QrEyeStyle(
                                        eyeShape: QrEyeShape.square,
                                        color: Color(0xFF1A1A2E),
                                      ),
                                      dataModuleStyle: const QrDataModuleStyle(
                                        dataModuleShape: QrDataModuleShape.square,
                                        color: Color(0xFF1A1A2E),
                                      ),
                                      errorCorrectionLevel: QrErrorCorrectLevel.H,
                                    ),
                                  ),
                                  const SizedBox(height: 16),

                                  // Token preview (truncated)
                                  Text(
                                    _qrToken ?? '',
                                    style: TextStyle(
                                      fontSize: 11,
                                      fontFamily: 'monospace',
                                      letterSpacing: 1,
                                      color: Colors.grey[400],
                                      fontWeight: FontWeight.w500,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                          const SizedBox(height: 30),

                          // Membership info
                          if (_customer != null) _buildMembershipInfo(isDark),
                          const SizedBox(height: 20),

                          // Instructions
                          _buildInstructions(),
                        ],
                      ),
                    ),
    );
  }

  Widget _buildError() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 60, color: AppTheme.dangerColor),
            const SizedBox(height: 16),
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            ElevatedButton(onPressed: _loadQrToken, child: const Text('Retry')),
          ],
        ),
      ),
    );
  }

  Widget _buildNoProfile() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                color: AppTheme.warningColor.withOpacity(0.1),
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.qr_code_2,
                  size: 60, color: AppTheme.warningColor),
            ),
            const SizedBox(height: 20),
            const Text(
              'No Customer Profile Linked',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(
              'Contact the gym admin to link\nyour account to your customer profile.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.grey[600]),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMembershipInfo(bool isDark) {
    final phone = _customer?['phone'] ?? '';
    final email = _customer?['email'] ?? '';
    final isActive = _customer?['isActive'] ?? false;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: isDark ? Colors.grey[850] : Colors.grey[50],
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isDark ? Colors.grey[700]! : Colors.grey[200]!,
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.person_outline,
                  size: 18, color: AppTheme.primaryColor),
              const SizedBox(width: 8),
              const Text('Member Details',
                  style: TextStyle(fontWeight: FontWeight.w600, fontSize: 15)),
              const Spacer(),
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: isActive
                      ? AppTheme.successColor.withOpacity(0.1)
                      : AppTheme.dangerColor.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  isActive ? 'Active' : 'Inactive',
                  style: TextStyle(
                    color:
                        isActive ? AppTheme.successColor : AppTheme.dangerColor,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          if (phone.isNotEmpty && phone != 'N/A')
            _infoRow(Icons.phone_outlined, phone),
          if (email.isNotEmpty)
            _infoRow(Icons.email_outlined, email),
        ],
      ),
    );
  }

  Widget _infoRow(IconData icon, String text) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        children: [
          Icon(icon, size: 16, color: Colors.grey[500]),
          const SizedBox(width: 8),
          Text(text, style: TextStyle(color: Colors.grey[600], fontSize: 13)),
        ],
      ),
    );
  }

  Widget _buildInstructions() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppTheme.infoColor.withOpacity(0.06),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppTheme.infoColor.withOpacity(0.15)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(Icons.info_outline, size: 18, color: AppTheme.infoColor),
              SizedBox(width: 8),
              Text('How it works',
                  style: TextStyle(
                      fontWeight: FontWeight.w600,
                      color: AppTheme.infoColor,
                      fontSize: 14)),
            ],
          ),
          const SizedBox(height: 12),
          _stepRow('1', 'Open this QR code at the gym entrance'),
          _stepRow('2', 'Admin scans your QR with the gym scanner'),
          _stepRow('3', 'You\'re automatically checked in!'),
          _stepRow('4', 'Check out when you leave the gym'),
        ],
      ),
    );
  }

  Widget _stepRow(String num, String text) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 22,
            height: 22,
            decoration: BoxDecoration(
              color: AppTheme.infoColor.withOpacity(0.15),
              shape: BoxShape.circle,
            ),
            child: Center(
              child: Text(num,
                  style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: AppTheme.infoColor)),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Text(text,
                style: TextStyle(fontSize: 13, color: Colors.grey[600])),
          ),
        ],
      ),
    );
  }
}
