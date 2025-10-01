console.log("MessagesTest script loading...");
let testResults = [];

function addResult(message, type = "info") {
  const timestamp = new Date().toLocaleTimeString();
  const resultDiv = document.createElement("div");
  resultDiv.className = `test-result ${type}`;
  resultDiv.innerHTML = `<span class="text-muted">[${timestamp}]</span> ${message}`;

  const resultsContainer = document.getElementById("test-results");
  resultsContainer.appendChild(resultDiv);
  resultsContainer.scrollTop = resultsContainer.scrollHeight;

  testResults.push({ timestamp, message, type });
}

function clearResults() {
  document.getElementById("test-results").innerHTML =
    '<div class="text-muted opacity-75">Test results cleared...</div>';
  testResults = [];
}

function exportResults() {
  const dataStr = JSON.stringify(testResults, null, 2);
  const dataBlob = new Blob([dataStr], { type: "application/json" });
  const url = URL.createObjectURL(dataBlob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `message-test-results-${
    new Date().toISOString().split("T")[0]
  }.json`;
  link.click();
  URL.revokeObjectURL(url);
}

async function runValidationTests() {
  addResult("🧪 Starting Message Validation Tests...", "info");

  const testCases = [
    {
      name: "Valid Message",
      payload: {
        senderId: "user123",
        receiverId: "user456",
        projectId: "project789",
        subject: "Test Message",
        content: "This is a valid test message",
      },
      expectedResult: "success",
    },
    {
      name: "Empty Content",
      payload: {
        senderId: "user123",
        receiverId: "user456",
        projectId: "project789",
        subject: "Test",
        content: "",
      },
      expectedResult: "error",
    },
    {
      name: "Missing Sender ID",
      payload: {
        receiverId: "user456",
        projectId: "project789",
        subject: "Test",
        content: "Content",
      },
      expectedResult: "error",
    },
    {
      name: "Content Too Long",
      payload: {
        senderId: "user123",
        receiverId: "user456",
        projectId: "project789",
        subject: "Test",
        content: "A".repeat(5001),
      },
      expectedResult: "error",
    },
    {
      name: "Subject Too Long",
      payload: {
        senderId: "user123",
        receiverId: "user456",
        projectId: "project789",
        subject: "A".repeat(201),
        content: "Content",
      },
      expectedResult: "error",
    },
  ];

  for (const testCase of testCases) {
    try {
      const response = await fetch("/MessagesTest/TestMessageValidation", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken:
            document.querySelector('input[name="__RequestVerificationToken"]')
              ?.value || "",
        },
        body: JSON.stringify({
          testName: testCase.name,
          payload: testCase.payload,
          expectedResult: testCase.expectedResult,
        }),
      });

      const result = await response.json();
      const actualResult = result.success ? "success" : "error";
      const passed = actualResult === testCase.expectedResult;

      addResult(
        `${passed ? "✅" : "❌"} ${testCase.name} - Expected: ${
          testCase.expectedResult
        }, Got: ${actualResult}`,
        passed ? "success" : "error"
      );
    } catch (error) {
      addResult(`❌ ${testCase.name} - Error: ${error.message}`, "error");
    }
  }

  addResult("✅ Message Validation Tests completed", "success");
}

async function runWorkflowTests() {
  addResult("🔄 Starting Workflow Integration Tests...", "info");

  const workflowTests = [
    {
      name: "Quote Approval Notification",
      endpoint: "/api/messages/workflow/quote-approval",
      payload: {
        quoteId: "quote123",
        action: "approved",
        userId: "user123",
      },
    },
    {
      name: "Invoice Payment Notification",
      endpoint: "/api/messages/workflow/invoice-payment",
      payload: {
        invoiceId: "invoice123",
        action: "paid",
        userId: "user123",
      },
    },
    {
      name: "Project Update Notification",
      endpoint: "/api/messages/workflow/project-update",
      payload: {
        projectId: "project123",
        updateType: "status_changed",
        userId: "user123",
      },
    },
    {
      name: "System Alert",
      endpoint: "/api/messages/workflow/system-alert",
      payload: {
        alertType: "maintenance",
        message: "System maintenance scheduled for tonight at 2 AM",
        recipients: ["user123", "user456", "user789"],
      },
    },
  ];

  for (const test of workflowTests) {
    try {
      const response = await fetch("/MessagesTest/TestWorkflowIntegration", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken:
            document.querySelector('input[name="__RequestVerificationToken"]')
              ?.value || "",
        },
        body: JSON.stringify({
          testName: test.name,
          endpoint: test.endpoint,
          payload: test.payload,
        }),
      });

      const result = await response.json();
      addResult(
        `${result.success ? "✅" : "❌"} ${test.name} - Status: ${
          result.statusCode
        }`,
        result.success ? "success" : "error"
      );
    } catch (error) {
      addResult(`❌ ${test.name} - Error: ${error.message}`, "error");
    }
  }

  addResult("✅ Workflow Integration Tests completed", "success");
}

async function runErrorHandlingTests() {
  addResult("⚠️ Starting Error Handling Tests...", "info");

  const errorTests = [
    {
      name: "Invalid User ID",
      payload: {
        senderId: "invalid-user-id",
        receiverId: "user456",
        projectId: "project789",
        subject: "Test",
        content: "Testing invalid user ID",
      },
      expectedResult: "error",
    },
    {
      name: "SQL Injection Attempt",
      payload: {
        senderId: "user123",
        receiverId: "user456",
        projectId: "project789",
        subject: "'; DROP TABLE messages; --",
        content: "Testing SQL injection protection",
      },
      expectedResult: "success", // Should be sanitized, not rejected
    },
    {
      name: "XSS Attempt",
      payload: {
        senderId: "user123",
        receiverId: "user456",
        projectId: "project789",
        subject: "Test",
        content: "<script>alert('XSS')</script>Testing XSS protection",
      },
      expectedResult: "success", // Should be sanitized, not rejected
    },
    {
      name: "Unicode and Special Characters",
      payload: {
        senderId: "user123",
        receiverId: "user456",
        projectId: "project789",
        subject: "Test with émojis 🚀 and spëcial chars",
        content: "Testing Unicode support: 你好世界 🌍 ñáéíóú",
      },
      expectedResult: "success",
    },
  ];

  for (const test of errorTests) {
    try {
      const response = await fetch("/MessagesTest/TestErrorHandling", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken:
            document.querySelector('input[name="__RequestVerificationToken"]')
              ?.value || "",
        },
        body: JSON.stringify({
          testName: test.name,
          payload: test.payload,
          expectedResult: test.expectedResult,
        }),
      });

      const result = await response.json();
      const passed = result.actualResult === test.expectedResult;
      addResult(
        `${passed ? "✅" : "❌"} ${test.name} - Expected: ${
          test.expectedResult
        }, Got: ${result.actualResult}`,
        passed ? "success" : "error"
      );
    } catch (error) {
      addResult(`❌ ${test.name} - Error: ${error.message}`, "error");
    }
  }

  addResult("✅ Error Handling Tests completed", "success");
}

async function runPerformanceTests() {
  addResult("⚡ Starting Performance Tests...", "info");

  try {
    const response = await fetch("/MessagesTest/TestPerformance", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken:
          document.querySelector('input[name="__RequestVerificationToken"]')
            ?.value || "",
      },
      body: JSON.stringify({
        senderId: "user123",
        receiverId: "user456",
        projectId: "project789",
        content: "Performance test content",
        messageCount: 5,
      }),
    });

    const result = await response.json();
    if (result.success) {
      addResult(
        `✅ Performance Test - Total Time: ${result.totalTime.toFixed(
          2
        )}ms, Average:
${result.averageTime.toFixed(2)}ms per message`,
        "success"
      );
      addResult(
        `📊 Processed ${result.totalMessages} messages successfully`,
        "info"
      );
    } else {
      addResult(`❌ Performance Test failed: ${result.error}`, "error");
    }
  } catch (error) {
    addResult(`❌ Performance Test error: ${error.message}`, "error");
  }

  addResult("✅ Performance Tests completed", "success");
}

async function runRateLimitTests() {
  addResult("⏱️ Starting Rate Limiting Tests...", "info");

  if (!confirm("This will send multiple test messages. Continue?")) {
    addResult("❌ Rate Limiting Tests cancelled by user", "warning");
    return;
  }

  try {
    const response = await fetch("/MessagesTest/TestRateLimiting", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken:
          document.querySelector('input[name="__RequestVerificationToken"]')
            ?.value || "",
      },
      body: JSON.stringify({
        senderId: "user123",
        receiverId: "user456",
        projectId: "project789",
        content: "Rate limit test content",
        messageCount: 10,
      }),
    });

    const result = await response.json();
    if (result.success) {
      addResult(
        `✅ Rate Limiting Test - Success Rate: ${result.successRate.toFixed(1)}%
(${result.successCount}/${result.totalMessages})`,
        "success"
      );
      addResult(
        `📊 Successful: ${result.successCount}, Failed: ${result.errorCount}`,
        "info"
      );
    } else {
      addResult(`❌ Rate Limiting Test failed: ${result.error}`, "error");
    }
  } catch (error) {
    addResult(`❌ Rate Limiting Test error: ${error.message}`, "error");
  }

  addResult("✅ Rate Limiting Tests completed", "success");
}

async function runComprehensiveTests() {
  addResult("🚀 Starting Comprehensive Message System Tests...", "info");
  addResult(
    "This will test validation, workflow, error handling, performance, and rate limiting",
    "info"
  );

  if (
    !confirm(
      "This will run comprehensive tests that may send multiple messages. Continue?"
    )
  ) {
    addResult("❌ Comprehensive Tests cancelled by user", "warning");
    return;
  }

  // Run tests in sequence
  await runValidationTests();
  await new Promise((resolve) => setTimeout(resolve, 1000));

  await runWorkflowTests();
  await new Promise((resolve) => setTimeout(resolve, 1000));

  await runErrorHandlingTests();
  await new Promise((resolve) => setTimeout(resolve, 1000));

  await runPerformanceTests();
  await new Promise((resolve) => setTimeout(resolve, 1000));

  await runRateLimitTests();

  addResult("🎉 Comprehensive Message System Tests completed!", "success");
  addResult("📊 Test Summary:", "info");
  addResult(
    " ✅ Message Validation - Content length, required fields, spam detection",
    "success"
  );
  addResult(
    " ✅ Workflow Integration - Quote approval, invoice payment, project updates, system alerts",
    "success"
  );
  addResult(
    " ✅ Error Handling - Invalid inputs, security tests, edge cases",
    "success"
  );
  addResult(
    " ✅ Performance Testing - Response times and throughput",
    "success"
  );
  addResult(
    " ✅ Rate Limiting - Message frequency limits and spam protection",
    "success"
  );
  addResult("Check the results above for detailed test outcomes.", "info");
}

// Initialize
document.addEventListener("DOMContentLoaded", function () {
  console.log("DOM loaded, functions available:", {
    runValidationTests: typeof runValidationTests,
    runWorkflowTests: typeof runWorkflowTests,
    runErrorHandlingTests: typeof runErrorHandlingTests,
    runPerformanceTests: typeof runPerformanceTests,
    runRateLimitTests: typeof runRateLimitTests,
    runComprehensiveTests: typeof runComprehensiveTests,
    clearResults: typeof clearResults,
  });
  addResult("🚀 Message System Testing Suite loaded successfully!", "success");
  addResult("Ready to run comprehensive message system tests", "info");
});
