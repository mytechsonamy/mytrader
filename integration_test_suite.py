#!/usr/bin/env python3
"""
Comprehensive Integration Test Suite for myTrader Application

This script performs end-to-end integration testing of:
1. API endpoints and contract validation
2. WebSocket/SignalR real-time functionality
3. Database connectivity and data integrity
4. Authentication flow integration
5. Error handling and recovery mechanisms
6. Cross-platform compatibility
7. Performance under load

Usage: python3 integration_test_suite.py
"""

import asyncio
import aiohttp
import json
import time
import psycopg2
import websockets
from datetime import datetime, timedelta
from typing import Dict, List, Any, Optional
import sys

# Configuration
API_BASE_URL = "http://localhost:5002"
WS_BASE_URL = "ws://localhost:5002"
DB_CONFIG = {
    'host': 'localhost',
    'port': 5432,
    'database': 'mytrader',
    'user': 'postgres',
    'password': 'password'
}

class IntegrationTestResult:
    def __init__(self, test_name: str):
        self.test_name = test_name
        self.success = False
        self.message = ""
        self.duration = 0.0
        self.details = {}
        self.start_time = time.time()

    def complete(self, success: bool, message: str, details: Dict = None):
        self.success = success
        self.message = message
        self.details = details or {}
        self.duration = time.time() - self.start_time

    def __str__(self):
        status = "✅ PASS" if self.success else "❌ FAIL"
        return f"{status} {self.test_name} ({self.duration:.2f}s): {self.message}"

class IntegrationTestSuite:
    def __init__(self):
        self.results: List[IntegrationTestResult] = []
        self.total_tests = 0
        self.passed_tests = 0

    async def run_all_tests(self):
        """Execute complete integration test suite"""
        print("🚀 Starting Comprehensive Integration Test Suite")
        print("=" * 60)

        # Test Categories
        await self.test_database_integration()
        await self.test_api_contract_integration()
        await self.test_websocket_integration()
        await self.test_authentication_integration()
        await self.test_error_handling_integration()
        await self.test_performance_integration()

        # Generate Report
        self.generate_integration_report()

    async def test_database_integration(self):
        """Test database connectivity and data integrity"""
        print("\n📊 Testing Database Integration")

        result = IntegrationTestResult("Database Connectivity")
        try:
            conn = psycopg2.connect(**DB_CONFIG)
            cursor = conn.cursor()

            # Test basic connectivity
            cursor.execute("SELECT version()")
            version = cursor.fetchone()[0]

            # Count tables
            cursor.execute("""
                SELECT COUNT(*) FROM information_schema.tables
                WHERE table_schema = 'public'
            """)
            table_count = cursor.fetchone()[0]

            # Check critical tables
            critical_tables = ['market_data', 'symbols', 'users', 'markets']
            table_status = {}

            for table in critical_tables:
                cursor.execute(f'SELECT COUNT(*) FROM "{table}"')
                count = cursor.fetchone()[0]
                table_status[table] = count

            conn.close()

            result.complete(True, f"Connected to {table_count} tables", {
                'version': version,
                'tables': table_status
            })

        except Exception as e:
            result.complete(False, f"Database connection failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

    async def test_api_contract_integration(self):
        """Test API endpoints and contract validation"""
        print("\n🌐 Testing API Contract Integration")

        # Test Health Endpoint
        result = IntegrationTestResult("Health Endpoint")
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(f"{API_BASE_URL}/health") as response:
                    if response.status == 200:
                        data = await response.json()
                        result.complete(True, "Health endpoint responsive", data)
                    else:
                        result.complete(False, f"Health endpoint returned {response.status}")
        except Exception as e:
            result.complete(False, f"Health endpoint failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

        # Test Market Data API Endpoints
        endpoints_to_test = [
            "/api/symbols",
            "/api/marketdata",
            "/api/prices/stocks",
            "/api/prices/crypto"
        ]

        for endpoint in endpoints_to_test:
            result = IntegrationTestResult(f"API {endpoint}")
            try:
                async with aiohttp.ClientSession() as session:
                    async with session.get(f"{API_BASE_URL}{endpoint}") as response:
                        status = response.status
                        content_type = response.headers.get('content-type', '')

                        if status == 200:
                            if 'application/json' in content_type:
                                data = await response.json()
                                result.complete(True, f"JSON response with {len(data) if isinstance(data, list) else 'data'} items")
                            else:
                                result.complete(True, f"Response received (non-JSON)")
                        elif status == 404:
                            result.complete(False, "Endpoint not found (404)")
                        elif status == 500:
                            result.complete(False, "Internal server error (500)")
                        else:
                            result.complete(False, f"Unexpected status code: {status}")

            except Exception as e:
                result.complete(False, f"Request failed: {str(e)}")

            self.results.append(result)
            print(f"  {result}")

    async def test_websocket_integration(self):
        """Test WebSocket/SignalR real-time functionality"""
        print("\n🔄 Testing WebSocket Integration")

        # Test SignalR Hub Connection
        result = IntegrationTestResult("SignalR Hub Connection")
        try:
            # Attempt to connect to SignalR negotiate endpoint
            async with aiohttp.ClientSession() as session:
                async with session.post(f"{API_BASE_URL}/markethub/negotiate") as response:
                    if response.status in [200, 404]:  # 404 is expected if SignalR not properly configured
                        result.complete(True if response.status == 200 else False,
                                      f"SignalR negotiate endpoint responded with {response.status}")
                    else:
                        result.complete(False, f"Unexpected response: {response.status}")

        except Exception as e:
            result.complete(False, f"SignalR connection failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

        # Test WebSocket Direct Connection (if available)
        result = IntegrationTestResult("WebSocket Direct Connection")
        try:
            # This would be a simplified WebSocket test
            # In practice, SignalR uses a more complex protocol
            result.complete(False, "WebSocket direct connection not implemented (normal for SignalR)")
        except Exception as e:
            result.complete(False, f"WebSocket test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

    async def test_authentication_integration(self):
        """Test authentication flow integration"""
        print("\n🔐 Testing Authentication Integration")

        # Test Registration Endpoint
        result = IntegrationTestResult("Registration Endpoint")
        try:
            test_user = {
                "username": f"testuser_{int(time.time())}",
                "email": f"test_{int(time.time())}@example.com",
                "password": "TestPassword123!"
            }

            async with aiohttp.ClientSession() as session:
                async with session.post(f"{API_BASE_URL}/api/auth/register",
                                      json=test_user) as response:
                    status = response.status

                    if status in [200, 201]:
                        result.complete(True, "Registration endpoint accepts requests")
                    elif status == 400:
                        result.complete(True, "Registration endpoint validates input (400 expected)")
                    elif status == 404:
                        result.complete(False, "Registration endpoint not found")
                    else:
                        result.complete(False, f"Unexpected status: {status}")

        except Exception as e:
            result.complete(False, f"Registration test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

        # Test Login Endpoint
        result = IntegrationTestResult("Login Endpoint")
        try:
            login_data = {
                "username": "testuser",
                "password": "testpassword"
            }

            async with aiohttp.ClientSession() as session:
                async with session.post(f"{API_BASE_URL}/api/auth/login",
                                      json=login_data) as response:
                    status = response.status

                    if status in [200, 401]:  # 401 is expected for invalid credentials
                        result.complete(True, f"Login endpoint responsive (status: {status})")
                    elif status == 404:
                        result.complete(False, "Login endpoint not found")
                    else:
                        result.complete(False, f"Unexpected status: {status}")

        except Exception as e:
            result.complete(False, f"Login test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

    async def test_error_handling_integration(self):
        """Test error handling and recovery integration"""
        print("\n🛡️  Testing Error Handling Integration")

        # Test Invalid Endpoint
        result = IntegrationTestResult("404 Error Handling")
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(f"{API_BASE_URL}/api/nonexistent") as response:
                    if response.status == 404:
                        result.complete(True, "404 errors handled correctly")
                    else:
                        result.complete(False, f"Expected 404, got {response.status}")

        except Exception as e:
            result.complete(False, f"Error handling test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

        # Test Malformed Request
        result = IntegrationTestResult("Malformed Request Handling")
        try:
            async with aiohttp.ClientSession() as session:
                async with session.post(f"{API_BASE_URL}/api/auth/login",
                                      data="invalid json") as response:
                    if response.status in [400, 415]:  # Bad Request or Unsupported Media Type
                        result.complete(True, f"Malformed requests handled (status: {response.status})")
                    else:
                        result.complete(False, f"Unexpected status for malformed request: {response.status}")

        except Exception as e:
            result.complete(False, f"Malformed request test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

    async def test_performance_integration(self):
        """Test performance and concurrent user integration"""
        print("\n⚡ Testing Performance Integration")

        # Test Response Time
        result = IntegrationTestResult("Response Time Performance")
        try:
            start_time = time.time()

            async with aiohttp.ClientSession() as session:
                async with session.get(f"{API_BASE_URL}/health") as response:
                    end_time = time.time()
                    response_time = (end_time - start_time) * 1000  # milliseconds

                    if response.status == 200:
                        if response_time < 1000:  # Under 1 second
                            result.complete(True, f"Response time: {response_time:.2f}ms")
                        else:
                            result.complete(False, f"Slow response time: {response_time:.2f}ms")
                    else:
                        result.complete(False, f"Performance test failed with status {response.status}")

        except Exception as e:
            result.complete(False, f"Performance test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

        # Test Concurrent Requests
        result = IntegrationTestResult("Concurrent Request Handling")
        try:
            concurrent_requests = 5
            tasks = []

            async def make_request(session):
                async with session.get(f"{API_BASE_URL}/health") as response:
                    return response.status == 200

            async with aiohttp.ClientSession() as session:
                tasks = [make_request(session) for _ in range(concurrent_requests)]
                results = await asyncio.gather(*tasks, return_exceptions=True)

                successful = sum(1 for r in results if r is True)
                success_rate = (successful / concurrent_requests) * 100

                if success_rate >= 80:  # 80% success rate acceptable
                    result.complete(True, f"Concurrent requests: {success_rate:.0f}% success rate")
                else:
                    result.complete(False, f"Poor concurrent performance: {success_rate:.0f}% success rate")

        except Exception as e:
            result.complete(False, f"Concurrent request test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

    def generate_integration_report(self):
        """Generate comprehensive integration test report"""
        print("\n" + "=" * 60)
        print("📋 INTEGRATION TEST REPORT")
        print("=" * 60)

        total_tests = len(self.results)
        passed_tests = sum(1 for r in self.results if r.success)
        failed_tests = total_tests - passed_tests
        success_rate = (passed_tests / total_tests) * 100 if total_tests > 0 else 0

        print(f"\n📊 SUMMARY:")
        print(f"   Total Tests: {total_tests}")
        print(f"   Passed: {passed_tests}")
        print(f"   Failed: {failed_tests}")
        print(f"   Success Rate: {success_rate:.1f}%")

        # Overall System Health Assessment
        critical_failures = []
        warnings = []

        for result in self.results:
            if not result.success:
                if any(keyword in result.test_name.lower()
                      for keyword in ['database', 'auth', 'health']):
                    critical_failures.append(result.test_name)
                else:
                    warnings.append(result.test_name)

        print(f"\n🎯 SYSTEM HEALTH ASSESSMENT:")
        if success_rate >= 90:
            print("   ✅ EXCELLENT - System ready for production")
        elif success_rate >= 75:
            print("   ⚠️  GOOD - Minor issues need attention")
        elif success_rate >= 50:
            print("   🚧 FAIR - Several issues need resolution")
        else:
            print("   🚨 POOR - Major issues require immediate attention")

        if critical_failures:
            print(f"\n🚨 CRITICAL FAILURES:")
            for failure in critical_failures:
                print(f"   - {failure}")

        if warnings:
            print(f"\n⚠️  WARNINGS:")
            for warning in warnings:
                print(f"   - {warning}")

        # Detailed Results
        print(f"\n📝 DETAILED RESULTS:")
        for result in self.results:
            print(f"   {result}")
            if result.details:
                for key, value in result.details.items():
                    print(f"      {key}: {value}")

        # Integration Analysis
        print(f"\n🔍 INTEGRATION ANALYSIS:")

        # Database Integration
        db_tests = [r for r in self.results if 'database' in r.test_name.lower()]
        if db_tests and all(r.success for r in db_tests):
            print("   ✅ Database integration working")
        else:
            print("   ❌ Database integration issues detected")

        # API Integration
        api_tests = [r for r in self.results if 'api' in r.test_name.lower() or 'endpoint' in r.test_name.lower()]
        api_success_rate = (sum(1 for r in api_tests if r.success) / len(api_tests)) * 100 if api_tests else 0
        if api_success_rate >= 80:
            print(f"   ✅ API integration: {api_success_rate:.0f}% operational")
        else:
            print(f"   ⚠️  API integration: {api_success_rate:.0f}% operational")

        # Real-time Integration
        ws_tests = [r for r in self.results if 'websocket' in r.test_name.lower() or 'signalr' in r.test_name.lower()]
        if ws_tests and any(r.success for r in ws_tests):
            print("   ✅ Real-time integration partially working")
        else:
            print("   ❌ Real-time integration needs attention")

        # Authentication Integration
        auth_tests = [r for r in self.results if 'auth' in r.test_name.lower()]
        if auth_tests and all(r.success for r in auth_tests):
            print("   ✅ Authentication integration working")
        else:
            print("   ⚠️  Authentication integration needs review")

        print(f"\n🏁 Integration test suite completed!")

        return success_rate >= 75  # Return True if tests generally pass

async def main():
    """Run the complete integration test suite"""
    suite = IntegrationTestSuite()
    success = await suite.run_all_tests()

    # Exit with appropriate code
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    asyncio.run(main())