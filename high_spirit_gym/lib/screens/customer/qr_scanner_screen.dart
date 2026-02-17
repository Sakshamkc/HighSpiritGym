import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';

class QrScannerScreen extends StatefulWidget {
  const QrScannerScreen({super.key});

  @override
  State<QrScannerScreen> createState() => _QrScannerScreenState();
}

class _QrScannerScreenState extends State<QrScannerScreen> {
  MobileScannerController? _controller;
  bool _isProcessing = false;
  String? _statusMessage;
  bool _isSuccess = false;
  bool _isScannerActive = false;

  @override
  void dispose() {
    _controller?.dispose();
    super.dispose();
  }

  void _startScanner() {
    _controller = MobileScannerController(
      detectionSpeed: DetectionSpeed.normal,
      facing: CameraFacing.back,
    );
    setState(() {
      _isScannerActive = true;
      _statusMessage = null;
    });
  }

  void _stopScanner() {
    _controller?.dispose();
    _controller = null;
    setState(() => _isScannerActive = false);
  }

  Future<void> _onDetect(BarcodeCapture capture) async {
    if (_isProcessing) return;
    final barcode = capture.barcodes.firstOrNull;
    if (barcode == null || barcode.rawValue == null) return;

    setState(() => _isProcessing = true);
    _stopScanner();

    final qrData = barcode.rawValue!;

    // Parse QR token from QR code
    // Expected QR format: "HIGHSPIRIT-{guid-token}" or legacy "HIGHSPIRIT-{customerID}"
    String? qrToken;
    int? customerId;

    if (qrData.startsWith('HIGHSPIRIT-')) {
      final payload = qrData.substring(11);
      // Check if it's a GUID (contains dashes and letters) or a numeric ID
      if (payload.contains('-') || payload.contains(RegExp(r'[a-fA-F]'))) {
        qrToken = payload;
      } else {
        customerId = int.tryParse(payload);
      }
    } else {
      customerId = int.tryParse(qrData);
    }

    if (qrToken == null && customerId == null) {
      setState(() {
        _statusMessage = 'Invalid QR code format';
        _isSuccess = false;
        _isProcessing = false;
      });
      return;
    }

    try {
      final auth = context.read<AuthProvider>();
      final body = <String, dynamic>{};
      if (qrToken != null) {
        body['qrToken'] = qrToken;
      } else {
        body['customerID'] = customerId;
      }

      final resp = await auth.api.post('/attendance/checkin', body: body);

      setState(() {
        _statusMessage = resp['message'] ?? 'Check-in successful!';
        _isSuccess = resp['success'] ?? false;
        _isProcessing = false;
      });
    } catch (e) {
      setState(() {
        _statusMessage = e.toString();
        _isSuccess = false;
        _isProcessing = false;
      });
    }
  }

  // Manual check-in for customer (self)
  Future<void> _selfCheckIn() async {
    final auth = context.read<AuthProvider>();
    final customerId = auth.user?.customerId;
    if (customerId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('No customer profile linked')),
      );
      return;
    }

    setState(() => _isProcessing = true);

    try {
      final resp = await auth.api.post('/attendance/checkin', body: {
        'customerID': customerId,
      });

      setState(() {
        _statusMessage = resp['message'] ?? 'Checked in!';
        _isSuccess = resp['success'] ?? false;
        _isProcessing = false;
      });
    } catch (e) {
      setState(() {
        _statusMessage = e.toString();
        _isSuccess = false;
        _isProcessing = false;
      });
    }
  }

  Future<void> _selfCheckOut() async {
    final auth = context.read<AuthProvider>();
    final customerId = auth.user?.customerId;
    if (customerId == null) return;

    setState(() => _isProcessing = true);

    try {
      final resp = await auth.api.post('/attendance/checkout/$customerId');
      setState(() {
        _statusMessage = resp['message'] ?? 'Checked out!';
        _isSuccess = resp['success'] ?? false;
        _isProcessing = false;
      });
    } catch (e) {
      setState(() {
        _statusMessage = e.toString();
        _isSuccess = false;
        _isProcessing = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final isAdminView = auth.isAdmin;

    return Scaffold(
      appBar: AppBar(
        title: Text(isAdminView ? 'QR Scanner - Check In' : 'Attendance'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            // Scanner area (for admin) or self check-in (for customer)
            if (isAdminView) ...[
              // QR Scanner for admin
              if (_isScannerActive && _controller != null)
                Expanded(
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(16),
                    child: MobileScanner(
                      controller: _controller!,
                      onDetect: _onDetect,
                    ),
                  ),
                )
              else
                Expanded(
                  child: Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Container(
                          width: 120,
                          height: 120,
                          decoration: BoxDecoration(
                            color: AppTheme.primaryColor.withOpacity(0.1),
                            borderRadius: BorderRadius.circular(20),
                          ),
                          child: const Icon(Icons.qr_code_scanner,
                              size: 60, color: AppTheme.primaryColor),
                        ),
                        const SizedBox(height: 20),
                        const Text('Scan member QR code',
                            style: TextStyle(fontSize: 16)),
                        const SizedBox(height: 20),
                        ElevatedButton.icon(
                          onPressed: _startScanner,
                          icon: const Icon(Icons.camera_alt),
                          label: const Text('Start Scanner'),
                        ),
                      ],
                    ),
                  ),
                ),
            ] else ...[
              // Self check-in for customer
              Expanded(
                child: Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Container(
                        width: 140,
                        height: 140,
                        decoration: BoxDecoration(
                          gradient: AppTheme.primaryGradient,
                          borderRadius: BorderRadius.circular(30),
                          boxShadow: [
                            BoxShadow(
                              color: AppTheme.primaryColor.withOpacity(0.3),
                              blurRadius: 20,
                              offset: const Offset(0, 10),
                            ),
                          ],
                        ),
                        child: const Icon(Icons.fingerprint,
                            size: 70, color: Colors.white),
                      ),
                      const SizedBox(height: 30),
                      const Text(
                        'Tap to Check In/Out',
                        style: TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
                      ),
                      const SizedBox(height: 30),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          ElevatedButton.icon(
                            onPressed: _isProcessing ? null : _selfCheckIn,
                            icon: const Icon(Icons.login),
                            label: const Text('Check In'),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: AppTheme.successColor,
                              padding: const EdgeInsets.symmetric(
                                  horizontal: 24, vertical: 14),
                            ),
                          ),
                          const SizedBox(width: 16),
                          ElevatedButton.icon(
                            onPressed: _isProcessing ? null : _selfCheckOut,
                            icon: const Icon(Icons.logout),
                            label: const Text('Check Out'),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: AppTheme.warningColor,
                              padding: const EdgeInsets.symmetric(
                                  horizontal: 24, vertical: 14),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            ],

            // Status message
            if (_isProcessing)
              const Padding(
                padding: EdgeInsets.all(16),
                child: CircularProgressIndicator(),
              ),

            if (_statusMessage != null)
              Container(
                width: double.infinity,
                margin: const EdgeInsets.only(top: 16),
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: _isSuccess
                      ? AppTheme.successColor.withOpacity(0.1)
                      : AppTheme.dangerColor.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(
                    color: _isSuccess ? AppTheme.successColor : AppTheme.dangerColor,
                  ),
                ),
                child: Row(
                  children: [
                    Icon(
                      _isSuccess ? Icons.check_circle : Icons.error_outline,
                      color: _isSuccess ? AppTheme.successColor : AppTheme.dangerColor,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        _statusMessage!,
                        style: TextStyle(
                          color:
                              _isSuccess ? AppTheme.successColor : AppTheme.dangerColor,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}
