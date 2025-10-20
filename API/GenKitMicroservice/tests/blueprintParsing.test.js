const assert = require("assert");
const ParseBlueprintFlow = require("../flows/ParseBlueprintFlow");

// Test data for blueprint parsing validation
const testCases = [
  {
    name: "Basic PDF Blueprint Processing",
    input: {
      fileData:
        "data:application/pdf;base64,JVBERi0xLjAKMSAwIG9iago8PC9UeXBlIC9DYXRhbG9nL1BhZ2VzIDIgMCBSPj4KZW5kb2JqCjIgMCBvYmoKPDwvVHlwZSAvUGFnZXMvS2lkcyBbMyAwIFJdL0NvdW50IDE+PgplbmRvYmoKMyAwIG9iago8PC9UeXBlIC9QYWdlL01lZGlhQm94IFswIDAgNjEyIDc5Ml0vUGFyZW50IDIgMCBSL1Jlc291cmNlcyA8PC9Gb250IDw8L0YxIDQgMCBSPj4+Pi9Db250ZW50cyA1IDAgUj4+CmVuZG9iagw0IDAgb2JqCjw8L1R5cGUgL0ZvbnQvU3VidHlwZSAvVHlwZTEvQmFzZUZvbnQgL0hlbHZldGljYT4+CmVuZG9iagw1IDAgb2JqCjw8L0xlbmd0aCA0ND4+CnN0cmVhbQpCVAovRjEgMjQgVGYgCjEwMCAxMDAgVEQgCihIZWxsbyBXb3JsZCkgVEogCmVuZHN0cmVhbQplbmRvYmoKeHJlZgowIDYKMDAwMDAwMDAwMCA2NTUzNSBmIAowMDAwMDAwMDEwIDAwMDAwIG4gCjAwMDAwMDAwNzQgMDAwMDAgbiAKMDAwMDAwMDE0MCAwMDAwMCBuIAowMDAwMDAwMjA2IDAwMDAwIG4gCjAwMDAwMDAzMjUgMDAwMDAgbiAKdHJhaWxlcgo8PC9TaXplIDYvUm9vdCAxIDAgUj4+CnN0YXJ0eHJlZgowMDAwMDAwMDAwCnN0YXJ0eHJlZiAxNjYKYWVudGRvYmoK",
      fileType: "pdf",
      projectContext: {
        projectId: "TEST_PROJECT_001",
        projectType: "residential",
        buildingType: "single_family_home",
      },
    },
    expected: {
      hasLineItems: true,
      hasMetadata: true,
      hasSummary: true,
      minLineItems: 5,
      hasDemolitionCategory: false, // Basic test shouldn't need demolition
      hasSitePreparationCategory: true,
      averageConfidence: (confidence) => confidence > 0.5,
      coveragePercentage: (coverage) => coverage > 50,
    },
  },
  {
    name: "Image Blueprint Processing",
    input: {
      fileData:
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==",
      fileType: "image",
      projectContext: {
        projectId: "TEST_PROJECT_002",
        projectType: "commercial",
        buildingType: "office_building",
      },
    },
    expected: {
      hasLineItems: true,
      hasMetadata: true,
      hasSummary: true,
      minLineItems: 3,
      hasMEPCategory: true,
      averageConfidence: (confidence) => confidence > 0.4, // Vision AI may be less accurate
      coveragePercentage: (coverage) => coverage > 30,
    },
  },
  {
    name: "Blueprint with Existing Structure (Demolition Required)",
    input: {
      fileData:
        "data:application/pdf;base64,JVBERi0xLjAKMSAwIG9iagouLi4KZW5kb2JqCjI2IDAgb2JqCjw8L1R5cGUgL1BhZ2UvUGFyZW50IDEgMCBSL01lZGlhQm94IFswIDAgNjEyIDc5Ml0KL1Jlc291cmNlcyA8PC9Gb250IDw8L0YxIDQgMCBSPj4+Pi9Db250ZW50cyAyNyAwIFIPj4KZW5kb2JqCjI3IDAgb2JqCjw8L0xlbmd0aCAxMjM0PgplbmRvYmoK",
      fileType: "pdf",
      projectContext: {
        projectId: "TEST_PROJECT_003",
        projectType: "renovation",
        buildingType: "existing_structure",
        requiresDemolition: true,
      },
    },
    expected: {
      hasLineItems: true,
      hasMetadata: true,
      hasSummary: true,
      minLineItems: 8,
      hasDemolitionCategory: true,
      hasSitePreparationCategory: true,
      hasProjectOverheadCategory: true,
      averageConfidence: (confidence) => confidence > 0.6,
      coveragePercentage: (coverage) => coverage > 70,
    },
  },
];

// Comprehensive testing function
async function runBlueprintParsingTests() {
  console.log("🧪 Starting Blueprint Parsing Tests...\n");

  const results = {
    passed: 0,
    failed: 0,
    total: testCases.length,
    details: [],
  };

  for (let i = 0; i < testCases.length; i++) {
    const testCase = testCases[i];
    console.log(`\n📋 Test ${i + 1}: ${testCase.name}`);

    try {
      // Run the blueprint parsing flow
      const result = await ParseBlueprintFlow(testCase.input);

      // Validate results against expectations
      const validation = validateTestResult(result, testCase.expected);

      if (validation.passed) {
        console.log(`✅ PASSED: ${validation.message}`);
        results.passed++;
        results.details.push({
          test: testCase.name,
          status: "PASSED",
          message: validation.message,
          result: result,
        });
      } else {
        console.log(`❌ FAILED: ${validation.message}`);
        results.failed++;
        results.details.push({
          test: testCase.name,
          status: "FAILED",
          message: validation.message,
          result: result,
        });
      }
    } catch (error) {
      console.log(`💥 ERROR: ${error.message}`);
      results.failed++;
      results.details.push({
        test: testCase.name,
        status: "ERROR",
        message: error.message,
        error: error,
      });
    }
  }

  // Print summary
  console.log(`\n📊 Test Summary:`);
  console.log(`✅ Passed: ${results.passed}`);
  console.log(`❌ Failed: ${results.failed}`);
  console.log(
    `📈 Success Rate: ${((results.passed / results.total) * 100).toFixed(1)}%`
  );

  if (results.failed === 0) {
    console.log(`🎉 All tests passed! Blueprint parsing is working correctly.`);
  } else {
    console.log(`⚠️  Some tests failed. Please review the implementation.`);
  }

  return results;
}

// Validation function for test results
function validateTestResult(result, expected) {
  try {
    // Check basic structure
    if (!result.lineItems || !Array.isArray(result.lineItems)) {
      return { passed: false, message: "Result missing lineItems array" };
    }

    if (!result.metadata) {
      return { passed: false, message: "Result missing metadata" };
    }

    if (!result.summary) {
      return { passed: false, message: "Result missing summary" };
    }

    // Check minimum line items
    if (result.lineItems.length < expected.minLineItems) {
      return {
        passed: false,
        message: `Insufficient line items: got ${result.lineItems.length}, expected at least ${expected.minLineItems}`,
      };
    }

    // Check for required categories
    const categories = [
      ...new Set(result.lineItems.map((item) => item.category)),
    ];

    if (expected.hasDemolitionCategory && !categories.includes("Demolition")) {
      return { passed: false, message: "Missing required Demolition category" };
    }

    if (
      expected.hasSitePreparationCategory &&
      !categories.includes("Site Preparation")
    ) {
      return {
        passed: false,
        message: "Missing required Site Preparation category",
      };
    }

    if (
      expected.hasMEPCategory &&
      !categories.some(
        (cat) =>
          cat.includes("MEP") ||
          cat === "Electrical" ||
          cat === "Plumbing" ||
          cat === "Mechanical"
      )
    ) {
      return { passed: false, message: "Missing required MEP category" };
    }

    if (
      expected.hasProjectOverheadCategory &&
      !categories.includes("Project Overhead")
    ) {
      return {
        passed: false,
        message: "Missing required Project Overhead category",
      };
    }

    // Check confidence score
    if (
      result.metadata.confidence !== undefined &&
      !expected.averageConfidence(result.metadata.confidence)
    ) {
      return {
        passed: false,
        message: `Low confidence score: ${result.metadata.confidence}, expected higher than threshold`,
      };
    }

    // Check coverage percentage
    if (
      result.metadata.coverage !== undefined &&
      !expected.coveragePercentage(result.metadata.coverage)
    ) {
      return {
        passed: false,
        message: `Low coverage percentage: ${result.metadata.coverage}%, expected higher than threshold`,
      };
    }

    // Validate line item structure
    for (const item of result.lineItems) {
      if (!item.itemId || !item.name || !item.category) {
        return {
          passed: false,
          message: `Invalid line item structure: missing required fields`,
        };
      }

      if (
        typeof item.quantity !== "number" ||
        typeof item.unitPrice !== "number"
      ) {
        return {
          passed: false,
          message: `Invalid line item pricing: quantity and unitPrice must be numbers`,
        };
      }

      if (item.lineTotal !== item.quantity * item.unitPrice) {
        return {
          passed: false,
          message: `Line total mismatch for item: ${item.name}`,
        };
      }
    }

    return {
      passed: true,
      message: `All validations passed: ${
        result.lineItems.length
      } line items, ${result.metadata.confidence || "N/A"} confidence, ${
        result.metadata.coverage || "N/A"
      }% coverage`,
    };
  } catch (error) {
    return { passed: false, message: `Validation error: ${error.message}` };
  }
}

// Performance testing function
async function runPerformanceTests() {
  console.log("\n🚀 Running Performance Tests...");

  const iterations = 5;
  const startTime = Date.now();

  try {
    for (let i = 0; i < iterations; i++) {
      console.log(`Performance test iteration ${i + 1}/${iterations}`);

      // Use a simple test case for performance testing
      const simpleTestCase = testCases[0];
      await ParseBlueprintFlow(simpleTestCase.input);
    }

    const totalTime = Date.now() - startTime;
    const averageTime = totalTime / iterations;

    console.log(`⏱️  Performance Results:`);
    console.log(`   Total time: ${totalTime}ms`);
    console.log(`   Average time per test: ${averageTime.toFixed(2)}ms`);
    console.log(
      `   Throughput: ${(1000 / averageTime).toFixed(2)} tests/second`
    );

    if (averageTime < 5000) {
      // Less than 5 seconds
      console.log(`✅ Performance test PASSED: Processing time is acceptable`);
    } else {
      console.log(
        `⚠️  Performance test WARNING: Processing time may be too slow for production`
      );
    }
  } catch (error) {
    console.log(`❌ Performance test ERROR: ${error.message}`);
  }
}

// Error handling and edge case testing
async function runEdgeCaseTests() {
  console.log("\n🔍 Running Edge Case Tests...");

  const edgeCases = [
    {
      name: "Empty file data",
      input: { fileData: "", fileType: "pdf" },
      shouldHandleGracefully: true,
    },
    {
      name: "Invalid file type",
      input: { fileData: "test", fileType: "invalid" },
      shouldHandleGracefully: true,
    },
    {
      name: "Missing project context",
      input: {
        fileData: "data:application/pdf;base64,JVBERi0xLjAK",
        fileType: "pdf",
      },
      shouldHandleGracefully: true,
    },
    {
      name: "Corrupted PDF data",
      input: {
        fileData: "data:application/pdf;base64,corrupted_data_here",
        fileType: "pdf",
      },
      shouldHandleGracefully: true,
    },
  ];

  for (const edgeCase of edgeCases) {
    try {
      console.log(`Testing edge case: ${edgeCase.name}`);
      const result = await ParseBlueprintFlow(edgeCase.input);

      if (edgeCase.shouldHandleGracefully) {
        // Should either succeed or fail gracefully with fallback
        if (result.success === false && result.metadata?.fallbackUsed) {
          console.log(`✅ ${edgeCase.name}: Handled gracefully with fallback`);
        } else if (result.success === true) {
          console.log(
            `✅ ${edgeCase.name}: Processed successfully despite edge case`
          );
        } else {
          console.log(`⚠️  ${edgeCase.name}: Unexpected result structure`);
        }
      }
    } catch (error) {
      if (edgeCase.shouldHandleGracefully) {
        console.log(`✅ ${edgeCase.name}: Failed gracefully as expected`);
      } else {
        console.log(`❌ ${edgeCase.name}: Unexpected error: ${error.message}`);
      }
    }
  }
}

// Main test runner
async function runAllTests() {
  try {
    // Run main functionality tests
    const mainResults = await runBlueprintParsingTests();

    // Run performance tests
    await runPerformanceTests();

    // Run edge case tests
    await runEdgeCaseTests();

    console.log("\n🏁 All tests completed!");

    // Exit with appropriate code for CI/CD integration
    process.exit(mainResults.failed > 0 ? 1 : 0);
  } catch (error) {
    console.error("Test runner error:", error);
    process.exit(1);
  }
}

// Export functions for external use
module.exports = {
  runAllTests,
  runBlueprintParsingTests,
  runPerformanceTests,
  runEdgeCaseTests,
  validateTestResult,
};

// Run tests if this file is executed directly
if (require.main === module) {
  runAllTests();
}
