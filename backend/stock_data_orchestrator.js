#!/usr/bin/env node

/**
 * Stock_Scrapper Data Import Orchestrator
 * Systematically imports historical data across all markets with progress monitoring
 */

const fs = require('fs');
const path = require('path');
const axios = require('axios');

// Configuration
const CONFIG = {
    API_BASE_URL: 'http://localhost:5245/api',
    STOCK_SCRAPPER_DATA_PATH: '/Users/mustafayildirim/Documents/Personal Documents/Projects/Stock_Scrapper/DATA',
    BATCH_SIZE: 5, // Files per batch
    DELAY_BETWEEN_BATCHES: 2000, // 2 seconds
    MAX_RETRIES: 3,
    MARKETS: [
        { name: 'CRYPTO', priority: 1, estimated_time: '2 minutes' },  // Smallest dataset first
        { name: 'NASDAQ', priority: 2, estimated_time: '10 minutes' }, // Medium datasets
        { name: 'NYSE', priority: 3, estimated_time: '10 minutes' },
        { name: 'BIST', priority: 4, estimated_time: '20 minutes' }    // Largest dataset last
    ]
};

// Global state tracking
const STATE = {
    totalFiles: 0,
    processedFiles: 0,
    successfulFiles: 0,
    failedFiles: 0,
    startTime: null,
    currentMarket: null,
    marketResults: {},
    errors: []
};

/**
 * Progress reporting utilities
 */
class ProgressReporter {
    static logHeader() {
        console.log('\n🚀 MYTRADER DATA IMPORT ORCHESTRATOR');
        console.log('=====================================');
        console.log(`Target: ${STATE.totalFiles} files across ${CONFIG.MARKETS.length} markets`);
        console.log(`API Endpoint: ${CONFIG.API_BASE_URL}`);
        console.log(`Data Source: ${CONFIG.STOCK_SCRAPPER_DATA_PATH}`);
        console.log('=====================================\n');
    }

    static logMarketStart(market, fileCount) {
        console.log(`\n📈 STARTING ${market.name} MARKET`);
        console.log(`   Files: ${fileCount}`);
        console.log(`   Estimated Time: ${market.estimated_time}`);
        console.log(`   Priority: ${market.priority}`);
        console.log('   ─────────────────────────────────');
    }

    static logProgress() {
        const elapsed = Date.now() - STATE.startTime;
        const rate = STATE.processedFiles / (elapsed / 1000 / 60); // files per minute
        const remaining = STATE.totalFiles - STATE.processedFiles;
        const eta = remaining / Math.max(rate, 0.1); // avoid division by zero

        console.log(`\n📊 PROGRESS UPDATE`);
        console.log(`   Processed: ${STATE.processedFiles}/${STATE.totalFiles} (${((STATE.processedFiles/STATE.totalFiles)*100).toFixed(1)}%)`);
        console.log(`   Success: ${STATE.successfulFiles} | Failed: ${STATE.failedFiles}`);
        console.log(`   Rate: ${rate.toFixed(1)} files/min`);
        console.log(`   ETA: ${eta.toFixed(1)} minutes`);
        console.log(`   Elapsed: ${(elapsed/1000/60).toFixed(1)} minutes`);
    }

    static logFileResult(fileName, success, message) {
        const status = success ? '✅' : '❌';
        const truncatedName = fileName.length > 30 ? fileName.substring(0, 27) + '...' : fileName;
        console.log(`   ${status} ${truncatedName.padEnd(30)} ${message}`);
    }

    static logMarketComplete(market, results) {
        console.log(`\n✅ ${market.name} MARKET COMPLETE`);
        console.log(`   Success: ${results.successful}/${results.total}`);
        console.log(`   Failed: ${results.failed}`);
        console.log(`   Records: ${results.totalRecords.toLocaleString()}`);
        console.log(`   Time: ${results.processingTime}`);

        if (results.errors.length > 0) {
            console.log(`   ⚠️  ${results.errors.length} errors occurred`);
        }
    }

    static logFinalSummary() {
        const elapsed = Date.now() - STATE.startTime;
        console.log('\n🎉 DATA IMPORT ORCHESTRATION COMPLETE');
        console.log('=====================================');
        console.log(`Total Files: ${STATE.totalFiles}`);
        console.log(`Successful: ${STATE.successfulFiles} (${((STATE.successfulFiles/STATE.totalFiles)*100).toFixed(1)}%)`);
        console.log(`Failed: ${STATE.failedFiles}`);
        console.log(`Total Time: ${(elapsed/1000/60).toFixed(1)} minutes`);
        console.log(`Average Rate: ${(STATE.processedFiles/(elapsed/1000/60)).toFixed(1)} files/min`);

        // Market breakdown
        console.log('\n📊 MARKET BREAKDOWN:');
        Object.entries(STATE.marketResults).forEach(([market, results]) => {
            console.log(`   ${market}: ${results.successful}/${results.total} files (${results.totalRecords.toLocaleString()} records)`);
        });

        if (STATE.errors.length > 0) {
            console.log('\n⚠️  ERRORS ENCOUNTERED:');
            STATE.errors.forEach((error, index) => {
                console.log(`   ${index + 1}. ${error}`);
            });
        }

        console.log('\n✨ Ready for trading analysis!');
    }
}

/**
 * File discovery and validation
 */
class FileDiscovery {
    static async scanMarkets() {
        const markets = {};
        let totalFiles = 0;

        for (const market of CONFIG.MARKETS) {
            const marketPath = path.join(CONFIG.STOCK_SCRAPPER_DATA_PATH, market.name);

            if (!fs.existsSync(marketPath)) {
                console.error(`❌ Market directory not found: ${marketPath}`);
                continue;
            }

            const files = fs.readdirSync(marketPath)
                .filter(file => file.endsWith('.csv'))
                .map(file => ({
                    name: file,
                    path: path.join(marketPath, file),
                    market: market.name
                }));

            markets[market.name] = {
                config: market,
                files: files,
                count: files.length
            };

            totalFiles += files.length;
        }

        STATE.totalFiles = totalFiles;
        return markets;
    }

    static async validateApiConnection() {
        try {
            const response = await axios.get(`${CONFIG.API_BASE_URL}/test/health`, { timeout: 5000 });
            return true;
        } catch (error) {
            console.error('❌ API server not accessible:', error.message);
            console.error('   Please ensure the API server is running on localhost:5245');
            return false;
        }
    }
}

/**
 * Data import execution with retry logic
 */
class DataImporter {
    static async importFile(file, retryCount = 0) {
        try {
            const response = await axios.post(`${CONFIG.API_BASE_URL}/DataImport/import-csv`, {
                filePath: file.path,
                dataSource: file.market
            }, {
                timeout: 60000, // 1 minute timeout per file
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.data.success) {
                return {
                    success: true,
                    message: `${response.data.data.recordsImported} records imported`,
                    data: response.data.data
                };
            } else {
                return {
                    success: false,
                    message: response.data.message || 'Unknown error',
                    data: null
                };
            }
        } catch (error) {
            if (retryCount < CONFIG.MAX_RETRIES) {
                console.log(`   🔄 Retry ${retryCount + 1}/${CONFIG.MAX_RETRIES} for ${file.name}`);
                await this.delay(1000 * (retryCount + 1)); // Exponential backoff
                return this.importFile(file, retryCount + 1);
            }

            return {
                success: false,
                message: `API Error: ${error.message}`,
                data: null
            };
        }
    }

    static async processBatch(files) {
        const promises = files.map(file => this.importFile(file));
        return Promise.all(promises);
    }

    static delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}

/**
 * Market-level orchestration
 */
class MarketOrchestrator {
    static async processMarket(market, files) {
        STATE.currentMarket = market.name;
        ProgressReporter.logMarketStart(market.config, files.length);

        const marketStart = Date.now();
        let successful = 0;
        let failed = 0;
        let totalRecords = 0;
        const errors = [];

        // Process files in batches
        for (let i = 0; i < files.length; i += CONFIG.BATCH_SIZE) {
            const batch = files.slice(i, i + CONFIG.BATCH_SIZE);
            const results = await DataImporter.processBatch(batch);

            // Process batch results
            for (let j = 0; j < results.length; j++) {
                const result = results[j];
                const file = batch[j];

                STATE.processedFiles++;

                if (result.success) {
                    successful++;
                    STATE.successfulFiles++;
                    totalRecords += result.data?.recordsImported || 0;
                } else {
                    failed++;
                    STATE.failedFiles++;
                    errors.push(`${file.name}: ${result.message}`);
                    STATE.errors.push(`${market.name}/${file.name}: ${result.message}`);
                }

                ProgressReporter.logFileResult(file.name, result.success, result.message);
            }

            // Progress update after each batch
            if (i + CONFIG.BATCH_SIZE < files.length) {
                ProgressReporter.logProgress();
                await DataImporter.delay(CONFIG.DELAY_BETWEEN_BATCHES);
            }
        }

        const marketElapsed = Date.now() - marketStart;
        const marketResults = {
            total: files.length,
            successful,
            failed,
            totalRecords,
            processingTime: `${(marketElapsed/1000/60).toFixed(1)} min`,
            errors
        };

        STATE.marketResults[market.name] = marketResults;
        ProgressReporter.logMarketComplete(market, marketResults);

        return marketResults;
    }
}

/**
 * Quality gates and validation
 */
class QualityGates {
    static async validateMarketData(market) {
        try {
            // Get import statistics for validation
            const today = new Date().toISOString().split('T')[0];
            const lastWeek = new Date(Date.now() - 7*24*60*60*1000).toISOString().split('T')[0];

            const response = await axios.get(`${CONFIG.API_BASE_URL}/DataImport/statistics`, {
                params: { startDate: lastWeek, endDate: today }
            });

            if (response.data.success) {
                const stats = response.data.data;
                console.log(`\n🔍 QUALITY CHECK - ${market.toUpperCase()}`);
                console.log(`   Total Records: ${stats.totalRecordsImported.toLocaleString()}`);
                console.log(`   Data Sources: ${Object.keys(stats.recordsBySource).join(', ')}`);
                console.log(`   Quality Score: ${stats.qualityStats?.avgQualityScore || 'N/A'}`);

                return true;
            }
        } catch (error) {
            console.log(`   ⚠️  Quality check failed for ${market}: ${error.message}`);
        }
        return false;
    }

    static async performFinalValidation() {
        console.log('\n🔍 PERFORMING FINAL QUALITY GATES');
        console.log('================================');

        for (const market of CONFIG.MARKETS) {
            await this.validateMarketData(market.name);
        }

        // Additional validation could include:
        // - Data completeness checks
        // - Date range validation
        // - Symbol count verification
        // - Duplicate detection
    }
}

/**
 * Main orchestration flow
 */
async function main() {
    try {
        STATE.startTime = Date.now();

        // Pre-flight checks
        console.log('🔍 Pre-flight checks...');

        const apiConnected = await FileDiscovery.validateApiConnection();
        if (!apiConnected) {
            process.exit(1);
        }

        const markets = await FileDiscovery.scanMarkets();
        if (Object.keys(markets).length === 0) {
            console.error('❌ No markets found or accessible');
            process.exit(1);
        }

        ProgressReporter.logHeader();

        // Execute market-by-market in priority order
        for (const marketConfig of CONFIG.MARKETS) {
            const market = markets[marketConfig.name];
            if (!market || market.count === 0) {
                console.log(`⏭️  Skipping ${marketConfig.name} (no files found)`);
                continue;
            }

            await MarketOrchestrator.processMarket(market, market.files);

            // Brief pause between markets
            if (marketConfig !== CONFIG.MARKETS[CONFIG.MARKETS.length - 1]) {
                await DataImporter.delay(3000);
            }
        }

        // Final quality gates
        await QualityGates.performFinalValidation();

        // Summary report
        ProgressReporter.logFinalSummary();

        // Success criteria check
        const successRate = (STATE.successfulFiles / STATE.totalFiles) * 100;
        if (successRate >= 95) {
            console.log('\n🎯 SUCCESS CRITERIA MET (>95% success rate)');
            process.exit(0);
        } else if (successRate >= 85) {
            console.log('\n⚠️  PARTIAL SUCCESS (85-95% success rate)');
            console.log('   Review failed imports and retry if needed');
            process.exit(1);
        } else {
            console.log('\n❌ QUALITY GATE FAILED (<85% success rate)');
            console.log('   Significant issues detected - manual review required');
            process.exit(1);
        }

    } catch (error) {
        console.error('\n💥 ORCHESTRATION FAILED:', error.message);
        console.error(error.stack);
        process.exit(1);
    }
}

// Execute if run directly
if (require.main === module) {
    main();
}

module.exports = {
    CONFIG,
    STATE,
    FileDiscovery,
    DataImporter,
    MarketOrchestrator,
    QualityGates,
    ProgressReporter
};