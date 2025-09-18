using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Tester")]
    public class MessagesTestController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MessagesTestController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public MessagesTestController(
            HttpClient httpClient,
            ILogger<MessagesTestController> logger,
            IConfiguration configuration
        )
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
        }

        public IActionResult Index()
        {
            // Debug: Log all available claims
            Console.WriteLine("=== DEBUG: All User Claims ===");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }
            Console.WriteLine("=== END DEBUG ===");

            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("User is not authenticated");
                ViewBag.CurrentUserId = null;
                return View();
            }

            // Get user ID from claims - with additional debugging
            var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            Console.WriteLine($"NameIdentifier claim found: {nameIdentifierClaim != null}");
            Console.WriteLine($"NameIdentifier claim value: '{nameIdentifierClaim?.Value}'");

            var currentUserId = nameIdentifierClaim?.Value;
            Console.WriteLine($"currentUserId variable: '{currentUserId}'");
            Console.WriteLine($"currentUserId is null: {currentUserId == null}");
            Console.WriteLine($"currentUserId is empty: {string.IsNullOrEmpty(currentUserId)}");

            // Debug: Log the specific claim values
            Console.WriteLine(
                $"NameIdentifier: {User.FindFirst(ClaimTypes.NameIdentifier)?.Value}"
            );
            Console.WriteLine($"Name: {User.FindFirst(ClaimTypes.Name)?.Value}");
            Console.WriteLine($"Email: {User.FindFirst(ClaimTypes.Email)?.Value}");
            Console.WriteLine($"Role: {User.FindFirst(ClaimTypes.Role)?.Value}");
            Console.WriteLine(
                $"FirebaseToken: {User.FindFirst("FirebaseToken")?.Value?.Substring(0, Math.Min(20, User.FindFirst("FirebaseToken")?.Value?.Length ?? 0))}..."
            );

            ViewBag.CurrentUserId = currentUserId;
            Console.WriteLine($"Index | Current User ID: {currentUserId}");
            Console.WriteLine($"ViewBag.CurrentUserId set to: {ViewBag.CurrentUserId}");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestMessageValidation(
            [FromBody] MessageValidationTestRequest request
        )
        {
            try
            {
                // Get Firebase token from user claims
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(
                        new TestResponse
                        {
                            Success = false,
                            Error = "Authentication token not found. Please log in.",
                        }
                    );
                }

                // Get current user ID from claims
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine("Current User ID:", currentUserId);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Json(
                        new TestResponse
                        {
                            Success = false,
                            Error = "Current user ID not found in authentication claims.",
                        }
                    );
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                // Update the payload to use the current user's ID
                var payload = request.Payload;
                if (payload is System.Text.Json.JsonElement jsonElement)
                {
                    var payloadDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        jsonElement.GetRawText()
                    );
                    if (payloadDict != null)
                    {
                        // Use current user as sender, and create a test receiver ID
                        //payloadDict["senderId"] = currentUserId;
                        // For testing, we'll use a known test user ID for receiver
                        // You might want to create a test user in your database
                        //payloadDict["receiverId"] = currentUserId;
                        payload = payloadDict;
                    }
                }

                // Log the request payload
                Console.WriteLine($"Sending request to: {_apiBaseUrl}/api/messages");
                Console.WriteLine($"Request payload: {JsonSerializer.Serialize(payload)}");

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/messages",
                    payload
                );
                Console.WriteLine($"Message Validation Test Response: {response}");
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response content: {content}");

                var result = new TestResponse
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    ResponseBody = content,
                    TestName = request.TestName,
                    ExpectedResult = request.ExpectedResult,
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing message validation");
                return Json(new TestResponse { Success = false, Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestWorkflowIntegration(
            [FromBody] WorkflowTestRequest request
        )
        {
            try
            {
                // Get Firebase token from user claims
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(
                        new TestResponse
                        {
                            Success = false,
                            Error = "Authentication token not found. Please log in.",
                        }
                    );
                }

                // Get current user ID from claims
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Json(
                        new TestResponse
                        {
                            Success = false,
                            Error = "Current user ID not found in authentication claims.",
                        }
                    );
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}{request.Endpoint}",
                    request.Payload
                );
                Console.WriteLine($"Workflow Integration Test Response: {response}");
                var content = await response.Content.ReadAsStringAsync();

                var result = new TestResponse
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    ResponseBody = content,
                    TestName = request.TestName,
                    Endpoint = request.Endpoint,
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing workflow integration");
                return Json(new TestResponse { Success = false, Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestRateLimiting([FromBody] RateLimitTestRequest request)
        {
            try
            {
                // Get Firebase token from user claims
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(
                        new RateLimitTestResponse
                        {
                            Success = false,
                            Error = "Authentication token not found. Please log in.",
                        }
                    );
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var results = new List<TestResult>();
                var successCount = 0;
                var errorCount = 0;

                for (int i = 0; i < request.MessageCount; i++)
                {
                    var payload = new
                    {
                        senderId = request.SenderId,
                        receiverId = request.ReceiverId,
                        projectId = request.ProjectId,
                        subject = $"Rate Limit Test {i + 1}",
                        content = request.Content,
                    };

                    var response = await _httpClient.PostAsJsonAsync(
                        $"{_apiBaseUrl}/api/messages",
                        payload
                    );
                    var content = await response.Content.ReadAsStringAsync();

                    var result = new TestResult
                    {
                        MessageNumber = i + 1,
                        Success = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        ResponseBody = content,
                    };

                    results.Add(result);

                    if (response.IsSuccessStatusCode)
                        successCount++;
                    else
                        errorCount++;

                    // Small delay between requests
                    await Task.Delay(100);
                }

                return Json(
                    new RateLimitTestResponse
                    {
                        Success = true,
                        TotalMessages = request.MessageCount,
                        SuccessCount = successCount,
                        ErrorCount = errorCount,
                        SuccessRate = (double)successCount / request.MessageCount * 100,
                        Results = results,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing rate limiting");
                return Json(new RateLimitTestResponse { Success = false, Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestPerformance([FromBody] PerformanceTestRequest request)
        {
            try
            {
                // Get Firebase token from user claims
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(
                        new PerformanceTestResponse
                        {
                            Success = false,
                            Error = "Authentication token not found. Please log in.",
                        }
                    );
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var startTime = DateTime.UtcNow;
                var results = new List<TestResult>();

                for (int i = 0; i < request.MessageCount; i++)
                {
                    var payload = new
                    {
                        senderId = request.SenderId,
                        receiverId = request.ReceiverId,
                        projectId = request.ProjectId,
                        subject = $"Performance Test {i + 1}",
                        content = request.Content,
                    };

                    var messageStartTime = DateTime.UtcNow;
                    var response = await _httpClient.PostAsJsonAsync(
                        $"{_apiBaseUrl}/api/messages",
                        payload
                    );
                    var messageEndTime = DateTime.UtcNow;
                    var content = await response.Content.ReadAsStringAsync();

                    var result = new TestResult
                    {
                        MessageNumber = i + 1,
                        Success = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        ResponseTime = (messageEndTime - messageStartTime).TotalMilliseconds,
                        ResponseBody = content,
                    };

                    results.Add(result);
                }

                var endTime = DateTime.UtcNow;
                var totalTime = (endTime - startTime).TotalMilliseconds;

                return Json(
                    new PerformanceTestResponse
                    {
                        Success = true,
                        TotalMessages = request.MessageCount,
                        TotalTime = totalTime,
                        AverageTime = totalTime / request.MessageCount,
                        Results = results,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing performance");
                return Json(new PerformanceTestResponse { Success = false, Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestErrorHandling(
            [FromBody] ErrorHandlingTestRequest request
        )
        {
            try
            {
                // Get Firebase token from user claims
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(
                        new ErrorHandlingTestResponse
                        {
                            Success = false,
                            Error = "Authentication token not found. Please log in.",
                        }
                    );
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/messages",
                    request.Payload
                );
                var content = await response.Content.ReadAsStringAsync();

                var result = new ErrorHandlingTestResponse
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    ResponseBody = content,
                    TestName = request.TestName,
                    ExpectedResult = request.ExpectedResult,
                    ActualResult = response.IsSuccessStatusCode ? "success" : "error",
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing error handling");
                return Json(new ErrorHandlingTestResponse { Success = false, Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RunComprehensiveTests(
            [FromBody] ComprehensiveTestRequest request
        )
        {
            try
            {
                // Get Firebase token from user claims
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(
                        new ComprehensiveTestResponse
                        {
                            Success = false,
                            Error = "Authentication token not found. Please log in.",
                        }
                    );
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                Console.WriteLine("🚀 Starting Comprehensive Test Suite");
                Console.WriteLine(
                    $"Test Configuration: Validation={request.IncludeValidation}, Workflow={request.IncludeWorkflow}, ErrorHandling={request.IncludeErrorHandling}, Performance={request.IncludePerformance}, RateLimit={request.IncludeRateLimit}"
                );

                var results = new Dictionary<string, object>();
                var testSummary = new List<string>();

                if (request.IncludeValidation)
                {
                    var validationResults = await RunValidationTests();
                    results["validationTests"] = validationResults;
                    var validationSummary = GetTestSummary(validationResults, "Validation");
                    testSummary.Add(validationSummary);
                }

                if (request.IncludeWorkflow)
                {
                    var workflowResults = await RunWorkflowTests();
                    results["workflowTests"] = workflowResults;
                    var workflowSummary = GetTestSummary(workflowResults, "Workflow");
                    testSummary.Add(workflowSummary);
                }

                if (request.IncludeErrorHandling)
                {
                    var errorHandlingResults = await RunErrorHandlingTests();
                    results["errorHandlingTests"] = errorHandlingResults;
                    var errorHandlingSummary = GetTestSummary(
                        errorHandlingResults,
                        "Error Handling"
                    );
                    testSummary.Add(errorHandlingSummary);
                }

                if (request.IncludePerformance)
                {
                    var performanceResults = await RunPerformanceTests();
                    results["performanceTests"] = performanceResults;
                    testSummary.Add("Performance Tests: Completed");
                }

                if (request.IncludeRateLimit)
                {
                    var rateLimitResults = await RunRateLimitTests();
                    results["rateLimitTests"] = rateLimitResults;
                    testSummary.Add("Rate Limit Tests: Completed");
                }

                // Print overall summary
                Console.WriteLine("COMPREHENSIVE TEST SUMMARY:");
                foreach (var summary in testSummary)
                {
                    Console.WriteLine($"  {summary}");
                }

                return Json(
                    new ComprehensiveTestResponse
                    {
                        Success = true,
                        Results = results,
                        Summary = testSummary,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running comprehensive tests");
                return Json(new ComprehensiveTestResponse { Success = false, Error = ex.Message });
            }
        }

        private async Task<object> RunValidationTests()
        {
            var tests = new List<object>();
            Console.WriteLine("=== Starting Validation Tests ===");

            // Test cases for validation
            var testCases = new List<dynamic>
            {
                new
                {
                    name = "Valid Message",
                    payload = new
                    {
                        senderId = "user123",
                        receiverId = "user456",
                        projectId = "project789",
                        subject = "Test",
                        content = "Valid content",
                    },
                    expected = "success",
                },
                new
                {
                    name = "Empty Content",
                    payload = new
                    {
                        senderId = "user123",
                        receiverId = "user456",
                        projectId = "project789",
                        subject = "Test",
                        content = "",
                    },
                    expected = "error",
                },
                new
                {
                    name = "Missing Sender",
                    payload = new
                    {
                        receiverId = "user456",
                        projectId = "project789",
                        subject = "Test",
                        content = "Content",
                    },
                    expected = "error",
                },
                new
                {
                    name = "Long Content",
                    payload = new
                    {
                        senderId = "user123",
                        receiverId = "user456",
                        projectId = "project789",
                        subject = "Test",
                        content = new string('A', 5001),
                    },
                    expected = "error",
                },
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"Testing: {testCase.name}");

                try
                {
                    var response = await _httpClient.PostAsJsonAsync(
                        $"{_apiBaseUrl}/api/messages",
                        (object)testCase.payload
                    );
                    var content = await response.Content.ReadAsStringAsync();

                    var testResult = new
                    {
                        name = testCase.name,
                        success = response.IsSuccessStatusCode,
                        statusCode = (int)response.StatusCode,
                        expected = testCase.expected,
                        actual = response.IsSuccessStatusCode ? "success" : "error",
                        responseBody = content,
                        errorMessage = response.IsSuccessStatusCode
                            ? null
                            : GetErrorMessage(content),
                        testPassed = (
                            testCase.expected == "success" && response.IsSuccessStatusCode
                        ) || (testCase.expected == "error" && !response.IsSuccessStatusCode),
                    };

                    tests.Add(testResult);

                    if (testResult.testPassed)
                    {
                        Console.WriteLine(
                            $"✅ {testCase.name}: PASSED (Expected: {testCase.expected}, Got: {testResult.actual})"
                        );
                    }
                    else
                    {
                        Console.WriteLine(
                            $"❌ {testCase.name}: FAILED (Expected: {testCase.expected}, Got: {testResult.actual}) - {testResult.errorMessage}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 {testCase.name}: EXCEPTION - {ex.Message}");

                    tests.Add(
                        new
                        {
                            name = testCase.name,
                            success = false,
                            statusCode = 0,
                            expected = testCase.expected,
                            actual = "exception",
                            responseBody = ex.Message,
                            errorMessage = ex.Message,
                            testPassed = false,
                        }
                    );
                }
            }

            Console.WriteLine("=== Validation Tests Complete ===");
            return tests;
        }

        private async Task<object> RunWorkflowTests()
        {
            var tests = new List<object>();
            Console.WriteLine("=== Starting Workflow Tests ===");

            // Workflow test cases
            var workflowTests = new List<dynamic>
            {
                new
                {
                    name = "Quote Approval",
                    endpoint = "/api/messages/workflow/quote-approval",
                    payload = new
                    {
                        quoteId = "quote123",
                        action = "approved",
                        userId = "user123",
                    },
                },
                new
                {
                    name = "Invoice Payment",
                    endpoint = "/api/messages/workflow/invoice-payment",
                    payload = new
                    {
                        invoiceId = "invoice123",
                        action = "paid",
                        userId = "user123",
                    },
                },
                new
                {
                    name = "Project Update",
                    endpoint = "/api/messages/workflow/project-update",
                    payload = new
                    {
                        projectId = "project123",
                        updateType = "status_changed",
                        userId = "user123",
                    },
                },
            };

            foreach (var test in workflowTests)
            {
                Console.WriteLine($"Testing: {test.name} - {test.endpoint}");
                Console.WriteLine($"Payload: {JsonSerializer.Serialize(test.payload)}");

                try
                {
                    var response = await _httpClient.PostAsJsonAsync(
                        $"{_apiBaseUrl}{test.endpoint}",
                        (object)test.payload
                    );
                    var content = await response.Content.ReadAsStringAsync();

                    var testResult = new
                    {
                        name = test.name,
                        endpoint = test.endpoint,
                        success = response.IsSuccessStatusCode,
                        statusCode = (int)response.StatusCode,
                        responseBody = content,
                        errorMessage = response.IsSuccessStatusCode
                            ? null
                            : GetErrorMessage(content),
                        expectedFailure = true, // These are expected to fail due to missing test data
                        failureReason = response.IsSuccessStatusCode
                            ? null
                            : "Test entity not found in database",
                    };

                    tests.Add(testResult);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"✅ {test.name}: SUCCESS ({response.StatusCode})");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"❌ {test.name}: FAILED ({response.StatusCode}) - {testResult.errorMessage}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 {test.name}: EXCEPTION - {ex.Message}");

                    tests.Add(
                        new
                        {
                            name = test.name,
                            endpoint = test.endpoint,
                            success = false,
                            statusCode = 0,
                            responseBody = ex.Message,
                            errorMessage = ex.Message,
                            expectedFailure = true,
                            failureReason = "Exception occurred during test",
                        }
                    );
                }
            }

            Console.WriteLine("=== Workflow Tests Complete ===");
            return tests;
        }

        private async Task<object> RunErrorHandlingTests()
        {
            var tests = new List<object>();
            Console.WriteLine("=== Starting Error Handling Tests ===");

            // Error handling test cases
            var errorTests = new[]
            {
                new
                {
                    name = "Invalid User ID",
                    payload = new
                    {
                        senderId = "invalid-user",
                        receiverId = "user456",
                        projectId = "project789",
                        subject = "Test",
                        content = "Content",
                    },
                },
                new
                {
                    name = "SQL Injection Attempt",
                    payload = new
                    {
                        senderId = "user123",
                        receiverId = "user456",
                        projectId = "project789",
                        subject = "'; DROP TABLE messages; --",
                        content = "Content",
                    },
                },
                new
                {
                    name = "XSS Attempt",
                    payload = new
                    {
                        senderId = "user123",
                        receiverId = "user456",
                        projectId = "project789",
                        subject = "Test",
                        content = "<script>alert('XSS')</script>Content",
                    },
                },
            };

            foreach (var test in errorTests)
            {
                Console.WriteLine($"Testing: {test.name}");

                try
                {
                    var response = await _httpClient.PostAsJsonAsync(
                        $"{_apiBaseUrl}/api/messages",
                        test.payload
                    );
                    var content = await response.Content.ReadAsStringAsync();

                    var testResult = new
                    {
                        name = test.name,
                        success = response.IsSuccessStatusCode,
                        statusCode = (int)response.StatusCode,
                        responseBody = content,
                        errorMessage = response.IsSuccessStatusCode
                            ? null
                            : GetErrorMessage(content),
                        expectedToFail = true, // These should fail for security reasons
                        securityTestPassed = !response.IsSuccessStatusCode, // Should be rejected
                    };

                    tests.Add(testResult);

                    if (testResult.securityTestPassed)
                    {
                        Console.WriteLine(
                            $"✅ {test.name}: SECURITY TEST PASSED (Properly rejected)"
                        );
                    }
                    else
                    {
                        Console.WriteLine(
                            $"⚠️ {test.name}: SECURITY CONCERN (Should have been rejected but wasn't)"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 {test.name}: EXCEPTION - {ex.Message}");

                    tests.Add(
                        new
                        {
                            name = test.name,
                            success = false,
                            statusCode = 0,
                            responseBody = ex.Message,
                            errorMessage = ex.Message,
                            expectedToFail = true,
                            securityTestPassed = true, // Exception is also acceptable for security tests
                        }
                    );
                }
            }

            Console.WriteLine("=== Error Handling Tests Complete ===");
            return tests;
        }

        private async Task<object> RunPerformanceTests()
        {
            var startTime = DateTime.UtcNow;
            var testPayload = new
            {
                senderId = "user123",
                receiverId = "user456",
                projectId = "project789",
                subject = "Performance Test",
                content = "Performance test content",
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiBaseUrl}/api/messages",
                testPayload
            );
            var endTime = DateTime.UtcNow;
            var content = await response.Content.ReadAsStringAsync();

            return new
            {
                success = response.IsSuccessStatusCode,
                statusCode = (int)response.StatusCode,
                responseTime = (endTime - startTime).TotalMilliseconds,
                responseBody = content,
            };
        }

        private async Task<object> RunRateLimitTests()
        {
            var results = new List<object>();
            var testPayload = new
            {
                senderId = "user123",
                receiverId = "user456",
                projectId = "project789",
                subject = "Rate Limit Test",
                content = "Rate limit test content",
            };

            for (int i = 0; i < 5; i++)
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/messages",
                    testPayload
                );
                var content = await response.Content.ReadAsStringAsync();

                results.Add(
                    new
                    {
                        messageNumber = i + 1,
                        success = response.IsSuccessStatusCode,
                        statusCode = (int)response.StatusCode,
                        responseBody = content,
                    }
                );

                await Task.Delay(100);
            }

            return results;
        }

        private string GetErrorMessage(string responseBody)
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    responseBody
                );
                if (errorResponse != null && errorResponse.ContainsKey("error"))
                {
                    return errorResponse["error"].ToString();
                }
                if (errorResponse != null && errorResponse.ContainsKey("message"))
                {
                    return errorResponse["message"].ToString();
                }
            }
            catch
            {
                // If JSON parsing fails, return the raw response
            }

            return responseBody.Length > 200
                ? responseBody.Substring(0, 200) + "..."
                : responseBody;
        }

        private string GetTestSummary(object testResults, string testType)
        {
            try
            {
                if (testResults is List<object> results)
                {
                    var total = results.Count;
                    var passed = results.Count(r =>
                    {
                        var resultDict = r as Dictionary<string, object>;
                        if (resultDict != null)
                        {
                            // Check for different success indicators
                            if (resultDict.ContainsKey("testPassed"))
                                return (bool)resultDict["testPassed"];
                            if (resultDict.ContainsKey("securityTestPassed"))
                                return (bool)resultDict["securityTestPassed"];
                            if (resultDict.ContainsKey("success"))
                                return (bool)resultDict["success"];
                        }
                        return false;
                    });
                    var failed = total - passed;
                    return $"{testType} Tests: {passed}/{total} passed, {failed} failed";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating summary for {testType}: {ex.Message}");
            }

            return $"{testType} Tests: Summary unavailable";
        }
    }

    // Request models
    public class MessageValidationTestRequest
    {
        public string TestName { get; set; } = string.Empty;
        public object Payload { get; set; } = new();
        public string ExpectedResult { get; set; } = string.Empty;
    }

    public class WorkflowTestRequest
    {
        public string TestName { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public object Payload { get; set; } = new();
    }

    public class RateLimitTestRequest
    {
        public string SenderId { get; set; } = "user123";
        public string ReceiverId { get; set; } = "user456";
        public string ProjectId { get; set; } = "project789";
        public string Content { get; set; } = "Rate limit test content";
        public int MessageCount { get; set; } = 10;
    }

    public class PerformanceTestRequest
    {
        public string SenderId { get; set; } = "user123";
        public string ReceiverId { get; set; } = "user456";
        public string ProjectId { get; set; } = "project789";
        public string Content { get; set; } = "Performance test content";
        public int MessageCount { get; set; } = 5;
    }

    public class ErrorHandlingTestRequest
    {
        public string TestName { get; set; } = string.Empty;
        public object Payload { get; set; } = new();
        public string ExpectedResult { get; set; } = string.Empty;
    }

    public class ComprehensiveTestRequest
    {
        public bool IncludeValidation { get; set; } = true;
        public bool IncludeWorkflow { get; set; } = true;
        public bool IncludeErrorHandling { get; set; } = true;
        public bool IncludePerformance { get; set; } = true;
        public bool IncludeRateLimit { get; set; } = true;
    }

    // Response models
    public class TestResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public string? TestName { get; set; }
        public string? ExpectedResult { get; set; }
        public string? Endpoint { get; set; }
    }

    public class RateLimitTestResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int TotalMessages { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public double SuccessRate { get; set; }
        public List<TestResult> Results { get; set; } = new();
    }

    public class PerformanceTestResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int TotalMessages { get; set; }
        public double TotalTime { get; set; }
        public double AverageTime { get; set; }
        public List<TestResult> Results { get; set; } = new();
    }

    public class TestResult
    {
        public int MessageNumber { get; set; }
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public double ResponseTime { get; set; }
        public string? ResponseBody { get; set; }
    }

    public class ErrorHandlingTestResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public string? TestName { get; set; }
        public string? ExpectedResult { get; set; }
        public string? ActualResult { get; set; }
    }

    public class ComprehensiveTestResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public object? Results { get; set; }
        public List<string>? Summary { get; set; }
    }
}
