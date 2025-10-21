# Simple Blueprint Parsing Test

This directory contains a simple test to verify that the AI model can parse the PDF blueprint located in the GenKitMicroservice directory.

## Files

- `tests/simpleBlueprintParsing.test.js` - The main test file that tests PDF blueprint parsing
- `run-simple-test.js` - Simple script to run the test
- `blueprint.pdf` - The actual PDF blueprint file to be parsed

## How to Run the Test

### Option 1: Using the simple runner script

```bash
cd API/GenKitMicroservice
node run-simple-test.js
```

### Option 2: Running the test directly

```bash
cd API/GenKitMicroservice
node tests/simpleBlueprintParsing.test.js
```

## What the Test Does

1. **File Validation**: Checks if the `blueprint.pdf` file exists in the directory
2. **PDF Processing**: Converts the PDF to base64 format and processes it through the ParseBlueprintFlow
3. **Result Validation**: Validates that the AI model can:
   - Extract line items from the blueprint
   - Identify construction categories
   - Generate reasonable confidence scores
   - Complete processing within acceptable time limits

## Expected Results

The test expects:

- At least 3 line items to be extracted
- Confidence score above 0.3
- Coverage percentage above 20%
- Processing time under 30 seconds
- Categories like "Site Preparation" and "Construction" to be identified

## Test Output

The test will display:

- ✅ **PASSED** if all validations succeed
- ❌ **FAILED** if any validation fails
- Detailed results including line items, confidence scores, and processing time

## Troubleshooting

If the test fails:

1. Ensure the `blueprint.pdf` file exists in the GenKitMicroservice directory
2. Check that all required environment variables are set (API keys, etc.)
3. Verify that the ParseBlueprintFlow is working correctly
4. Check the console output for specific error messages

## Integration

This test can be integrated into CI/CD pipelines by checking the exit code:

- Exit code 0: Test passed
- Exit code 1: Test failed
