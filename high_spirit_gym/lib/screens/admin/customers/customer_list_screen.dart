import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/models/customer.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';
import 'package:high_spirit_gym/screens/admin/customers/customer_detail_screen.dart';
import 'package:high_spirit_gym/screens/admin/customers/customer_form_screen.dart';

class CustomerListScreen extends StatefulWidget {
  const CustomerListScreen({super.key});

  @override
  State<CustomerListScreen> createState() => _CustomerListScreenState();
}

class _CustomerListScreenState extends State<CustomerListScreen> {
  List<Customer> _customers = [];
  bool _isLoading = true;
  String _search = '';
  String _filter = 'All';
  int _currentPage = 1;
  int _totalPages = 1;
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _loadCustomers();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadCustomers() async {
    setState(() => _isLoading = true);
    try {
      final auth = context.read<AuthProvider>();
      final query = <String, String>{
        'page': _currentPage.toString(),
        'pageSize': '20',
      };
      if (_search.isNotEmpty) query['search'] = _search;
      if (_filter != 'All') query['filter'] = _filter.toLowerCase();

      final resp = await auth.api.get('/customers', query: query);
      final data = resp['data'] as List? ?? resp['items'] as List? ?? [];
      final items = data
          .map((e) => Customer.fromJson(e))
          .toList();

      setState(() {
        _customers = items;
        _totalPages = (resp['totalPages'] as num?)?.toInt() ?? 1;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // Search & Filter
        Container(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _searchController,
                  decoration: InputDecoration(
                    hintText: 'Search members...',
                    prefixIcon: const Icon(Icons.search),
                    suffixIcon: _search.isNotEmpty
                        ? IconButton(
                            icon: const Icon(Icons.clear),
                            onPressed: () {
                              _searchController.clear();
                              _search = '';
                              _currentPage = 1;
                              _loadCustomers();
                            },
                          )
                        : null,
                    isDense: true,
                    contentPadding: const EdgeInsets.symmetric(vertical: 10),
                  ),
                  onSubmitted: (v) {
                    _search = v;
                    _currentPage = 1;
                    _loadCustomers();
                  },
                ),
              ),
              const SizedBox(width: 8),
              PopupMenuButton<String>(
                icon: const Icon(Icons.filter_list),
                onSelected: (v) {
                  _filter = v;
                  _currentPage = 1;
                  _loadCustomers();
                },
                itemBuilder: (_) => ['All', 'Active', 'Expired', 'Soon']
                    .map((f) => PopupMenuItem(value: f, child: Text(f == 'Soon' ? 'Expiring Soon' : f)))
                    .toList(),
              ),
            ],
          ),
        ),

        // Filter chip
        if (_filter != 'All')
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Row(
              children: [
                Chip(
                  label: Text(_filter == 'Soon' ? 'Expiring Soon' : _filter),
                  deleteIcon: const Icon(Icons.close, size: 16),
                  onDeleted: () {
                    _filter = 'All';
                    _loadCustomers();
                  },
                ),
              ],
            ),
          ),

        // List
        Expanded(
          child: _isLoading
              ? const Center(child: CircularProgressIndicator())
              : _customers.isEmpty
                  ? const Center(child: Text('No members found'))
                  : RefreshIndicator(
                      onRefresh: _loadCustomers,
                      child: ListView.builder(
                        padding: const EdgeInsets.symmetric(horizontal: 12),
                        itemCount: _customers.length,
                        itemBuilder: (context, index) {
                          final c = _customers[index];
                          return _buildCustomerCard(c);
                        },
                      ),
                    ),
        ),

        // Pagination
        if (_totalPages > 1)
          Padding(
            padding: const EdgeInsets.all(8),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                IconButton(
                  icon: const Icon(Icons.chevron_left),
                  onPressed: _currentPage > 1
                      ? () {
                          _currentPage--;
                          _loadCustomers();
                        }
                      : null,
                ),
                Text('$_currentPage / $_totalPages'),
                IconButton(
                  icon: const Icon(Icons.chevron_right),
                  onPressed: _currentPage < _totalPages
                      ? () {
                          _currentPage++;
                          _loadCustomers();
                        }
                      : null,
                ),
              ],
            ),
          ),
      ],
    );
  }

  Widget _buildCustomerCard(Customer c) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        onTap: () async {
          await Navigator.push(
            context,
            MaterialPageRoute(
                builder: (_) => CustomerDetailScreen(customerId: c.customerID)),
          );
          _loadCustomers();
        },
        leading: CircleAvatar(
          backgroundColor: AppTheme.primaryColor.withOpacity(0.1),
          backgroundImage: c.photoBase64 != null && c.photoBase64!.isNotEmpty
              ? MemoryImage(base64Decode(c.photoBase64!))
              : null,
          child: c.photoBase64 == null || c.photoBase64!.isEmpty
              ? Text(c.fullName.isNotEmpty ? c.fullName[0].toUpperCase() : '?',
                  style: const TextStyle(
                      color: AppTheme.primaryColor, fontWeight: FontWeight.bold))
              : null,
        ),
        title: Text(c.fullName,
            style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14)),
        subtitle: Text(
          '${c.phone ?? ''} • ${c.currentPlan ?? 'No Plan'}',
          style: TextStyle(fontSize: 12, color: Colors.grey[600]),
          overflow: TextOverflow.ellipsis,
        ),
        trailing: Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: c.isReallyActive
                ? AppTheme.successColor.withOpacity(0.1)
                : AppTheme.dangerColor.withOpacity(0.1),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Text(
            c.statusText,
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w600,
              color:
                  c.isReallyActive ? AppTheme.successColor : AppTheme.dangerColor,
            ),
          ),
        ),
      ),
    );
  }
}
