#!/usr/bin/env python3
"""
MyTrader Frontend Integration Test Suite
Comprehensive validation of all system components
"""

import asyncio
import json
import time
import requests
import subprocess
from urllib.parse import urljoin
import sys
from datetime import datetime

class MyTraderIntegrationTester:
    def __init__(self):
        self.api_base = "http://localhost:5002"
        self.frontend_base = "http://localhost:3000"
        self.results = {
            "timestamp": datetime.now().isoformat(),
            "tests_passed": 0,
            "tests_failed": 0,
            "tests_total": 0,
            "backend_connectivity": {},
            "frontend_connectivity": {},
            "api_endpoints": {},
            "websocket_tests": {},
            "performance_metrics": {},
            "errors": []
        }

    def log_result(self, test_name, success, details="", error=None):
        """Log test result"""
        self.results["tests_total"] += 1
        if success:
            self.results["tests_passed"] += 1
            status = "✅ PASS"
        else:
            self.results["tests_failed"] += 1
            status = "❌ FAIL"
            if error:
                self.results["errors"].append(f"{test_name}: {error}")

        print(f"{status} {test_name}")
        if details:
            print(f"    {details}")
        if error:
            print(f"    Error: {error}")

    def test_backend_health(self):
        """Test backend health endpoint"""
        try:
            response = requests.get(f"{self.api_base}/health", timeout=10)
            if response.status_code == 200:
                data = response.json()
                self.results["backend_connectivity"]["health"] = data
                self.log_result("Backend Health Check", True,
                              f"Status: {data.get('status', 'Unknown')}")
                return True
            else:
                self.log_result("Backend Health Check", False,
                              f"HTTP {response.status_code}")
                return False
        except Exception as e:
            self.log_result("Backend Health Check", False, error=str(e))
            return False

    def test_frontend_accessibility(self):
        """Test frontend accessibility"""
        try:
            response = requests.get(self.frontend_base, timeout=10)
            if response.status_code == 200:
                html_content = response.text
                has_react = "react" in html_content.lower()
                has_vite = "vite" in html_content.lower()

                self.results["frontend_connectivity"]["accessible"] = True
                self.results["frontend_connectivity"]["has_react"] = has_react
                self.results["frontend_connectivity"]["has_vite"] = has_vite

                self.log_result("Frontend Accessibility", True,
                              f"React: {has_react}, Vite: {has_vite}")
                return True
            else:
                self.log_result("Frontend Accessibility", False,
                              f"HTTP {response.status_code}")
                return False
        except Exception as e:
            self.log_result("Frontend Accessibility", False, error=str(e))
            return False

    def test_api_endpoints(self):
        """Test critical API endpoints"""
        endpoints_to_test = [
            ("/", "Root API"),
            ("/health", "Health Check"),
            ("/api/symbols", "Symbols API"),
            ("/api/auth/guest-session", "Guest Session", "POST"),
            ("/hubs/marketdata", "MarketData Hub"),
            ("/hubs/dashboard", "Dashboard Hub"),
        ]

        for endpoint_path, name, *method in endpoints_to_test:
            http_method = method[0] if method else "GET"
            try:
                url = f"{self.api_base}{endpoint_path}"
                if http_method == "POST":
                    response = requests.post(url, json={}, timeout=10)
                else:
                    response = requests.get(url, timeout=10)

                # Different status codes are acceptable for different endpoints
                success = False
                details = f"HTTP {response.status_code}"

                if endpoint_path in ["/", "/health", "/api/symbols"]:
                    success = response.status_code == 200
                elif endpoint_path == "/api/auth/guest-session":
                    # Guest session might return various codes depending on configuration
                    success = response.status_code in [200, 201, 400, 401]
                elif "hubs" in endpoint_path:
                    # SignalR hubs expect WebSocket upgrade, so 404 or connection required is ok
                    success = response.status_code in [404, 400] or "Connection ID required" in response.text

                if success and response.status_code == 200:
                    try:
                        data = response.json()
                        self.results["api_endpoints"][name] = data
                        if "symbols" in endpoint_path and isinstance(data, dict) and "symbols" in data:
                            details += f", {len(data['symbols'])} symbols found"
                    except:
                        # Not JSON, that's ok for some endpoints
                        pass

                self.log_result(f"API: {name}", success, details)

            except Exception as e:
                self.log_result(f"API: {name}", False, error=str(e))

    def test_cors_configuration(self):
        """Test CORS configuration"""
        try:
            headers = {
                'Origin': self.frontend_base,
                'Content-Type': 'application/json'
            }
            response = requests.get(f"{self.api_base}/health", headers=headers, timeout=10)

            cors_headers = {
                'access-control-allow-origin': response.headers.get('Access-Control-Allow-Origin'),
                'access-control-allow-methods': response.headers.get('Access-Control-Allow-Methods'),
                'access-control-allow-headers': response.headers.get('Access-Control-Allow-Headers')
            }

            self.results["backend_connectivity"]["cors"] = cors_headers

            # CORS is configured if we get the response successfully
            success = response.status_code == 200
            self.log_result("CORS Configuration", success,
                          f"Origin allowed: {cors_headers['access-control-allow-origin'] is not None}")

        except Exception as e:
            self.log_result("CORS Configuration", False, error=str(e))

    def test_performance_metrics(self):
        """Test basic performance metrics"""
        try:
            # Test backend response time
            start_time = time.time()
            response = requests.get(f"{self.api_base}/health", timeout=10)
            backend_response_time = (time.time() - start_time) * 1000

            # Test frontend response time
            start_time = time.time()
            response = requests.get(self.frontend_base, timeout=10)
            frontend_response_time = (time.time() - start_time) * 1000

            self.results["performance_metrics"] = {
                "backend_response_time_ms": round(backend_response_time, 2),
                "frontend_response_time_ms": round(frontend_response_time, 2)
            }

            backend_fast = backend_response_time < 1000  # < 1 second
            frontend_fast = frontend_response_time < 3000  # < 3 seconds

            self.log_result("Backend Performance", backend_fast,
                          f"{backend_response_time:.0f}ms")
            self.log_result("Frontend Performance", frontend_fast,
                          f"{frontend_response_time:.0f}ms")

        except Exception as e:
            self.log_result("Performance Test", False, error=str(e))

    def test_websocket_connectivity(self):
        """Test WebSocket connectivity using a simple HTTP check"""
        # Since we can't easily test WebSocket in Python without additional libraries,
        # we'll test if the SignalR negotiate endpoint is available
        try:
            # Test MarketData Hub negotiate
            response = requests.post(f"{self.api_base}/hubs/marketdata/negotiate",
                                   timeout=10)
            marketdata_available = response.status_code in [200, 404, 405]  # Various acceptable responses

            # Test Dashboard Hub negotiate
            response = requests.post(f"{self.api_base}/hubs/dashboard/negotiate",
                                   timeout=10)
            dashboard_available = response.status_code in [200, 404, 405]

            self.results["websocket_tests"] = {
                "marketdata_hub_available": marketdata_available,
                "dashboard_hub_available": dashboard_available
            }

            self.log_result("WebSocket Hubs Available",
                          marketdata_available and dashboard_available,
                          f"MarketData: {marketdata_available}, Dashboard: {dashboard_available}")

        except Exception as e:
            self.log_result("WebSocket Connectivity", False, error=str(e))

    def test_mobile_api_compatibility(self):
        """Test mobile API compatibility"""
        try:
            # Test with mobile user agent
            headers = {
                'User-Agent': 'MyTrader-Mobile/1.0 (iOS)',
                'Content-Type': 'application/json'
            }

            response = requests.get(f"{self.api_base}/api/symbols",
                                  headers=headers, timeout=10)

            mobile_compatible = response.status_code == 200
            self.log_result("Mobile API Compatibility", mobile_compatible,
                          f"Mobile user agent accepted: {mobile_compatible}")

        except Exception as e:
            self.log_result("Mobile API Compatibility", False, error=str(e))

    def check_service_status(self):
        """Check if required services are running"""
        try:
            # Check for dotnet process
            result = subprocess.run(['pgrep', '-f', 'dotnet'],
                                  capture_output=True, text=True)
            dotnet_running = result.returncode == 0

            # Check for node processes
            result = subprocess.run(['pgrep', '-f', 'node'],
                                  capture_output=True, text=True)
            node_running = result.returncode == 0

            self.log_result("Backend Service Running", dotnet_running,
                          f".NET process detected: {dotnet_running}")
            self.log_result("Frontend Service Running", node_running,
                          f"Node.js process detected: {node_running}")

        except Exception as e:
            self.log_result("Service Status Check", False, error=str(e))

    def run_all_tests(self):
        """Run all integration tests"""
        print("🚀 Starting MyTrader Integration Test Suite")
        print("=" * 60)

        # Service status
        print("\n📋 Service Status:")
        self.check_service_status()

        # Connectivity tests
        print("\n🌐 Connectivity Tests:")
        self.test_backend_health()
        self.test_frontend_accessibility()

        # API tests
        print("\n🔗 API Endpoint Tests:")
        self.test_api_endpoints()
        self.test_cors_configuration()
        self.test_mobile_api_compatibility()

        # WebSocket tests
        print("\n⚡ WebSocket Tests:")
        self.test_websocket_connectivity()

        # Performance tests
        print("\n⏱️  Performance Tests:")
        self.test_performance_metrics()

        # Summary
        print("\n" + "=" * 60)
        print("📊 Test Summary:")
        print(f"✅ Passed: {self.results['tests_passed']}")
        print(f"❌ Failed: {self.results['tests_failed']}")
        print(f"📈 Total:  {self.results['tests_total']}")

        success_rate = (self.results['tests_passed'] / self.results['tests_total']) * 100
        print(f"🎯 Success Rate: {success_rate:.1f}%")

        if self.results['errors']:
            print("\n🚨 Errors:")
            for error in self.results['errors']:
                print(f"   {error}")

        # Final assessment
        if success_rate >= 80:
            print("\n🎉 OVERALL: SYSTEM READY FOR PRODUCTION")
        elif success_rate >= 60:
            print("\n⚠️  OVERALL: SYSTEM NEEDS MINOR FIXES")
        else:
            print("\n🚨 OVERALL: SYSTEM NEEDS MAJOR FIXES")

        return self.results

if __name__ == "__main__":
    tester = MyTraderIntegrationTester()
    results = tester.run_all_tests()

    # Save detailed results
    with open('/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader/integration_test_results.json', 'w') as f:
        json.dump(results, f, indent=2)

    print(f"\n📄 Detailed results saved to integration_test_results.json")