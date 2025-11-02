#!/usr/bin/env node

/**
 * Simple Test Runner for Blueprint Parsing
 *
 * This script runs the simple blueprint parsing test to verify
 * that the model can parse the PDF blueprint in the project.
 *
 * Usage: node run-simple-test.js
 */

// Load environment variables
require("dotenv").config();

const { runSimpleTest } = require("./tests/simpleBlueprintParsing.test");

async function main() {
  console.log("🚀 Running Simple Blueprint Parsing Test...\n");

  try {
    const result = await runSimpleTest();

    if (result.success) {
      console.log("\n🎉 Test completed successfully!");
      console.log("✅ The model can parse the PDF blueprint correctly.");
    } else {
      console.log("\n❌ Test failed!");
      console.log("🔧 Please check the implementation and try again.");
    }
  } catch (error) {
    console.error("💥 Test runner error:", error);
    process.exit(1);
  }
}

main();
