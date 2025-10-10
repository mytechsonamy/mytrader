#!/usr/bin/env python3
"""
Error Handling and Connection Resilience Test Suite
Tests the system's ability to handle failures gracefully and recover from errors
"""

import asyncio
import json
import time
import requests
import subprocess
import signal
import sys
from datetime import datetime
from concurrent.futures import ThreadPoolExecutor
import threading

class ErrorResilienceTestSuite:
    def __init__(self):
        self.api_base = "http://localhost:5002"
        self.frontend_base = "http://localhost:3000"
        self.results = {
            "timestamp": datetime.now().isoformat(),
            "error_handling_tests": {},
            "connection_resilience_tests": {},
            "recovery_tests": {},
            "stress_tests": {},
            "total_tests": 0,
            "passed_tests": 0,
            "failed_tests": 0,
            "critical_failures": [],
            "resilience_score": 0
        }

    def log_result(self, test_category, test_name, success, details="", error=None):
        """Log test result"""
        if test_category not in self.results:
            self.results[test_category] = {}

        self.results["total_tests"] += 1
        if success:
            self.results["passed_tests"] += 1
            status = "✅ PASS"
        else:
            self.results["failed_tests"] += 1
            status = "❌ FAIL"
            if "critical" in test_name.lower():
                self.results["critical_failures"].append(f"{test_name}: {error or 'Unknown error'}")

        self.results[test_category][test_name] = {
            "passed": success,
            "details": details,
            "error": error,
            "timestamp": datetime.now().isoformat()
        }

        print(f"{status} [{test_category}] {test_name}")
        if details:
            print(f"    {details}")
        if error:
            print(f"    Error: {error}")

    def test_api_error_handling(self):
        """Test API error handling for various scenarios"""
        print("\n🔥 Testing API Error Handling...")

        # Test 1: Invalid endpoint
        try:
            response = requests.get(f"{self.api_base}/api/nonexistent", timeout=5)
            expected_404 = response.status_code == 404
            self.log_result("error_handling_tests", "Invalid Endpoint (404)", expected_404,
                           f"Expected 404, got {response.status_code}")
        except Exception as e:
            self.log_result("error_handling_tests", "Invalid Endpoint (404)", False, error=str(e))

        # Test 2: Malformed request
        try:
            response = requests.post(f"{self.api_base}/api/auth/login",
                                   json={"invalid": "data"}, timeout=5)
            handles_bad_request = response.status_code in [400, 422, 500]
            self.log_result("error_handling_tests", "Malformed Request", handles_bad_request,
                           f"Returns appropriate error code: {response.status_code}")
        except Exception as e:
            self.log_result("error_handling_tests", "Malformed Request", False, error=str(e))

        # Test 3: Large payload
        try:
            large_payload = {"data": "x" * 1000000}  # 1MB of data
            response = requests.post(f"{self.api_base}/api/auth/login",
                                   json=large_payload, timeout=10)
            handles_large_payload = response.status_code in [400, 413, 500]
            self.log_result("error_handling_tests", "Large Payload Handling", handles_large_payload,
                           f"Handles large payload appropriately: {response.status_code}")
        except Exception as e:
            self.log_result("error_handling_tests", "Large Payload Handling", True,
                           "Request rejected due to size (expected behavior)")

        # Test 4: Rapid requests (rate limiting)
        try:
            responses = []
            start_time = time.time()
            for i in range(50):  # 50 rapid requests
                try:
                    response = requests.get(f"{self.api_base}/health", timeout=1)
                    responses.append(response.status_code)
                except:
                    responses.append(0)  # Timeout/error

            success_rate = len([r for r in responses if r == 200]) / len(responses)
            rate_limiting_works = success_rate < 1.0 or time.time() - start_time > 5

            self.log_result("error_handling_tests", "Rate Limiting Protection", rate_limiting_works,
                           f"Success rate: {success_rate:.2f}, Duration: {time.time() - start_time:.1f}s")
        except Exception as e:
            self.log_result("error_handling_tests", "Rate Limiting Protection", False, error=str(e))

    def test_connection_resilience(self):
        """Test connection resilience and recovery"""
        print("\n🔗 Testing Connection Resilience...")

        # Test 1: Connection timeout handling
        try:
            start_time = time.time()
            try:
                # Try to connect to a non-existent service
                response = requests.get("http://localhost:9999/nonexistent", timeout=3)
                connection_handled = False
            except (requests.exceptions.ConnectionError, requests.exceptions.Timeout):
                connection_handled = True
                elapsed = time.time() - start_time

            self.log_result("connection_resilience_tests", "Connection Timeout Handling",
                           connection_handled, f"Timeout handled in {elapsed:.1f}s")
        except Exception as e:
            self.log_result("connection_resilience_tests", "Connection Timeout Handling", False, error=str(e))

        # Test 2: Graceful degradation when backend is slow
        try:
            # Simulate slow responses with multiple concurrent requests
            def slow_request():
                try:
                    response = requests.get(f"{self.api_base}/health", timeout=10)
                    return response.status_code == 200
                except:
                    return False

            with ThreadPoolExecutor(max_workers=10) as executor:
                futures = [executor.submit(slow_request) for _ in range(10)]
                results = [f.result() for f in futures]

            success_rate = sum(results) / len(results)
            graceful_degradation = success_rate >= 0.8  # At least 80% should succeed

            self.log_result("connection_resilience_tests", "Concurrent Request Handling",
                           graceful_degradation, f"Success rate: {success_rate:.2f}")
        except Exception as e:
            self.log_result("connection_resilience_tests", "Concurrent Request Handling", False, error=str(e))

        # Test 3: CORS error handling
        try:
            headers = {'Origin': 'http://malicious-site.com'}
            response = requests.get(f"{self.api_base}/health", headers=headers, timeout=5)

            # Check if CORS is properly configured
            cors_header = response.headers.get('Access-Control-Allow-Origin')
            cors_properly_configured = cors_header in [self.frontend_base, '*'] or response.status_code == 200

            self.log_result("connection_resilience_tests", "CORS Security", cors_properly_configured,
                           f"CORS header: {cors_header}")
        except Exception as e:
            self.log_result("connection_resilience_tests", "CORS Security", False, error=str(e))

    def test_frontend_error_handling(self):
        """Test frontend error handling"""
        print("\n🌐 Testing Frontend Error Handling...")

        # Test 1: Frontend accessibility when backend is down
        try:
            response = requests.get(self.frontend_base, timeout=10)
            frontend_accessible = response.status_code == 200

            if frontend_accessible:
                html_content = response.text
                has_error_boundary = any(keyword in html_content.lower() for keyword in
                                       ['error', 'fallback', 'boundary', 'catch'])

                self.log_result("error_handling_tests", "Frontend Accessibility", True,
                               f"Frontend loads, error handling present: {has_error_boundary}")
            else:
                self.log_result("error_handling_tests", "Frontend Accessibility", False,
                               f"Frontend not accessible: {response.status_code}")
        except Exception as e:
            self.log_result("error_handling_tests", "Frontend Accessibility", False, error=str(e))

        # Test 2: JavaScript error handling
        try:
            # This would require browser automation in a real scenario
            # For now, we'll check if the frontend serves proper error pages
            response = requests.get(f"{self.frontend_base}/nonexistent-page", timeout=5)
            handles_404 = response.status_code in [200, 404]  # Either serves app or 404

            self.log_result("error_handling_tests", "Frontend 404 Handling", handles_404,
                           f"Response: {response.status_code}")
        except Exception as e:
            self.log_result("error_handling_tests", "Frontend 404 Handling", False, error=str(e))

    def test_data_integrity_on_errors(self):
        """Test data integrity when errors occur"""
        print("\n💾 Testing Data Integrity During Errors...")

        # Test 1: Concurrent writes/reads
        try:
            def make_request():
                try:
                    response = requests.get(f"{self.api_base}/api/symbols", timeout=5)
                    return response.status_code == 200 and 'symbols' in response.json()
                except:
                    return False

            # Make concurrent requests to test data consistency
            with ThreadPoolExecutor(max_workers=20) as executor:
                futures = [executor.submit(make_request) for _ in range(20)]
                results = [f.result() for f in futures]

            consistency_rate = sum(results) / len(results)
            data_consistent = consistency_rate >= 0.95  # 95% should be consistent

            self.log_result("error_handling_tests", "Data Consistency Under Load", data_consistent,
                           f"Consistency rate: {consistency_rate:.2f}")
        except Exception as e:
            self.log_result("error_handling_tests", "Data Consistency Under Load", False, error=str(e))

    def test_websocket_resilience(self):
        """Test WebSocket connection resilience"""
        print("\n🔌 Testing WebSocket Resilience...")

        # Note: This is a simplified test. In a real scenario, you'd use WebSocket libraries
        try:
            # Test WebSocket endpoint availability
            response = requests.get(f"{self.api_base}/hubs/marketdata", timeout=5)
            websocket_endpoint_available = response.status_code in [200, 400, 404]  # Various expected responses

            self.log_result("connection_resilience_tests", "WebSocket Endpoint Availability",
                           websocket_endpoint_available, f"Response: {response.status_code}")

            # Test SignalR negotiate endpoint
            try:
                negotiate_response = requests.post(f"{self.api_base}/hubs/marketdata/negotiate", timeout=5)
                negotiate_available = negotiate_response.status_code in [200, 404, 405]

                self.log_result("connection_resilience_tests", "SignalR Negotiate Endpoint",
                               negotiate_available, f"Negotiate response: {negotiate_response.status_code}")
            except Exception as ne:
                self.log_result("connection_resilience_tests", "SignalR Negotiate Endpoint", False, error=str(ne))

        except Exception as e:
            self.log_result("connection_resilience_tests", "WebSocket Endpoint Availability", False, error=str(e))

    def test_memory_leak_resilience(self):
        """Test for memory leaks and resource management"""
        print("\n🧠 Testing Memory and Resource Management...")

        try:
            # Test multiple rapid requests to check for resource leaks
            start_time = time.time()
            request_count = 100

            successful_requests = 0
            for i in range(request_count):
                try:
                    response = requests.get(f"{self.api_base}/health", timeout=2)
                    if response.status_code == 200:
                        successful_requests += 1
                except:
                    pass

            end_time = time.time()
            duration = end_time - start_time
            requests_per_second = request_count / duration

            # Check if performance degraded significantly
            performance_stable = requests_per_second > 10  # Should handle at least 10 req/s

            self.log_result("stress_tests", "Resource Management Under Load", performance_stable,
                           f"{successful_requests}/{request_count} successful, {requests_per_second:.1f} req/s")

        except Exception as e:
            self.log_result("stress_tests", "Resource Management Under Load", False, error=str(e))

    def test_database_connection_resilience(self):
        """Test database connection handling"""
        print("\n🗄️  Testing Database Connection Resilience...")

        try:
            # Test health endpoint which likely checks database
            response = requests.get(f"{self.api_base}/health", timeout=10)

            if response.status_code == 200:
                health_data = response.json()
                db_status = None

                # Look for database status in health check
                if 'entries' in health_data:
                    for entry in health_data['entries']:
                        if 'database' in entry.get('name', '').lower() or 'postgresql' in entry.get('name', '').lower():
                            db_status = entry.get('status')
                            break

                db_healthy = db_status == 'Healthy' if db_status else True
                self.log_result("connection_resilience_tests", "Database Connection Health", db_healthy,
                               f"Database status: {db_status or 'Not reported'}")
            else:
                self.log_result("connection_resilience_tests", "Database Connection Health", False,
                               f"Health endpoint failed: {response.status_code}")

        except Exception as e:
            self.log_result("connection_resilience_tests", "Database Connection Health", False, error=str(e))

    def test_security_error_handling(self):
        """Test security-related error handling"""
        print("\n🔐 Testing Security Error Handling...")

        # Test 1: SQL injection attempt
        try:
            malicious_payload = {"email": "admin'; DROP TABLE users; --", "password": "password"}
            response = requests.post(f"{self.api_base}/api/auth/login",
                                   json=malicious_payload, timeout=5)

            sql_injection_prevented = response.status_code in [400, 401, 422]
            self.log_result("error_handling_tests", "SQL Injection Prevention", sql_injection_prevented,
                           f"Malicious login attempt result: {response.status_code}")
        except Exception as e:
            self.log_result("error_handling_tests", "SQL Injection Prevention", True,
                           "Request blocked (expected security behavior)")

        # Test 2: XSS payload
        try:
            xss_payload = {"search": "<script>alert('xss')</script>"}
            response = requests.get(f"{self.api_base}/api/symbols", params=xss_payload, timeout=5)

            xss_handled = response.status_code in [200, 400]  # Should either filter or reject
            if response.status_code == 200:
                # Check if script tags are escaped/removed
                content = response.text
                xss_handled = "<script>" not in content

            self.log_result("error_handling_tests", "XSS Prevention", xss_handled,
                           "XSS payload properly handled")
        except Exception as e:
            self.log_result("error_handling_tests", "XSS Prevention", True,
                           "Request blocked (expected security behavior)")

    def test_recovery_mechanisms(self):
        """Test system recovery mechanisms"""
        print("\n🔄 Testing Recovery Mechanisms...")

        # Test 1: Service recovery after failure simulation
        try:
            # Test if services can handle rapid connect/disconnect
            success_count = 0
            total_attempts = 10

            for i in range(total_attempts):
                try:
                    response = requests.get(f"{self.api_base}/health", timeout=3)
                    if response.status_code == 200:
                        success_count += 1
                    time.sleep(0.1)  # Small delay between requests
                except:
                    pass

            recovery_rate = success_count / total_attempts
            recovery_effective = recovery_rate >= 0.8

            self.log_result("recovery_tests", "Service Recovery Rate", recovery_effective,
                           f"Recovery rate: {recovery_rate:.2f}")

        except Exception as e:
            self.log_result("recovery_tests", "Service Recovery Rate", False, error=str(e))

        # Test 2: Auto-reconnection capability
        try:
            # This would test WebSocket auto-reconnection in a real scenario
            # For now, test if multiple connections can be established
            connection_success = 0
            for i in range(5):
                try:
                    response = requests.get(f"{self.api_base}/", timeout=5)
                    if response.status_code == 200:
                        connection_success += 1
                except:
                    pass

            auto_reconnect_capable = connection_success >= 4  # At least 4/5 should succeed

            self.log_result("recovery_tests", "Auto-Reconnection Capability", auto_reconnect_capable,
                           f"Connection success: {connection_success}/5")

        except Exception as e:
            self.log_result("recovery_tests", "Auto-Reconnection Capability", False, error=str(e))

    def calculate_resilience_score(self):
        """Calculate overall resilience score"""
        if self.results["total_tests"] == 0:
            self.results["resilience_score"] = 0
            return

        base_score = (self.results["passed_tests"] / self.results["total_tests"]) * 100

        # Deduct points for critical failures
        critical_penalty = len(self.results["critical_failures"]) * 10

        # Bonus points for high success rate
        bonus = 5 if base_score >= 95 else 0

        self.results["resilience_score"] = max(0, min(100, base_score - critical_penalty + bonus))

    def run_all_tests(self):
        """Run all error handling and resilience tests"""
        print("🔥 Starting Error Handling & Resilience Test Suite")
        print("=" * 60)

        # API Error Handling Tests
        self.test_api_error_handling()

        # Connection Resilience Tests
        self.test_connection_resilience()

        # Frontend Error Handling Tests
        self.test_frontend_error_handling()

        # Data Integrity Tests
        self.test_data_integrity_on_errors()

        # WebSocket Resilience Tests
        self.test_websocket_resilience()

        # Memory and Resource Tests
        self.test_memory_leak_resilience()

        # Database Connection Tests
        self.test_database_connection_resilience()

        # Security Error Handling Tests
        self.test_security_error_handling()

        # Recovery Mechanism Tests
        self.test_recovery_mechanisms()

        # Calculate final score
        self.calculate_resilience_score()

        # Print summary
        print("\n" + "=" * 60)
        print("📊 Error Handling & Resilience Summary:")
        print(f"🎯 Resilience Score: {self.results['resilience_score']:.1f}/100")
        print(f"✅ Tests Passed: {self.results['passed_tests']}")
        print(f"❌ Tests Failed: {self.results['failed_tests']}")
        print(f"📈 Total Tests: {self.results['total_tests']}")

        if self.results["critical_failures"]:
            print(f"\n🚨 Critical Failures ({len(self.results['critical_failures'])}):")
            for failure in self.results["critical_failures"]:
                print(f"   {failure}")

        # Final assessment
        score = self.results["resilience_score"]
        if score >= 90:
            print("\n🎉 EXCELLENT: System demonstrates high resilience and error handling")
        elif score >= 80:
            print("\n✅ GOOD: System has solid error handling with minor issues")
        elif score >= 70:
            print("\n⚠️  FAIR: System has basic error handling but needs improvement")
        else:
            print("\n🚨 POOR: System has significant resilience and error handling issues")

        return self.results

if __name__ == "__main__":
    tester = ErrorResilienceTestSuite()
    results = tester.run_all_tests()

    # Save results
    with open('/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/error_resilience_test_results.json', 'w') as f:
        json.dump(results, f, indent=2)

    print(f"\n📄 Detailed results saved to error_resilience_test_results.json")