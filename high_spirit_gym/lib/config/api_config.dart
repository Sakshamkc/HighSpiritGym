class ApiConfig {
  // Change this to your server's IP address when running on a real device
  // For Android emulator, use 10.0.2.2 instead of localhost
  // For iOS simulator, use localhost
  // For real device, use your computer's local IP or your server's public IP
  // static const String baseUrl = 'http://10.0.2.2:5053/api';
  // static const String baseUrl = 'http://10.0.2.2:5053/api';
  // static const String baseUrl = 'http://localhost:5053/api';
  static const String baseUrl = 'https://127.0.0.1:7041/api';

  // Production URL - uncomment when deploying
  // static const String baseUrl = 'https://your-server.com/api';

  static const Duration timeout = Duration(seconds: 60);

  // Endpoints
  static const String login = '/auth/login';
  static const String register = '/auth/register';
  static const String me = '/auth/me';

  static const String customers = '/customers';
  static const String memberships = '/memberships';
  static const String boxing = '/boxing';
  static const String locker = '/locker';
  static const String dashboard = '/dashboard';
  static const String report = '/report';
  static const String attendance = '/attendance';
  static const String schedule = '/schedule';
}
