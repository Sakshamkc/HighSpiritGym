class RevenueReport {
  final double totalRevenue;
  final double totalDue;
  final double totalCollected;
  final double gymRevenue;
  final double gymDue;
  final double boxingRevenue;
  final double boxingDue;
  final double lockerRevenue;
  final double lockerDue;
  final double thisMonthRevenue;
  final double lastMonthRevenue;
  final double revenueGrowth;
  final double todayRevenue;
  final int todayTransactions;
  final double totalCash;
  final double totalEsewa;
  final List<MonthlyRevenue> monthlyBreakdown;
  final List<RecentTransaction> recentTransactions;

  RevenueReport({
    required this.totalRevenue,
    required this.totalDue,
    required this.totalCollected,
    required this.gymRevenue,
    required this.gymDue,
    required this.boxingRevenue,
    required this.boxingDue,
    required this.lockerRevenue,
    required this.lockerDue,
    required this.thisMonthRevenue,
    required this.lastMonthRevenue,
    required this.revenueGrowth,
    required this.todayRevenue,
    required this.todayTransactions,
    required this.totalCash,
    required this.totalEsewa,
    required this.monthlyBreakdown,
    required this.recentTransactions,
  });

  // Alias for screen compatibility
  double get monthlyRevenue => thisMonthRevenue;

  factory RevenueReport.fromJson(Map<String, dynamic> json) {
    return RevenueReport(
      totalRevenue: (json['totalRevenue'] as num?)?.toDouble() ?? 0,
      totalDue: (json['totalDue'] as num?)?.toDouble() ?? 0,
      totalCollected: (json['totalCollected'] as num?)?.toDouble() ?? 0,
      gymRevenue: (json['gymRevenue'] as num?)?.toDouble() ?? 0,
      gymDue: (json['gymDue'] as num?)?.toDouble() ?? 0,
      boxingRevenue: (json['boxingRevenue'] as num?)?.toDouble() ?? 0,
      boxingDue: (json['boxingDue'] as num?)?.toDouble() ?? 0,
      lockerRevenue: (json['lockerRevenue'] as num?)?.toDouble() ?? 0,
      lockerDue: (json['lockerDue'] as num?)?.toDouble() ?? 0,
      thisMonthRevenue: (json['thisMonthRevenue'] as num?)?.toDouble() ?? 0,
      lastMonthRevenue: (json['lastMonthRevenue'] as num?)?.toDouble() ?? 0,
      revenueGrowth: (json['revenueGrowth'] as num?)?.toDouble() ?? 0,
      todayRevenue: (json['todayRevenue'] as num?)?.toDouble() ?? 0,
      todayTransactions: json['todayTransactions'] ?? 0,
      totalCash: (json['totalCash'] as num?)?.toDouble() ?? 0,
      totalEsewa: (json['totalEsewa'] as num?)?.toDouble() ?? 0,
      monthlyBreakdown: (json['monthlyBreakdown'] as List?)
              ?.map((e) => MonthlyRevenue.fromJson(e))
              .toList() ??
          [],
      recentTransactions: (json['recentTransactions'] as List?)
              ?.map((e) => RecentTransaction.fromJson(e))
              .toList() ??
          [],
    );
  }
}

class MonthlyRevenue {
  final int month;
  final String monthName;
  final double gymRevenue;
  final double boxingRevenue;
  final double lockerRevenue;
  final double total;

  MonthlyRevenue({
    required this.month,
    required this.monthName,
    required this.gymRevenue,
    required this.boxingRevenue,
    required this.lockerRevenue,
    required this.total,
  });

  factory MonthlyRevenue.fromJson(Map<String, dynamic> json) {
    return MonthlyRevenue(
      month: json['month'] ?? 0,
      monthName: json['monthName'] ?? '',
      gymRevenue: (json['gymRevenue'] as num?)?.toDouble() ?? 0,
      boxingRevenue: (json['boxingRevenue'] as num?)?.toDouble() ?? 0,
      lockerRevenue: (json['lockerRevenue'] as num?)?.toDouble() ?? 0,
      total: (json['total'] as num?)?.toDouble() ?? 0,
    );
  }

  // Alias for screen compatibility
  double get totalRevenue => total;
}

class RecentTransaction {
  final int id;
  final String type;
  final String memberName;
  final String description;
  final double amount;
  final DateTime date;
  final String status;

  RecentTransaction({
    required this.id,
    required this.type,
    required this.memberName,
    required this.description,
    required this.amount,
    required this.date,
    required this.status,
  });

  factory RecentTransaction.fromJson(Map<String, dynamic> json) {
    return RecentTransaction(
      id: json['id'] ?? 0,
      type: json['type'] ?? '',
      memberName: json['memberName'] ?? '',
      description: json['description'] ?? '',
      amount: (json['amount'] as num?)?.toDouble() ?? 0,
      date: DateTime.tryParse(json['date'] ?? '') ?? DateTime.now(),
      status: json['status'] ?? '',
    );
  }

  // Alias for screen compatibility
  String get customerName => memberName;
}
