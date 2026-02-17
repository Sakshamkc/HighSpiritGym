class GymLocker {
  final int lockerID;
  final String lockerNumber;
  final String gender;
  final String status;
  final int? customerID;
  final String? assignedTo;
  final String? assignedPhone;
  final String? package;
  final DateTime? startDate;
  final DateTime? endDate;
  final int rentalMonths;
  final double monthlyRate;
  final double totalAmount;
  final double paidAmount;
  final double dueAmount;
  final String? remarks;
  final bool isExpired;
  final bool isExpiringSoon;
  final int daysRemaining;

  GymLocker({
    required this.lockerID,
    required this.lockerNumber,
    required this.gender,
    required this.status,
    this.customerID,
    this.assignedTo,
    this.assignedPhone,
    this.package,
    this.startDate,
    this.endDate,
    required this.rentalMonths,
    required this.monthlyRate,
    required this.totalAmount,
    required this.paidAmount,
    required this.dueAmount,
    this.remarks,
    required this.isExpired,
    required this.isExpiringSoon,
    required this.daysRemaining,
  });

  factory GymLocker.fromJson(Map<String, dynamic> json) {
    return GymLocker(
      lockerID: json['lockerID'] ?? 0,
      lockerNumber: json['lockerNumber'] ?? '',
      gender: json['gender'] ?? 'Gents',
      status: json['status'] ?? 'Empty',
      customerID: json['customerID'],
      assignedTo: json['assignedTo'],
      assignedPhone: json['assignedPhone'],
      package: json['package'],
      startDate: json['startDate'] != null ? DateTime.tryParse(json['startDate']) : null,
      endDate: json['endDate'] != null ? DateTime.tryParse(json['endDate']) : null,
      rentalMonths: json['rentalMonths'] ?? 0,
      monthlyRate: (json['monthlyRate'] as num?)?.toDouble() ?? 0,
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0,
      paidAmount: (json['paidAmount'] as num?)?.toDouble() ?? 0,
      dueAmount: (json['dueAmount'] as num?)?.toDouble() ?? 0,
      remarks: json['remarks'],
      isExpired: json['isExpired'] ?? false,
      isExpiringSoon: json['isExpiringSoon'] ?? false,
      daysRemaining: json['daysRemaining'] ?? 0,
    );
  }
}
