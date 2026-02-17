class Customer {
  final int customerID;
  final String fullName;
  final String? phone;
  final String? email;
  final String? address;
  final String? gender;
  final String? bloodGroup;
  final double? weightKG;
  final String? height;
  final String? occupation;
  final DateTime joinDate;
  final DateTime? dateOfBirth;
  final String? photoBase64;
  final String? remarks;
  final String? shift;
  final DateTime? createdAt;
  final String? currentPlan;
  final DateTime? membershipStart;
  final DateTime? membershipExpire;
  final int? paidPrice;
  final int? dueAmount;
  final bool isActive;
  final bool isExpired;

  Customer({
    required this.customerID,
    required this.fullName,
    this.phone,
    this.email,
    this.address,
    this.gender,
    this.bloodGroup,
    this.weightKG,
    this.height,
    this.occupation,
    required this.joinDate,
    this.dateOfBirth,
    this.photoBase64,
    this.remarks,
    this.shift,
    this.createdAt,
    this.currentPlan,
    this.membershipStart,
    this.membershipExpire,
    this.paidPrice,
    this.dueAmount,
    this.isActive = false,
    this.isExpired = false,
  });

  factory Customer.fromJson(Map<String, dynamic> json) {
    return Customer(
      customerID: json['customerID'] ?? 0,
      fullName: json['fullName'] ?? '',
      phone: json['phone'],
      email: json['email'],
      address: json['address'],
      gender: json['gender'],
      bloodGroup: json['bloodGroup'],
      weightKG: (json['weightKG'] as num?)?.toDouble(),
      height: json['height'],
      occupation: json['occupation'],
      joinDate: DateTime.tryParse(json['joinDate'] ?? '') ?? DateTime.now(),
      dateOfBirth: json['dateOfBirth'] != null ? DateTime.tryParse(json['dateOfBirth']) : null,
      photoBase64: json['photoBase64'],
      remarks: json['remarks'],
      shift: json['shift'],
      createdAt: json['createdAt'] != null ? DateTime.tryParse(json['createdAt']) : null,
      currentPlan: json['currentPlan'],
      membershipStart: json['membershipStart'] != null ? DateTime.tryParse(json['membershipStart']) : null,
      membershipExpire: json['membershipExpire'] != null ? DateTime.tryParse(json['membershipExpire']) : null,
      paidPrice: (json['paidPrice'] as num?)?.toInt(),
      dueAmount: (json['dueAmount'] as num?)?.toInt(),
      isActive: json['isActive'] ?? false,
      isExpired: json['isExpired'] ?? false,
    );
  }

  String get statusText {
    // Use expiry date as the source of truth (matches backend filter logic)
    if (membershipExpire != null) {
      final today = DateTime.now();
      final expiry = membershipExpire!;
      if (expiry.isAfter(today) || expiry.year == today.year && expiry.month == today.month && expiry.day == today.day) {
        // Check if expiring within 7 days
        if (expiry.difference(today).inDays <= 7) return 'Expiring Soon';
        return 'Active';
      }
      return 'Expired';
    }
    if (isActive && !isExpired) return 'Active';
    if (isExpired) return 'Expired';
    return 'Inactive';
  }

  bool get isReallyActive {
    if (membershipExpire != null) {
      final today = DateTime.now();
      return membershipExpire!.isAfter(today) || 
        (membershipExpire!.year == today.year && membershipExpire!.month == today.month && membershipExpire!.day == today.day);
    }
    return isActive && !isExpired;
  }
}
