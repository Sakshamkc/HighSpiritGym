class User {
  final String id;
  final String username;
  final String? email;
  final String role;
  final int? customerId;
  final String token;
  final DateTime expiresAt;

  User({
    required this.id,
    required this.username,
    this.email,
    required this.role,
    this.customerId,
    required this.token,
    required this.expiresAt,
  });

  bool get isAdmin => role == 'Admin';
  bool get isCustomer => role == 'Customer';

  factory User.fromLoginResponse(Map<String, dynamic> json) {
    return User(
      id: '',
      username: json['username'] ?? '',
      role: json['role'] ?? 'Customer',
      customerId: json['customerId'],
      token: json['token'] ?? '',
      expiresAt: DateTime.tryParse(json['expiresAt'] ?? '') ?? DateTime.now(),
    );
  }

  factory User.fromMeResponse(Map<String, dynamic> json, String token) {
    return User(
      id: json['id'] ?? '',
      username: json['userName'] ?? '',
      email: json['email'],
      role: json['role'] ?? 'Customer',
      customerId: json['customerId'],
      token: token,
      expiresAt: DateTime.now().add(const Duration(days: 30)),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'username': username,
    'email': email,
    'role': role,
    'customerId': customerId,
    'token': token,
    'expiresAt': expiresAt.toIso8601String(),
  };

  factory User.fromJson(Map<String, dynamic> json) {
    return User(
      id: json['id'] ?? '',
      username: json['username'] ?? '',
      email: json['email'],
      role: json['role'] ?? 'Customer',
      customerId: json['customerId'],
      token: json['token'] ?? '',
      expiresAt: DateTime.tryParse(json['expiresAt'] ?? '') ?? DateTime.now(),
    );
  }
}
