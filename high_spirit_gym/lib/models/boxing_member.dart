class BoxingMember {
  final int boxingMemberID;
  final String name;
  final DateTime? joinDate;
  final String guardianName;
  final String guardianContact;
  final String perMonthClass;
  final int cashAmount;
  final int esewaAmount;
  final int dueAmount;
  final int price;
  final String? remarks;
  final String? photoBase64;
  final String category;
  final DateTime? createdAt;

  BoxingMember({
    required this.boxingMemberID,
    required this.name,
    this.joinDate,
    required this.guardianName,
    required this.guardianContact,
    required this.perMonthClass,
    required this.cashAmount,
    required this.esewaAmount,
    required this.dueAmount,
    required this.price,
    this.remarks,
    this.photoBase64,
    required this.category,
    this.createdAt,
  });

  factory BoxingMember.fromJson(Map<String, dynamic> json) {
    return BoxingMember(
      boxingMemberID: json['boxingMemberID'] ?? 0,
      name: json['name'] ?? '',
      joinDate: json['joinDate'] != null ? DateTime.tryParse(json['joinDate']) : null,
      guardianName: json['guardianName'] ?? '',
      guardianContact: json['guardianContact'] ?? '',
      perMonthClass: json['perMonthClass'] ?? '0+0+0+0',
      cashAmount: json['cashAmount'] ?? 0,
      esewaAmount: json['esewaAmount'] ?? 0,
      dueAmount: json['dueAmount'] ?? 0,
      price: json['price'] ?? 0,
      remarks: json['remarks'],
      photoBase64: json['photoBase64'],
      category: json['category'] ?? 'Children',
      createdAt: json['createdAt'] != null ? DateTime.tryParse(json['createdAt']) : null,
    );
  }
}
