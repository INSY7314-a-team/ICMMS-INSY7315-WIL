const fs = require('fs');
const path = require('path');

// Test the integration between GenKitMicroservice and .NET API
async function testIntegration() {
  console.log('🧪 Testing GenKitMicroservice Integration with .NET API');
  console.log('=====================================================\n');

  try {
    // Download blueprint PDF from Supabase
    const blueprintUrl = "https://givtrrcqxteqcnmtnnkt.supabase.co/storage/v1/object/sign/upload/blueprint.pdf?token=eyJraWQiOiJzdG9yYWdlLXVybC1zaWduaW5nLWtleV8wYjEwMjZjYi04YTM1LTRhZTktYjQxMi1kZGRiYmQ1MDVmZTQiLCJhbGciOiJIUzI1NiJ9.eyJ1cmwiOiJ1cGxvYWQvYmx1ZXByaW50LnBkZiIsImlhdCI6MTc2MDY0MTQ0MiwiZXhwIjoxNzkyMTc3NDQyfQ.GiK-oUawd5-CA9Gl6CHdPJyXj3WuyCb7lJOVQOn7X-U";
    
    console.log('📥 Downloading blueprint from Supabase...');
    const response2 = await fetch(blueprintUrl);
    
    if (!response2.ok) {
      throw new Error(`Failed to download blueprint: ${response2.status} ${response2.statusText}`);
    }
    
    const blueprintBuffer = await response2.arrayBuffer();
    const fileData = Buffer.from(blueprintBuffer).toString('base64');

    // Test data
    const testData = {
      fileData: fileData,
      fileType: 'pdf',
      projectContext: {
        projectId: 'INTEGRATION_TEST_001',
        projectType: 'residential',
        buildingType: 'single_family_home',
        location: 'South Africa',
        squareFootage: 2500
      }
    };

    console.log('📄 Blueprint loaded from Supabase');
    console.log('📊 File size:', (blueprintBuffer.byteLength / 1024).toFixed(2), 'KB');
    console.log('🔧 Base64 length:', fileData.length, 'characters\n');

    // Test the new extract-line-items endpoint
    console.log('🚀 Testing /api/ai/extract-line-items endpoint...');
    
    const response = await fetch('http://localhost:3001/api/ai/extract-line-items', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(testData)
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const result = await response.json();
    
    if (result.success) {
      console.log('✅ Successfully extracted line items!');
      console.log('📊 Total items:', result.lineItems.length);
      console.log('⏱️  Processing time:', result.metadata.processingTime, 'ms');
      console.log('🎯 Confidence:', (result.metadata.confidence * 100).toFixed(1) + '%');
      console.log('🏗️  Blueprint types:', result.metadata.blueprintTypes.join(', '));
      console.log(result);
     } else {
      console.log('❌ Failed to extract line items');
      console.log('Error:', result.error);
    }

  } catch (error) {
    console.error('❌ Test failed:', error.message);
    
    if (error.code === 'ECONNREFUSED') {
      console.log('\n💡 Make sure the GenKitMicroservice is running:');
      console.log('   cd API/GenKitMicroservice');
      console.log('   npm start');
    }
  }
}

// Run the test
testIntegration();
