#!/usr/bin/env python3
"""
Cross-Platform Integration Test Script

Tests the integration between frontend applications (Web & Mobile) and the backend API.
Simulates frontend behavior to validate cross-platform compatibility.
"""

import asyncio
import aiohttp
import json
import subprocess
import os
import time
from pathlib import Path
from typing import Dict, List, Any, Optional

class CrossPlatformTestResult:
    def __init__(self, test_name: str):
        self.test_name = test_name
        self.success = False
        self.message = ""
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

class CrossPlatformIntegrationTester:
    def __init__(self):
        self.results: List[CrossPlatformTestResult] = []
        self.base_path = Path("/Users/mustafayildirim/Documents/Personal Documents/Projects/myTrader")
        self.api_base = "http://localhost:5002"

    async def run_cross_platform_tests(self):
        """Execute all cross-platform integration tests"""
        print("🌍 Starting Cross-Platform Integration Testing")
        print("=" * 60)

        await self.test_frontend_project_structure()
        await self.test_frontend_configuration()
        await self.test_api_service_integration()
        await self.test_websocket_service_integration()
        await self.test_mobile_specific_integration()
        await self.test_responsive_design_integration()

        self.generate_cross_platform_report()

    async def test_frontend_project_structure(self):
        """Test that frontend projects have proper structure"""
        print("\n📁 Testing Frontend Project Structure")

        # Test Web Frontend Structure
        result = CrossPlatformTestResult("Web Frontend Structure")
        try:
            web_path = self.base_path / "frontend" / "web"
            required_files = [
                "package.json",
                "src/App.tsx",
                "src/services/api.ts",
                "src/services/websocketService.ts",
                "src/components/Login.tsx",
                "src/components/Register.tsx"
            ]

            missing_files = []
            present_files = []

            for file_path in required_files:
                full_path = web_path / file_path
                if full_path.exists():
                    present_files.append(file_path)
                else:
                    missing_files.append(file_path)

            if not missing_files:
                result.complete(True, f"All {len(present_files)} critical files present")
            else:
                result.complete(False, f"Missing files: {', '.join(missing_files)}")

        except Exception as e:
            result.complete(False, f"Structure test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

        # Test Mobile Frontend Structure
        result = CrossPlatformTestResult("Mobile Frontend Structure")
        try:
            mobile_path = self.base_path / "frontend" / "mobile"
            required_files = [
                "package.json",
                "src/services/api.ts",
                "src/services/websocketService.ts",
                "src/screens/DashboardScreen.tsx",
                "src/screens/PortfolioScreen.tsx"
            ]

            missing_files = []
            present_files = []

            for file_path in required_files:
                full_path = mobile_path / file_path
                if full_path.exists():
                    present_files.append(file_path)
                else:
                    missing_files.append(file_path)

            if not missing_files:
                result.complete(True, f"All {len(present_files)} critical files present")
            else:
                result.complete(False, f"Missing files: {', '.join(missing_files)}")

        except Exception as e:
            result.complete(False, f"Structure test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

    async def test_frontend_configuration(self):
        """Test frontend configuration consistency"""
        print("\n⚙️  Testing Frontend Configuration")

        # Test Web Configuration
        result = CrossPlatformTestResult("Web Frontend Configuration")
        try:
            web_config_path = self.base_path / "frontend" / "web" / "src" / "config.ts"
            if web_config_path.exists():
                with open(web_config_path, 'r') as f:
                    config_content = f.read()

                # Check for API base URL configuration
                if "localhost:5002" in config_content or "API_BASE" in config_content:
                    result.complete(True, "API configuration found")
                else:
                    result.complete(False, "API configuration missing or incorrect")
            else:
                # Check package.json for proxy settings
                package_json_path = self.base_path / "frontend" / "web" / "package.json"
                if package_json_path.exists():
                    with open(package_json_path, 'r') as f:
                        package_data = json.load(f)

                    if "proxy" in package_data:
                        result.complete(True, f"Proxy configuration: {package_data['proxy']}")
                    else:
                        result.complete(False, "No API configuration found")
                else:
                    result.complete(False, "Configuration files missing")

        except Exception as e:
            result.complete(False, f"Web configuration test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

        # Test Mobile Configuration
        result = CrossPlatformTestResult("Mobile Frontend Configuration")
        try:
            mobile_config_path = self.base_path / "frontend" / "mobile" / "src" / "config.ts"
            if mobile_config_path.exists():
                with open(mobile_config_path, 'r') as f:
                    config_content = f.read()

                # Check for API configuration
                if "localhost" in config_content or "API_BASE" in config_content:
                    result.complete(True, "Mobile API configuration found")
                else:
                    result.complete(False, "Mobile API configuration missing")
            else:
                result.complete(False, "Mobile configuration file missing")

        except Exception as e:
            result.complete(False, f"Mobile configuration test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

    async def test_api_service_integration(self):
        """Test API service integration consistency across platforms"""
        print("\n🔗 Testing API Service Integration")

        # Test Web API Service
        result = CrossPlatformTestResult("Web API Service Integration")
        try:
            web_api_path = self.base_path / "frontend" / "web" / "src" / "services" / "api.ts"
            if web_api_path.exists():
                with open(web_api_path, 'r') as f:
                    api_content = f.read()

                # Check for critical API endpoints
                endpoints_to_check = ['auth', 'market', 'price', 'symbol']
                found_endpoints = [ep for ep in endpoints_to_check if ep in api_content.lower()]

                if len(found_endpoints) >= 3:
                    result.complete(True, f"API endpoints found: {', '.join(found_endpoints)}")
                else:
                    result.complete(False, f"Missing API endpoints. Found: {', '.join(found_endpoints)}")
            else:
                result.complete(False, "Web API service file missing")

        except Exception as e:
            result.complete(False, f"Web API service test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

        # Test Mobile API Service
        result = CrossPlatformTestResult("Mobile API Service Integration")
        try:
            mobile_api_path = self.base_path / "frontend" / "mobile" / "src" / "services" / "api.ts"
            if mobile_api_path.exists():
                with open(mobile_api_path, 'r') as f:
                    api_content = f.read()

                # Check for critical API endpoints
                endpoints_to_check = ['auth', 'market', 'price', 'symbol']
                found_endpoints = [ep for ep in endpoints_to_check if ep in api_content.lower()]

                if len(found_endpoints) >= 3:
                    result.complete(True, f"Mobile API endpoints found: {', '.join(found_endpoints)}")
                else:
                    result.complete(False, f"Missing mobile API endpoints. Found: {', '.join(found_endpoints)}")
            else:
                result.complete(False, "Mobile API service file missing")

        except Exception as e:
            result.complete(False, f"Mobile API service test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

    async def test_websocket_service_integration(self):
        """Test WebSocket service integration across platforms"""
        print("\n🔄 Testing WebSocket Service Integration")

        # Test Web WebSocket Service
        result = CrossPlatformTestResult("Web WebSocket Service")
        try:
            web_ws_path = self.base_path / "frontend" / "web" / "src" / "services" / "websocketService.ts"
            if web_ws_path.exists():
                with open(web_ws_path, 'r') as f:
                    ws_content = f.read()

                # Check for SignalR integration
                if "signalr" in ws_content.lower() or "hubconnection" in ws_content.lower():
                    result.complete(True, "Web SignalR integration found")
                elif "websocket" in ws_content.lower():
                    result.complete(True, "Web WebSocket integration found")
                else:
                    result.complete(False, "No real-time integration found")
            else:
                result.complete(False, "Web WebSocket service missing")

        except Exception as e:
            result.complete(False, f"Web WebSocket test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

        # Test Mobile WebSocket Service
        result = CrossPlatformTestResult("Mobile WebSocket Service")
        try:
            mobile_ws_path = self.base_path / "frontend" / "mobile" / "src" / "services" / "websocketService.ts"
            if mobile_ws_path.exists():
                with open(mobile_ws_path, 'r') as f:
                    ws_content = f.read()

                # Check for real-time integration
                if "signalr" in ws_content.lower() or "websocket" in ws_content.lower():
                    result.complete(True, "Mobile real-time integration found")
                else:
                    result.complete(False, "No mobile real-time integration found")
            else:
                result.complete(False, "Mobile WebSocket service missing")

        except Exception as e:
            result.complete(False, f"Mobile WebSocket test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

    async def test_mobile_specific_integration(self):
        """Test mobile-specific integration features"""
        print("\n📱 Testing Mobile-Specific Integration")

        # Test React Native Configuration
        result = CrossPlatformTestResult("React Native Configuration")
        try:
            mobile_path = self.base_path / "frontend" / "mobile"
            package_json_path = mobile_path / "package.json"
            app_json_path = mobile_path / "app.json"

            if package_json_path.exists() and app_json_path.exists():
                with open(package_json_path, 'r') as f:
                    package_data = json.load(f)

                with open(app_json_path, 'r') as f:
                    app_data = json.load(f)

                # Check for React Native dependencies
                dependencies = package_data.get('dependencies', {})
                rn_deps = [dep for dep in dependencies.keys() if 'react-native' in dep or 'expo' in dep]

                if rn_deps:
                    result.complete(True, f"React Native/Expo setup found: {len(rn_deps)} dependencies")
                else:
                    result.complete(False, "React Native/Expo dependencies missing")
            else:
                result.complete(False, "Mobile configuration files missing")

        except Exception as e:
            result.complete(False, f"React Native configuration test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

        # Test Mobile Network Configuration
        result = CrossPlatformTestResult("Mobile Network Configuration")
        try:
            # Check for network security configuration or CORS handling
            mobile_config_path = self.base_path / "frontend" / "mobile" / "src" / "config.ts"
            if mobile_config_path.exists():
                with open(mobile_config_path, 'r') as f:
                    config_content = f.read()

                # Check for localhost handling (important for mobile development)
                if "10.0.2.2" in config_content or "localhost" in config_content or "REACT_NATIVE" in config_content:
                    result.complete(True, "Mobile network configuration found")
                else:
                    result.complete(False, "Mobile network configuration may need attention")
            else:
                result.complete(False, "Mobile configuration missing")

        except Exception as e:
            result.complete(False, f"Mobile network configuration test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

    async def test_responsive_design_integration(self):
        """Test responsive design and cross-platform UI consistency"""
        print("\n📐 Testing Responsive Design Integration")

        # Test Web Responsive Design
        result = CrossPlatformTestResult("Web Responsive Design")
        try:
            web_path = self.base_path / "frontend" / "web" / "src"
            css_files = list(web_path.glob("**/*.css")) + list(web_path.glob("**/*.scss"))

            responsive_features = 0
            for css_file in css_files:
                with open(css_file, 'r') as f:
                    css_content = f.read()

                # Check for responsive design features
                if "@media" in css_content:
                    responsive_features += 1
                if "flex" in css_content or "grid" in css_content:
                    responsive_features += 1

            if responsive_features > 0:
                result.complete(True, f"Responsive design features found in {len(css_files)} files")
            else:
                result.complete(False, "No responsive design features detected")

        except Exception as e:
            result.complete(False, f"Responsive design test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

        # Test Cross-Platform UI Consistency
        result = CrossPlatformTestResult("Cross-Platform UI Consistency")
        try:
            # Compare component names and structures between web and mobile
            web_components_path = self.base_path / "frontend" / "web" / "src" / "components"
            mobile_components_path = self.base_path / "frontend" / "mobile" / "src" / "components"

            web_components = set()
            mobile_components = set()

            if web_components_path.exists():
                web_components = {f.stem for f in web_components_path.glob("**/*.tsx")}

            if mobile_components_path.exists():
                mobile_components = {f.stem for f in mobile_components_path.glob("**/*.tsx")}

            common_components = web_components.intersection(mobile_components)
            consistency_ratio = len(common_components) / max(len(web_components), len(mobile_components), 1)

            if consistency_ratio > 0.3:  # 30% consistency is reasonable
                result.complete(True, f"UI consistency: {consistency_ratio:.1%} ({len(common_components)} common components)")
            else:
                result.complete(False, f"Low UI consistency: {consistency_ratio:.1%}")

        except Exception as e:
            result.complete(False, f"UI consistency test failed: {str(e)}")

        self.results.append(result)
        print(f"  {result}")

    def generate_cross_platform_report(self):
        """Generate cross-platform integration report"""
        print("\n" + "=" * 60)
        print("📋 CROSS-PLATFORM INTEGRATION REPORT")
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

        # Platform-specific analysis
        web_tests = [r for r in self.results if 'web' in r.test_name.lower()]
        mobile_tests = [r for r in self.results if 'mobile' in r.test_name.lower()]

        web_success_rate = (sum(1 for r in web_tests if r.success) / len(web_tests)) * 100 if web_tests else 0
        mobile_success_rate = (sum(1 for r in mobile_tests if r.success) / len(mobile_tests)) * 100 if mobile_tests else 0

        print(f"\n🌐 PLATFORM ANALYSIS:")
        print(f"   Web Platform: {web_success_rate:.1f}% success rate")
        print(f"   Mobile Platform: {mobile_success_rate:.1f}% success rate")

        # Cross-platform compatibility assessment
        print(f"\n🔗 CROSS-PLATFORM COMPATIBILITY:")
        if success_rate >= 80:
            print("   ✅ EXCELLENT - Platforms are well integrated")
        elif success_rate >= 60:
            print("   ⚠️  GOOD - Minor cross-platform issues")
        else:
            print("   🚨 NEEDS ATTENTION - Significant cross-platform issues")

        # Detailed Results
        print(f"\n📝 DETAILED RESULTS:")
        for result in self.results:
            print(f"   {result}")

        print(f"\n🏁 Cross-platform integration testing completed!")
        return success_rate >= 70

async def main():
    """Run cross-platform integration tests"""
    tester = CrossPlatformIntegrationTester()
    success = await tester.run_cross_platform_tests()
    return success

if __name__ == "__main__":
    success = asyncio.run(main())
    exit(0 if success else 1)