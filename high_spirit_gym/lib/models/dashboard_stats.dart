class DashboardStats {
  // Gym stats
  final int gymTotal;
  final int gymActive;
  final int gymExpired;
  final int gymExpiringSoon;
  final int gymJoinedThisMonth;
  final int gymTotalDue;

  // Locker Gents
  final int lockerGentsTotal;
  final int lockerGentsOccupied;
  final int lockerGentsEmpty;
  final int lockerGentsExpired;

  // Locker Ladies
  final int lockerLadiesTotal;
  final int lockerLadiesOccupied;
  final int lockerLadiesEmpty;
  final int lockerLadiesExpired;

  final int lockerTotalDue;

  // Boxing
  final int boxingTotal;
  final int boxingPaid;
  final int boxingWithDue;
  final int boxingTotalDue;

  DashboardStats({
    required this.gymTotal,
    required this.gymActive,
    required this.gymExpired,
    required this.gymExpiringSoon,
    required this.gymJoinedThisMonth,
    required this.gymTotalDue,
    required this.lockerGentsTotal,
    required this.lockerGentsOccupied,
    required this.lockerGentsEmpty,
    required this.lockerGentsExpired,
    required this.lockerLadiesTotal,
    required this.lockerLadiesOccupied,
    required this.lockerLadiesEmpty,
    required this.lockerLadiesExpired,
    required this.lockerTotalDue,
    required this.boxingTotal,
    required this.boxingPaid,
    required this.boxingWithDue,
    required this.boxingTotalDue,
  });

  factory DashboardStats.fromJson(Map<String, dynamic> json) {
    return DashboardStats(
      gymTotal: json['gymTotal'] ?? 0,
      gymActive: json['gymActive'] ?? 0,
      gymExpired: json['gymExpired'] ?? 0,
      gymExpiringSoon: json['gymExpiringSoon'] ?? 0,
      gymJoinedThisMonth: json['gymJoinedThisMonth'] ?? 0,
      gymTotalDue: json['gymTotalDue'] ?? 0,
      lockerGentsTotal: json['lockerGentsTotal'] ?? 0,
      lockerGentsOccupied: json['lockerGentsOccupied'] ?? 0,
      lockerGentsEmpty: json['lockerGentsEmpty'] ?? 0,
      lockerGentsExpired: json['lockerGentsExpired'] ?? 0,
      lockerLadiesTotal: json['lockerLadiesTotal'] ?? 0,
      lockerLadiesOccupied: json['lockerLadiesOccupied'] ?? 0,
      lockerLadiesEmpty: json['lockerLadiesEmpty'] ?? 0,
      lockerLadiesExpired: json['lockerLadiesExpired'] ?? 0,
      lockerTotalDue: json['lockerTotalDue'] ?? 0,
      boxingTotal: json['boxingTotal'] ?? 0,
      boxingPaid: json['boxingPaid'] ?? 0,
      boxingWithDue: json['boxingWithDue'] ?? 0,
      boxingTotalDue: json['boxingTotalDue'] ?? 0,
    );
  }
}
