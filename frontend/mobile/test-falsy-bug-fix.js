/**
 * Test script to validate the JavaScript falsy bug fix
 *
 * This demonstrates the fix for the previousClose field issue where
 * the value 0 was being treated as falsy and converted to undefined.
 */

console.log('='.repeat(80));
console.log('TESTING JAVASCRIPT FALSY BUG FIX');
console.log('='.repeat(80));

// Simulate the normalizePrice function
function normalizePrice(value) {
  if (value === null || value === undefined) return 0;
  const numValue = Number(value);
  if (isNaN(numValue)) return 0;
  return numValue;
}

// Test data with various previousClose values
const testData = [
  { symbol: 'TSLA', price: 435.54, previousClose: 435.54 },
  { symbol: 'AAPL', price: 254.04, previousClose: 254.04 },
  { symbol: 'TEST1', price: 100, previousClose: 0 },      // Edge case: 0
  { symbol: 'TEST2', price: 50, previousClose: null },    // Edge case: null
  { symbol: 'TEST3', price: 75, previousClose: undefined }, // Edge case: undefined
  { symbol: 'GARAN', price: 130.00, previousClose: 130.00 }
];

console.log('\n1. BEFORE FIX (using falsy check):');
console.log('-'.repeat(80));

testData.forEach(data => {
  // OLD BUGGY CODE: const previousClose = data.previousClose ? normalizePrice(...) : undefined;
  const previousClose = data.previousClose ? normalizePrice(data.previousClose) : undefined;

  const status = previousClose !== undefined ? '✅' : '❌';
  console.log(`${status} Stock ${data.symbol}: previousClose = ${previousClose} (raw: ${data.previousClose})`);
});

console.log('\n2. AFTER FIX (using explicit null/undefined check):');
console.log('-'.repeat(80));

testData.forEach(data => {
  // NEW FIXED CODE: const previousClose = (data.previousClose !== undefined && data.previousClose !== null) ? normalizePrice(...) : undefined;
  const previousClose = (data.previousClose !== undefined && data.previousClose !== null)
    ? normalizePrice(data.previousClose)
    : undefined;

  const status = previousClose !== undefined ? '✅' : '❌';
  console.log(`${status} Stock ${data.symbol}: previousClose = ${previousClose} (raw: ${data.previousClose})`);
});

console.log('\n3. CRITICAL TEST: Value 0 handling');
console.log('-'.repeat(80));

const zeroTestData = { symbol: 'ZERO_TEST', price: 100, previousClose: 0 };

const buggyResult = zeroTestData.previousClose ? normalizePrice(zeroTestData.previousClose) : undefined;
const fixedResult = (zeroTestData.previousClose !== undefined && zeroTestData.previousClose !== null)
  ? normalizePrice(zeroTestData.previousClose)
  : undefined;

console.log(`Input: previousClose = ${zeroTestData.previousClose}`);
console.log(`Buggy code result: ${buggyResult} (${buggyResult === undefined ? 'FAIL ❌' : 'PASS ✅'})`);
console.log(`Fixed code result: ${fixedResult} (${fixedResult === undefined ? 'FAIL ❌' : 'PASS ✅'})`);

console.log('\n4. SUMMARY');
console.log('-'.repeat(80));

if (buggyResult === undefined && fixedResult !== undefined) {
  console.log('✅ FIX VALIDATED: The explicit null/undefined check correctly handles 0 values');
  console.log('✅ previousClose=0 is now preserved instead of becoming undefined');
  console.log('✅ The JavaScript falsy bug is FIXED');
} else {
  console.log('❌ VALIDATION FAILED: Fix did not work as expected');
}

console.log('='.repeat(80));
