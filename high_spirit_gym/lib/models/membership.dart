class Membership {
  final int membershipID;
  final int customerID;
  final String? customerName;
  final String? planName;
  final int paidPrice;
  final int dueAmount;
  final int totalPrice;
  final DateTime startDate;
  final int duration;
  final DateTime expireDate;
  final bool isActive;

  Membership({
    required this.membershipID,
    required this.customerID,
    this.customerName,
    this.planName,
    required this.paidPrice,
    required this.dueAmount,
    required this.totalPrice,
    required this.startDate,
    required this.duration,
    required this.expireDate,
    required this.isActive,
  });

  bool get isExpired => expireDate.isBefore(DateTime.now());

  int get daysRemaining {
    final diff = expireDate.difference(DateTime.now()).inDays;
    return diff < 0 ? 0 : diff;
  }

  factory Membership.fromJson(Map<String, dynamic> json) {
    return Membership(
      membershipID: json['membershipID'] ?? 0,
      customerID: json['customerID'] ?? 0,
      customerName: json['customerName'],
      planName: json['planName'],
      paidPrice: json['paidPrice'] ?? 0,
      dueAmount: json['dueAmount'] ?? 0,
      totalPrice: json['totalPrice'] ?? 0,
      startDate: DateTime.tryParse(json['startDate'] ?? '') ?? DateTime.now(),
      duration: json['duration'] ?? 0,
      expireDate: DateTime.tryParse(json['expireDate'] ?? '') ?? DateTime.now(),
      isActive: json['isActive'] ?? false,
    );
  }
}
