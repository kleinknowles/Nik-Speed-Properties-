import 'dart:convert';
import 'dart:io';
import 'models.dart';

class ApiClient {
  ApiClient({String? baseUrl}) : baseUrl = baseUrl ?? const String.fromEnvironment('API_URL', defaultValue: 'http://10.0.2.2:5188');
  final String baseUrl;
  Future<dynamic> _request(String method, String path, {Map<String, dynamic>? body, String? token}) async {
    final client = HttpClient();
    try { final request = await client.openUrl(method, Uri.parse('$baseUrl$path')); request.headers.contentType = ContentType.json; if(token!=null)request.headers.set(HttpHeaders.authorizationHeader,'Bearer $token'); if(body!=null)request.write(jsonEncode(body)); final response=await request.close(); final text=await response.transform(utf8.decoder).join(); final decoded=text.isEmpty?null:jsonDecode(text); if(response.statusCode<200||response.statusCode>299)throw ApiException(decoded is Map?decoded['message']?.toString()??'Request failed':'Request failed'); return decoded; } finally { client.close(); }
  }
  Future<List<Property>> properties({String? type,String? location}) async { final params=<String,String>{};if(type?.isNotEmpty??false)params['type']=type!;if(location?.isNotEmpty??false)params['location']=location!;final query=params.isEmpty?'':'?${Uri(queryParameters:params).query}';final result=await _request('GET','/api/properties$query') as List;return result.map((item)=>Property.fromJson(item)).toList(); }
  Future<Property> property(String id) async => Property.fromJson(await _request('GET','/api/properties/$id'));
  Future<List<ListingPlan>> plans() async => (await _request('GET','/api/plans') as List).map((item)=>ListingPlan.fromJson(item)).toList();
  Future<Map<String,dynamic>> authenticate({required bool register,required String name,required String email,required String password}) async => Map<String,dynamic>.from(await _request('POST','/api/auth/${register?'register':'login'}',body:{'name':name,'email':email,'password':password}));
  Future<void> enquire(Property property,Map<String,String> values) async => _request('POST','/api/enquiries',body:{'propertyId':property.id,...values});
  Future<List<Property>> myListings(String token) async => (await _request('GET','/api/me/listings',token:token) as List).map((item)=>Property.fromJson(item)).toList();
  Future<List<Payment>> payments(String token) async => (await _request('GET','/api/me/payments',token:token) as List).map((item)=>Payment.fromJson(item)).toList();
  Future<Map<String,dynamic>> checkout(String token,ListingPlan plan,String name,String email) async => Map<String,dynamic>.from(await _request('POST','/api/payments/checkout',token:token,body:{'planId':plan.id,'advertiserName':name,'email':email}));
}
class ApiException implements Exception { ApiException(this.message); final String message; @override String toString()=>message; }
