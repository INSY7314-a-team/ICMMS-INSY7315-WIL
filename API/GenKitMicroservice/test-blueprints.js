#!/usr/bin/env node

// Load environment variables
require("dotenv").config();

/**
 * Blueprint Processing Test Runner
 *
 * This script runs comprehensive tests for the enhanced blueprint parsing system.
 * It validates all aspects of the agent orchestration flow including:
 * - Text extraction from various file formats
 * - AI-powered line item generation
 * - Holistic coverage (demolition, site prep, overhead)
 * - Validation and confidence scoring
 * - PM review workflow
 * - Error handling and fallbacks
 *
 * Usage: node test-blueprints.js
 */

const { runAllTests } = require("./tests/blueprintParsing.test");

async function main() {
  console.log(`
╔══════════════════════════════════════════════════════════════╗
║              BLUEPRINT PARSING TEST SUITE                    ║
║              Enhanced Agent Orchestration Flow               ║
╚══════════════════════════════════════════════════════════════╝
  `);

  try {
    await runAllTests();
  } catch (error) {
    console.error("Test suite failed:", error);
    process.exit(1);
  }
}

// Handle unhandled promise rejections
process.on("unhandledRejection", (error) => {
  console.error("Unhandled promise rejection:", error);
  process.exit(1);
});

// Handle uncaught exceptions
process.on("uncaughtException", (error) => {
  console.error("Uncaught exception:", error);
  process.exit(1);
});

main();
