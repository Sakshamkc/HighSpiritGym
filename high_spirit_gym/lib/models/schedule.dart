class GymSchedule {
  final int scheduleID;
  final String dayOfWeek;
  final String className;
  final String startTime;
  final String endTime;
  final String? instructor;
  final String? description;
  final String category;
  final bool isActive;
  final int sortOrder;

  GymSchedule({
    required this.scheduleID,
    required this.dayOfWeek,
    required this.className,
    required this.startTime,
    required this.endTime,
    this.instructor,
    this.description,
    required this.category,
    required this.isActive,
    required this.sortOrder,
  });

  factory GymSchedule.fromJson(Map<String, dynamic> json) {
    return GymSchedule(
      scheduleID: json['scheduleID'] ?? 0,
      dayOfWeek: json['dayOfWeek'] ?? '',
      className: json['className'] ?? '',
      startTime: json['startTime'] ?? '',
      endTime: json['endTime'] ?? '',
      instructor: json['instructor'],
      description: json['description'],
      category: json['category'] ?? 'General',
      isActive: json['isActive'] ?? true,
      sortOrder: json['sortOrder'] ?? 0,
    );
  }

  Map<String, dynamic> toJson() => {
    'dayOfWeek': dayOfWeek,
    'className': className,
    'startTime': startTime,
    'endTime': endTime,
    'instructor': instructor,
    'description': description,
    'category': category,
    'isActive': isActive,
    'sortOrder': sortOrder,
  };
}
