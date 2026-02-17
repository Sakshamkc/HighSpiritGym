import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';

class CustomerFormScreen extends StatefulWidget {
  final int? customerId; // null for create, non-null for edit
  const CustomerFormScreen({super.key, this.customerId});

  @override
  State<CustomerFormScreen> createState() => _CustomerFormScreenState();
}

class _CustomerFormScreenState extends State<CustomerFormScreen> {
  final _formKey = GlobalKey<FormState>();
  bool _isLoading = false;
  bool _isSaving = false;

  final _nameCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  String _gender = 'Male';
  final _planCtrl = TextEditingController();
  final _paidCtrl = TextEditingController();
  final _dueCtrl = TextEditingController();
  final _durationCtrl = TextEditingController(text: '1');

  bool get isEditing => widget.customerId != null;

  @override
  void initState() {
    super.initState();
    if (isEditing) _loadCustomer();
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _phoneCtrl.dispose();
    _emailCtrl.dispose();
    _addressCtrl.dispose();
    _planCtrl.dispose();
    _paidCtrl.dispose();
    _dueCtrl.dispose();
    _durationCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadCustomer() async {
    setState(() => _isLoading = true);
    try {
      final auth = context.read<AuthProvider>();
      final resp = await auth.api.get('/customers/${widget.customerId}');
      final d = resp['data'];
      _nameCtrl.text = d['fullName'] ?? '';
      _phoneCtrl.text = d['phone'] ?? '';
      _emailCtrl.text = d['email'] ?? '';
      _addressCtrl.text = d['address'] ?? '';
      _gender = d['gender'] ?? 'Male';
      _planCtrl.text = d['currentPlan'] ?? '';
      _paidCtrl.text = (d['paidPrice'] ?? 0).toString();
      _dueCtrl.text = (d['dueAmount'] ?? 0).toString();
    } catch (e) {
      // ignore
    }
    setState(() => _isLoading = false);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _isSaving = true);

    try {
      final auth = context.read<AuthProvider>();
      if (isEditing) {
        await auth.api.put('/customers/${widget.customerId}', body: {
          'fullName': _nameCtrl.text,
          'phone': _phoneCtrl.text,
          'email': _emailCtrl.text,
          'address': _addressCtrl.text,
          'gender': _gender,
          'planName': _planCtrl.text,
          'paidPrice': int.tryParse(_paidCtrl.text) ?? 0,
          'dueAmount': int.tryParse(_dueCtrl.text) ?? 0,
        });
      } else {
        await auth.api.post('/customers', body: {
          'fullName': _nameCtrl.text,
          'phone': _phoneCtrl.text,
          'email': _emailCtrl.text,
          'address': _addressCtrl.text,
          'gender': _gender,
          'joinDate': DateTime.now().toIso8601String(),
          'planName': _planCtrl.text,
          'paidPrice': int.tryParse(_paidCtrl.text) ?? 0,
          'dueAmount': int.tryParse(_dueCtrl.text) ?? 0,
          'startDate': DateTime.now().toIso8601String(),
          'duration': int.tryParse(_durationCtrl.text) ?? 1,
        });
      }
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Error: $e')));
      }
    }
    setState(() => _isSaving = false);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(isEditing ? 'Edit Member' : 'Add Member'),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    TextFormField(
                      controller: _nameCtrl,
                      decoration: const InputDecoration(labelText: 'Full Name *'),
                      validator: (v) => v?.isEmpty == true ? 'Required' : null,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _phoneCtrl,
                      decoration: const InputDecoration(labelText: 'Phone'),
                      keyboardType: TextInputType.phone,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _emailCtrl,
                      decoration: const InputDecoration(labelText: 'Email'),
                      keyboardType: TextInputType.emailAddress,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _addressCtrl,
                      decoration: const InputDecoration(labelText: 'Address'),
                    ),
                    const SizedBox(height: 12),
                    DropdownButtonFormField<String>(
                      value: _gender,
                      decoration: const InputDecoration(labelText: 'Gender'),
                      items: ['Male', 'Female', 'Other']
                          .map((g) => DropdownMenuItem(value: g, child: Text(g)))
                          .toList(),
                      onChanged: (v) => _gender = v ?? 'Male',
                    ),
                    const SizedBox(height: 20),
                    const Text('Membership',
                        style: TextStyle(
                            fontSize: 16, fontWeight: FontWeight.w600)),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _planCtrl,
                      decoration: const InputDecoration(labelText: 'Plan Name'),
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: TextFormField(
                            controller: _paidCtrl,
                            decoration:
                                const InputDecoration(labelText: 'Paid Amount'),
                            keyboardType: TextInputType.number,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: TextFormField(
                            controller: _dueCtrl,
                            decoration:
                                const InputDecoration(labelText: 'Due Amount'),
                            keyboardType: TextInputType.number,
                          ),
                        ),
                      ],
                    ),
                    if (!isEditing) ...[
                      const SizedBox(height: 12),
                      TextFormField(
                        controller: _durationCtrl,
                        decoration: const InputDecoration(
                            labelText: 'Duration (months)'),
                        keyboardType: TextInputType.number,
                      ),
                    ],
                    const SizedBox(height: 24),
                    ElevatedButton(
                      onPressed: _isSaving ? null : _save,
                      child: _isSaving
                          ? const SizedBox(
                              width: 24,
                              height: 24,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : Text(isEditing ? 'Update' : 'Create'),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}
