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
      gymTotal: (json['gymTotal'] as num?)?.toInt() ?? 0,
      gymActive: (json['gymActive'] as num?)?.toInt() ?? 0,
      gymExpired: (json['gymExpired'] as num?)?.toInt() ?? 0,
      gymExpiringSoon: (json['gymExpiringSoon'] as num?)?.toInt() ?? 0,
      gymJoinedThisMonth: (json['gymJoinedThisMonth'] as num?)?.toInt() ?? 0,
      gymTotalDue: (json['gymTotalDue'] as num?)?.toInt() ?? 0,
      lockerGentsTotal: (json['lockerGentsTotal'] as num?)?.toInt() ?? 0,
      lockerGentsOccupied: (json['lockerGentsOccupied'] as num?)?.toInt() ?? 0,
      lockerGentsEmpty: (json['lockerGentsEmpty'] as num?)?.toInt() ?? 0,
      lockerGentsExpired: (json['lockerGentsExpired'] as num?)?.toInt() ?? 0,
      lockerLadiesTotal: (json['lockerLadiesTotal'] as num?)?.toInt() ?? 0,
      lockerLadiesOccupied: (json['lockerLadiesOccupied'] as num?)?.toInt() ?? 0,
      lockerLadiesEmpty: (json['lockerLadiesEmpty'] as num?)?.toInt() ?? 0,
      lockerLadiesExpired: (json['lockerLadiesExpired'] as num?)?.toInt() ?? 0,
      lockerTotalDue: (json['lockerTotalDue'] as num?)?.toInt() ?? 0,
      boxingTotal: (json['boxingTotal'] as num?)?.toInt() ?? 0,
      boxingPaid: (json['boxingPaid'] as num?)?.toInt() ?? 0,
      boxingWithDue: (json['boxingWithDue'] as num?)?.toInt() ?? 0,
      boxingTotalDue: (json['boxingTotalDue'] as num?)?.toInt() ?? 0,
    );
  }
}
