import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:high_spirit_gym/config/app_theme.dart';
import 'package:high_spirit_gym/models/boxing_member.dart';
import 'package:high_spirit_gym/providers/auth_provider.dart';

class BoxingListScreen extends StatefulWidget {
  const BoxingListScreen({super.key});

  @override
  State<BoxingListScreen> createState() => _BoxingListScreenState();
}

class _BoxingListScreenState extends State<BoxingListScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  List<BoxingMember> _members = [];
  bool _isLoading = true;
  String _category = '';
  String _search = '';

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    _tabController.addListener(() {
      if (!_tabController.indexIsChanging) {
        final cats = ['', 'Children', 'Adult'];
        _category = cats[_tabController.index];
        _loadMembers();
      }
    });
    _loadMembers();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  Future<void> _loadMembers() async {
    setState(() => _isLoading = true);
    try {
      final auth = context.read<AuthProvider>();
      final query = <String, String>{};
      if (_category.isNotEmpty) query['category'] = _category;
      if (_search.isNotEmpty) query['search'] = _search;

      final resp = await auth.api.get('/boxing', query: query);
      final list = resp['data'] as List? ?? [];
      setState(() {
        _members = list.map((e) => BoxingMember.fromJson(e)).toList();
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
        // Tabs
        TabBar(
          controller: _tabController,
          labelColor: AppTheme.primaryColor,
          tabs: const [
            Tab(text: 'All'),
            Tab(text: 'Children'),
            Tab(text: 'Adult'),
          ],
        ),

        // Search
        Padding(
          padding: const EdgeInsets.all(12),
          child: TextField(
            decoration: const InputDecoration(
              hintText: 'Search boxing members...',
              prefixIcon: Icon(Icons.search),
              isDense: true,
            ),
            onSubmitted: (v) {
              _search = v;
              _loadMembers();
            },
          ),
        ),

        // List
        Expanded(
          child: _isLoading
              ? const Center(child: CircularProgressIndicator())
              : _members.isEmpty
                  ? const Center(child: Text('No boxing members found'))
                  : RefreshIndicator(
                      onRefresh: _loadMembers,
                      child: ListView.builder(
                        padding: const EdgeInsets.symmetric(horizontal: 12),
                        itemCount: _members.length,
                        itemBuilder: (context, index) {
                          final m = _members[index];
                          return Card(
                            margin: const EdgeInsets.only(bottom: 8),
                            child: ListTile(
                              leading: CircleAvatar(
                                backgroundColor:
                                    AppTheme.primaryColor.withOpacity(0.1),
                                backgroundImage: m.photoBase64 != null &&
                                        m.photoBase64!.isNotEmpty
                                    ? MemoryImage(base64Decode(m.photoBase64!))
                                    : null,
                                child: m.photoBase64 == null ||
                                        m.photoBase64!.isEmpty
                                    ? const Icon(Icons.sports_mma,
                                        color: AppTheme.primaryColor)
                                    : null,
                              ),
                              title: Text(m.name,
                                  style: const TextStyle(
                                      fontWeight: FontWeight.w600, fontSize: 14)),
                              subtitle: Text(
                                '${m.category} • Rs.${m.price}',
                                style: TextStyle(
                                    fontSize: 12, color: Colors.grey[600]),
                              ),
                              trailing: m.dueAmount > 0
                                  ? Text('Due: Rs.${m.dueAmount}',
                                      style: const TextStyle(
                                          color: AppTheme.dangerColor,
                                          fontWeight: FontWeight.w600,
                                          fontSize: 12))
                                  : const Icon(Icons.check_circle,
                                      color: AppTheme.successColor, size: 20),
                              onTap: () => _showDetail(m),
                            ),
                          );
                        },
                      ),
                    ),
        ),
      ],
    );
  }

  void _showDetail(BoxingMember m) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => DraggableScrollableSheet(
        initialChildSize: 0.7,
        maxChildSize: 0.9,
        minChildSize: 0.5,
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
                child: Text(m.name,
                    style: const TextStyle(
                        fontSize: 20, fontWeight: FontWeight.bold)),
              ),
              const SizedBox(height: 4),
              Center(
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                  decoration: BoxDecoration(
                    color: AppTheme.primaryColor.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(m.category,
                      style: const TextStyle(
                          color: AppTheme.primaryColor,
                          fontWeight: FontWeight.w600)),
                ),
              ),
              const SizedBox(height: 20),
              _detailRow('Guardian', m.guardianName),
              _detailRow('Contact', m.guardianContact),
              _detailRow('Join Date',
                  m.joinDate?.toString().substring(0, 10) ?? 'N/A'),
              _detailRow('Price', 'Rs. ${m.price}'),
              _detailRow('Cash Paid', 'Rs. ${m.cashAmount}'),
              _detailRow('eSewa Paid', 'Rs. ${m.esewaAmount}'),
              _detailRow('Due', 'Rs. ${m.dueAmount}'),
              _detailRow('Monthly Classes', m.perMonthClass),
              if (m.remarks != null && m.remarks!.isNotEmpty)
                _detailRow('Remarks', m.remarks!),
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
