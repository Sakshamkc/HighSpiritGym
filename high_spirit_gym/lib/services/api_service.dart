import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:high_spirit_gym/config/api_config.dart';

class ApiService {
  String? _token;

  void setToken(String token) => _token = token;
  void clearToken() => _token = null;

  Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    if (_token != null) 'Authorization': 'Bearer $_token',
  };

  // ======================== GET ========================
  Future<Map<String, dynamic>> get(String endpoint, {Map<String, String>? query}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}$endpoint')
        .replace(queryParameters: query);
    print('GET: $uri');
    try {
      final response = await http.get(uri, headers: _headers)
          .timeout(ApiConfig.timeout);
      print('Response: ${response.statusCode} ${response.body}');
      return _handleResponse(response);
    } catch (e) {
      print('GET Error: $e');
      rethrow;
    }
  }

  // ======================== POST ========================
  Future<Map<String, dynamic>> post(String endpoint, {dynamic body}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}$endpoint');
    final response = await http.post(
      uri,
      headers: _headers,
      body: body != null ? jsonEncode(body) : null,
    ).timeout(ApiConfig.timeout);
    return _handleResponse(response);
  }

  // ======================== PUT ========================
  Future<Map<String, dynamic>> put(String endpoint, {dynamic body}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}$endpoint');
    final response = await http.put(
      uri,
      headers: _headers,
      body: body != null ? jsonEncode(body) : null,
    ).timeout(ApiConfig.timeout);
    return _handleResponse(response);
  }

  // ======================== DELETE ========================
  Future<Map<String, dynamic>> delete(String endpoint) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}$endpoint');
    final response = await http.delete(uri, headers: _headers)
        .timeout(ApiConfig.timeout);
    return _handleResponse(response);
  }

  // ======================== RESPONSE HANDLER ========================
  Map<String, dynamic> _handleResponse(http.Response response) {
    final body = jsonDecode(response.body) as Map<String, dynamic>;

    if (response.statusCode >= 200 && response.statusCode < 300) {
      return body;
    }

    throw ApiException(
      statusCode: response.statusCode,
      message: body['message'] ?? 'An error occurred',
    );
  }
}

class ApiException implements Exception {
  final int statusCode;
  final String message;

  ApiException({required this.statusCode, required this.message});

  @override
  String toString() => message;
}
