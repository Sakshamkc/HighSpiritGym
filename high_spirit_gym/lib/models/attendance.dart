class Attendance {
  final int attendanceID;
  final int customerID;
  final String customerName;
  final DateTime checkInTime;
  final DateTime? checkOutTime;
  final String? notes;

  Attendance({
    required this.attendanceID,
    required this.customerID,
    required this.customerName,
    required this.checkInTime,
    this.checkOutTime,
    this.notes,
  });

  bool get isCheckedIn => checkOutTime == null;

  String get durationText {
    if (checkOutTime == null) return 'Still in gym';
    final diff = checkOutTime!.difference(checkInTime);
    final hours = diff.inHours;
    final mins = diff.inMinutes % 60;
    if (hours > 0) return '${hours}h ${mins}m';
    return '${mins}m';
  }

  factory Attendance.fromJson(Map<String, dynamic> json) {
    return Attendance(
      attendanceID: json['attendanceID'] ?? 0,
      customerID: json['customerID'] ?? 0,
      customerName: json['customerName'] ?? '',
      checkInTime: DateTime.tryParse(json['checkInTime'] ?? '') ?? DateTime.now(),
      checkOutTime: json['checkOutTime'] != null ? DateTime.tryParse(json['checkOutTime']) : null,
      notes: json['notes'],
    );
  }
}
