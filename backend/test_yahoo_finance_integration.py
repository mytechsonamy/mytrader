#!/usr/bin/env python3
"""
Yahoo Finance Daily Sync Integration Test
Tests the complete integration flow from API calls to database persistence
"""

import asyncio
import aiohttp
import json
import logging
from datetime import datetime, timedelta
from typing import Dict, List, Any

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class YahooFinanceIntegrationTest:
    def __init__(self, base_url: str = "https://localhost:7001"):
        self.base_url = base_url
        self.session = None
        self.auth_token = None

    async def __aenter__(self):
        # Create SSL context that allows self-signed certificates for development
        import ssl
        ssl_context = ssl.create_default_context()
        ssl_context.check_hostname = False
        ssl_context.verify_mode = ssl.CERT_NONE

        connector = aiohttp.TCPConnector(ssl=ssl_context)
        self.session = aiohttp.ClientSession(connector=connector)
        return self

    async def __aexit__(self, exc_type, exc_val, exc_tb):
        if self.session:
            await self.session.close()

    async def authenticate(self, username: str = "testuser@example.com", password: str = "TestPassword123!"):
        """Authenticate and get JWT token"""
        try:
            auth_data = {
                "email": username,
                "password": password
            }

            async with self.session.post(f"{self.base_url}/api/auth/login", json=auth_data) as response:
                if response.status == 200:
                    result = await response.json()
                    self.auth_token = result.get("token")
                    logger.info("Authentication successful")
                    return True
                else:
                    logger.error(f"Authentication failed: {response.status}")
                    return False
        except Exception as e:
            logger.error(f"Authentication error: {e}")
            return False

    def get_headers(self) -> Dict[str, str]:
        """Get headers with authentication"""
        headers = {"Content-Type": "application/json"}
        if self.auth_token:
            headers["Authorization"] = f"Bearer {self.auth_token}"
        return headers

    async def test_api_connectivity(self) -> bool:
        """Test Yahoo Finance API connectivity"""
        logger.info("Testing Yahoo Finance API connectivity...")

        test_cases = [
            {"market": "NASDAQ", "symbol": "AAPL"},
            {"market": "NYSE", "symbol": "MSFT"},
            {"market": "BIST", "symbol": "THYAO"},
            {"market": "CRYPTO", "symbol": "BTC"}
        ]

        all_passed = True

        for test_case in test_cases:
            try:
                url = f"{self.base_url}/api/yahoofinancesync/test/{test_case['market']}/{test_case['symbol']}"
                async with self.session.get(url, headers=self.get_headers()) as response:
                    result = await response.json()

                    if result.get("success", False):
                        logger.info(f"✓ API connectivity test passed for {test_case['market']}:{test_case['symbol']}")
                    else:
                        logger.error(f"✗ API connectivity test failed for {test_case['market']}:{test_case['symbol']}: {result.get('message')}")
                        all_passed = False

            except Exception as e:
                logger.error(f"✗ API connectivity test error for {test_case['market']}:{test_case['symbol']}: {e}")
                all_passed = False

        return all_passed

    async def test_manual_sync(self) -> bool:
        """Test manual sync trigger for a specific market"""
        logger.info("Testing manual sync trigger...")

        # Test with NASDAQ (smaller dataset for testing)
        try:
            # Use yesterday's date for testing
            test_date = (datetime.now() - timedelta(days=1)).strftime("%Y-%m-%d")
            url = f"{self.base_url}/api/yahoofinancesync/sync/NASDAQ?specificDate={test_date}"

            async with self.session.post(url, headers=self.get_headers()) as response:
                result = await response.json()

                if result.get("success", False):
                    data = result.get("data", {})
                    stats = data.get("statistics", {})

                    logger.info(f"✓ Manual sync completed successfully")
                    logger.info(f"  - Market: {data.get('market')}")
                    logger.info(f"  - Sync Date: {data.get('syncDate')}")
                    logger.info(f"  - Duration: {data.get('duration')}")
                    logger.info(f"  - Successful: {stats.get('successful', 0)}")
                    logger.info(f"  - Failed: {stats.get('failed', 0)}")
                    logger.info(f"  - Total Records: {stats.get('totalRecords', 0)}")
                    logger.info(f"  - Completeness: {stats.get('completeness', 0):.1f}%")

                    return True
                else:
                    logger.error(f"✗ Manual sync failed: {result.get('message')}")
                    return False

        except Exception as e:
            logger.error(f"✗ Manual sync test error: {e}")
            return False

    async def test_data_quality_validation(self) -> bool:
        """Test data quality validation"""
        logger.info("Testing data quality validation...")

        try:
            # Test validation for yesterday's data
            test_date = (datetime.now() - timedelta(days=1)).strftime("%Y-%m-%d")
            url = f"{self.base_url}/api/yahoofinancesync/validate/NASDAQ?date={test_date}"

            async with self.session.post(url, headers=self.get_headers()) as response:
                result = await response.json()

                if result.get("success", False):
                    data = result.get("data", {})

                    logger.info(f"✓ Data quality validation completed")
                    logger.info(f"  - Market: {data.get('market')}")
                    logger.info(f"  - Validation Date: {data.get('validationDate')}")
                    logger.info(f"  - Overall Score: {data.get('overallScore', 0):.1f}")
                    logger.info(f"  - Total Records: {data.get('totalRecords', 0)}")
                    logger.info(f"  - Issues Found: {len(data.get('issues', []))}")

                    # Log recommendations if any
                    recommendations = data.get("recommendations", [])
                    if recommendations:
                        logger.info("  - Recommendations:")
                        for rec in recommendations[:3]:  # Show first 3
                            logger.info(f"    • {rec}")

                    return True
                else:
                    logger.error(f"✗ Data quality validation failed: {result.get('message')}")
                    return False

        except Exception as e:
            logger.error(f"✗ Data quality validation test error: {e}")
            return False

    async def test_data_completeness(self) -> bool:
        """Test data completeness check"""
        logger.info("Testing data completeness check...")

        try:
            # Check completeness for last 7 days
            end_date = datetime.now() - timedelta(days=1)
            start_date = end_date - timedelta(days=7)

            url = f"{self.base_url}/api/yahoofinancesync/completeness/NASDAQ"
            url += f"?startDate={start_date.strftime('%Y-%m-%d')}&endDate={end_date.strftime('%Y-%m-%d')}"

            async with self.session.get(url, headers=self.get_headers()) as response:
                result = await response.json()

                if result.get("success", False):
                    data = result.get("data", {})

                    logger.info(f"✓ Data completeness check completed")
                    logger.info(f"  - Market: {data.get('market')}")
                    logger.info(f"  - Average Completeness: {data.get('averageCompleteness', 0):.1f}%")
                    logger.info(f"  - Incomplete Days: {data.get('incompleteDaysCount', 0)}")

                    # Show recent completeness data
                    daily_data = data.get("dailyCompleteness", [])[:3]  # Show last 3 days
                    if daily_data:
                        logger.info("  - Recent Daily Completeness:")
                        for day in daily_data:
                            logger.info(f"    • {day.get('date')}: {day.get('completenessPercent', 0):.1f}% ({day.get('actualRecords')}/{day.get('expectedRecords')})")

                    return True
                else:
                    logger.error(f"✗ Data completeness check failed: {result.get('message')}")
                    return False

        except Exception as e:
            logger.error(f"✗ Data completeness check test error: {e}")
            return False

    async def test_gap_filling(self) -> bool:
        """Test data gap detection and filling"""
        logger.info("Testing data gap detection and filling...")

        try:
            # Test gap filling for last 7 days
            end_date = datetime.now() - timedelta(days=1)
            start_date = end_date - timedelta(days=7)

            url = f"{self.base_url}/api/yahoofinancesync/fill-gaps/NASDAQ"
            url += f"?startDate={start_date.strftime('%Y-%m-%d')}&endDate={end_date.strftime('%Y-%m-%d')}"

            async with self.session.post(url, headers=self.get_headers()) as response:
                result = await response.json()

                if result.get("success", False):
                    data = result.get("data", {})
                    stats = data.get("statistics", {})

                    logger.info(f"✓ Gap filling completed")
                    logger.info(f"  - Market: {data.get('market')}")
                    logger.info(f"  - Gaps Detected: {stats.get('gapsDetected', 0)}")
                    logger.info(f"  - Gaps Filled: {stats.get('gapsFilled', 0)}")
                    logger.info(f"  - Gaps Failed: {stats.get('gapsFailed', 0)}")

                    return True
                else:
                    logger.error(f"✗ Gap filling failed: {result.get('message')}")
                    return False

        except Exception as e:
            logger.error(f"✗ Gap filling test error: {e}")
            return False

    async def test_sync_status(self) -> bool:
        """Test sync status and error statistics"""
        logger.info("Testing sync status and error statistics...")

        try:
            url = f"{self.base_url}/api/yahoofinancesync/status"

            async with self.session.get(url, headers=self.get_headers()) as response:
                result = await response.json()

                if result.get("success", False):
                    data = result.get("data", {})

                    logger.info(f"✓ Sync status retrieved")
                    logger.info(f"  - Timestamp: {data.get('timestamp')}")
                    logger.info(f"  - Circuit Breakers: {len(data.get('circuitBreakers', []))}")
                    logger.info(f"  - Retry Statistics: {len(data.get('retryStatistics', []))}")

                    # Show circuit breaker states
                    circuit_breakers = data.get("circuitBreakers", [])
                    if circuit_breakers:
                        logger.info("  - Circuit Breaker States:")
                        for cb in circuit_breakers[:3]:  # Show first 3
                            logger.info(f"    • {cb.get('operation')}: {cb.get('state')} (failures: {cb.get('failureCount', 0)})")

                    return True
                else:
                    logger.error(f"✗ Sync status check failed: {result.get('message')}")
                    return False

        except Exception as e:
            logger.error(f"✗ Sync status test error: {e}")
            return False

async def run_integration_tests():
    """Run the complete integration test suite"""
    logger.info("🚀 Starting Yahoo Finance Daily Sync Integration Tests")
    logger.info("=" * 60)

    async with YahooFinanceIntegrationTest() as test_client:
        # Authenticate first
        if not await test_client.authenticate():
            logger.error("❌ Authentication failed - cannot proceed with tests")
            return False

        # Run all tests
        tests = [
            ("API Connectivity", test_client.test_api_connectivity),
            ("Sync Status", test_client.test_sync_status),
            ("Manual Sync", test_client.test_manual_sync),
            ("Data Quality Validation", test_client.test_data_quality_validation),
            ("Data Completeness", test_client.test_data_completeness),
            ("Gap Filling", test_client.test_gap_filling),
        ]

        results = []

        for test_name, test_func in tests:
            logger.info(f"\n📋 Running test: {test_name}")
            logger.info("-" * 40)

            try:
                result = await test_func()
                results.append((test_name, result))

                if result:
                    logger.info(f"✅ {test_name} - PASSED")
                else:
                    logger.info(f"❌ {test_name} - FAILED")

            except Exception as e:
                logger.error(f"💥 {test_name} - ERROR: {e}")
                results.append((test_name, False))

        # Summary
        logger.info("\n" + "=" * 60)
        logger.info("🏁 TEST SUMMARY")
        logger.info("=" * 60)

        passed = sum(1 for _, result in results if result)
        total = len(results)

        for test_name, result in results:
            status = "✅ PASSED" if result else "❌ FAILED"
            logger.info(f"{test_name:<30} {status}")

        logger.info("-" * 60)
        logger.info(f"Total: {passed}/{total} tests passed ({passed/total*100:.1f}%)")

        if passed == total:
            logger.info("🎉 All tests passed! Yahoo Finance sync system is working correctly.")
            return True
        else:
            logger.warning(f"⚠️  {total - passed} test(s) failed. Please check the implementation.")
            return False

if __name__ == "__main__":
    # Run the integration tests
    success = asyncio.run(run_integration_tests())
    exit(0 if success else 1)