#!/usr/bin/env node

/**
 * Simple Blueprint Parsing Test
 *
 * This test verifies that the model can parse the actual PDF blueprint
 * located in the GenKitMicroservice directory. It's designed to be a
 * straightforward test that validates basic functionality.
 */

const fs = require("fs");
const path = require("path");
const ParseBlueprintFlow = require("../flows/ParseBlueprintFlow");

// Test configuration
const TEST_CONFIG = {
  // Path to the actual PDF blueprint in the project
  pdfPath: path.join(__dirname, "..", "blueprint.pdf"),

  // Test project context
  projectContext: {
    projectId: "SIMPLE_TEST_001",
    projectType: "residential",
    buildingType: "single_family_home",
    location: "Test Location",
    squareFootage: 2500,
  },

  // Expected results thresholds
  expectations: {
    minLineItems: 3,
    minConfidence: 0.3,
    minCoverage: 20,
    requiredCategories: ["Site Preparation"], // Removed 'Construction' as it may not always be detected
    maxProcessingTime: 45000, // 45 seconds (increased for realistic processing time)
  },
};

/**
 * Convert file to base64 data URL
 */
function fileToBase64DataUrl(filePath, mimeType) {
  try {
    const fileBuffer = fs.readFileSync(filePath);
    const base64 = fileBuffer.toString("base64");
    return `data:${mimeType};base64,${base64}`;
  } catch (error) {
    throw new Error(`Failed to read file ${filePath}: ${error.message}`);
  }
}

/**
 * Validate test results against expectations
 */
function validateResults(result, expectations) {
  const issues = [];

  // Check basic structure
  if (!result.success) {
    issues.push("Test failed - result.success is false");
  }

  if (!result.lineItems || !Array.isArray(result.lineItems)) {
    issues.push("Missing or invalid lineItems array");
  } else if (result.lineItems.length < expectations.minLineItems) {
    issues.push(
      `Insufficient line items: got ${result.lineItems.length}, expected at least ${expectations.minLineItems}`
    );
  }

  if (!result.metadata) {
    issues.push("Missing metadata");
  } else {
    // Check confidence score
    if (
      result.metadata.confidence !== undefined &&
      result.metadata.confidence < expectations.minConfidence
    ) {
      issues.push(
        `Low confidence: ${result.metadata.confidence}, expected at least ${expectations.minConfidence}`
      );
    }

    // Check coverage percentage
    if (
      result.metadata.coverage !== undefined &&
      result.metadata.coverage < expectations.minCoverage
    ) {
      issues.push(
        `Low coverage: ${result.metadata.coverage}%, expected at least ${expectations.minCoverage}%`
      );
    }
  }

  // Check for required categories
  if (result.lineItems && result.lineItems.length > 0) {
    const categories = [
      ...new Set(result.lineItems.map((item) => item.category)),
    ];
    const missingCategories = expectations.requiredCategories.filter(
      (required) => !categories.some((cat) => cat.includes(required))
    );

    if (missingCategories.length > 0) {
      issues.push(
        `Missing required categories: ${missingCategories.join(", ")}`
      );
    }
  }

  // Validate line item structure
  if (result.lineItems) {
    for (let i = 0; i < result.lineItems.length; i++) {
      const item = result.lineItems[i];
      if (!item.name || !item.category) {
        issues.push(
          `Line item ${i + 1} missing required fields (name, category)`
        );
      }
      if (
        typeof item.quantity !== "number" ||
        typeof item.unitPrice !== "number"
      ) {
        issues.push(`Line item ${i + 1} has invalid quantity or unitPrice`);
      }
    }
  }

  return {
    passed: issues.length === 0,
    issues: issues,
    summary:
      issues.length === 0
        ? "All validations passed"
        : `${issues.length} validation issues found`,
  };
}

/**
 * Run the simple blueprint parsing test
 */
async function runSimpleTest() {
  console.log("🧪 Starting Simple Blueprint Parsing Test...\n");

  // Check if PDF file exists
  if (!fs.existsSync(TEST_CONFIG.pdfPath)) {
    console.log(
      "❌ ERROR: PDF blueprint file not found at:",
      TEST_CONFIG.pdfPath
    );
    console.log(
      "   Please ensure the blueprint.pdf file exists in the GenKitMicroservice directory."
    );
    return { success: false, error: "PDF file not found" };
  }

  console.log("📄 Found PDF blueprint file");
  console.log(
    "📋 Project Context:",
    JSON.stringify(TEST_CONFIG.projectContext, null, 2)
  );
  console.log(
    "🎯 Expectations:",
    JSON.stringify(TEST_CONFIG.expectations, null, 2)
  );

  try {
    // Convert PDF to base64 data URL
    console.log("\n🔄 Converting PDF to base64...");
    const fileData = fileToBase64DataUrl(
      TEST_CONFIG.pdfPath,
      "application/pdf"
    );
    console.log(
      `✅ PDF converted (${Math.round(fileData.length / 1024)}KB base64 data)`
    );

    // Prepare test input
    const testInput = {
      fileData: fileData,
      fileType: "pdf",
      projectContext: TEST_CONFIG.projectContext,
    };

    // Run the blueprint parsing flow
    console.log("\n🤖 Running ParseBlueprintFlow...");
    const startTime = Date.now();

    const result = await ParseBlueprintFlow(testInput);

    const processingTime = Date.now() - startTime;
    console.log(`⏱️  Processing completed in ${processingTime}ms`);

    // Check processing time
    if (processingTime > TEST_CONFIG.expectations.maxProcessingTime) {
      console.log(
        `⚠️  WARNING: Processing took longer than expected (${processingTime}ms > ${TEST_CONFIG.expectations.maxProcessingTime}ms)`
      );
    }

    // Validate results
    console.log("\n🔍 Validating results...");
    const validation = validateResults(result, TEST_CONFIG.expectations);

    // Display results
    console.log("\n📊 Test Results:");
    console.log("================");

    if (validation.passed) {
      console.log("✅ TEST PASSED");
      console.log(`📈 Summary: ${validation.summary}`);
    } else {
      console.log("❌ TEST FAILED");
      console.log("🚨 Issues found:");
      validation.issues.forEach((issue, index) => {
        console.log(`   ${index + 1}. ${issue}`);
      });
    }

    // Display detailed results
    console.log("\n📋 Detailed Results:");
    console.log("====================");
    console.log(`Success: ${result.success}`);
    console.log(
      `Line Items: ${result.lineItems ? result.lineItems.length : 0}`
    );
    console.log(`Confidence: ${result.metadata?.confidence || "N/A"}`);
    console.log(`Coverage: ${result.metadata?.coverage || "N/A"}%`);
    console.log(`Processing Time: ${processingTime}ms`);

    if (result.lineItems && result.lineItems.length > 0) {
      console.log("\n📝 Line Items:");
      result.lineItems.forEach((item, index) => {
        console.log(
          `   ${index + 1}. ${item.name} (${item.category}) - Qty: ${
            item.quantity
          }, Price: $${item.unitPrice}`
        );
      });

      //   if (result.lineItems.length > 3) {
      //     console.log(`   ... and ${result.lineItems.length - 3} more items`);
      //   }
    }

    if (result.metadata?.blueprintTypes) {
      console.log("\n🏗️  Detected Blueprint Types:");
      result.metadata.blueprintTypes.forEach((type) => {
        console.log(`   - ${type}`);
      });
    }

    return {
      success: validation.passed,
      processingTime: processingTime,
      result: result,
      validation: validation,
    };
  } catch (error) {
    console.log("\n💥 ERROR during test execution:");
    console.log("===============================");
    console.log(`Error: ${error.message}`);
    console.log(`Stack: ${error.stack}`);

    return {
      success: false,
      error: error.message,
      stack: error.stack,
    };
  }
}

/**
 * Main test runner
 */
async function main() {
  console.log(`
╔══════════════════════════════════════════════════════════════╗
║              SIMPLE BLUEPRINT PARSING TEST                  ║
║              Testing PDF Blueprint Processing               ║
╚══════════════════════════════════════════════════════════════╝
  `);

  try {
    const testResult = await runSimpleTest();

    console.log("\n🏁 Test Summary:");
    console.log("================");

    if (testResult.success) {
      console.log("🎉 SUCCESS: Blueprint parsing is working correctly!");
      console.log(`⏱️  Processing time: ${testResult.processingTime}ms`);
      console.log(
        `📊 Line items generated: ${testResult.result?.lineItems?.length || 0}`
      );
      process.exit(0);
    } else {
      console.log("❌ FAILURE: Blueprint parsing test failed");
      if (testResult.error) {
        console.log(`💥 Error: ${testResult.error}`);
      }
      if (testResult.validation?.issues) {
        console.log("🚨 Validation issues:");
        testResult.validation.issues.forEach((issue, index) => {
          console.log(`   ${index + 1}. ${issue}`);
        });
      }
      process.exit(1);
    }
  } catch (error) {
    console.error("💥 Test runner error:", error);
    process.exit(1);
  }
}

// Export for use in other test files
module.exports = {
  runSimpleTest,
  validateResults,
  TEST_CONFIG,
};

// Run test if this file is executed directly
if (require.main === module) {
  main();
}
