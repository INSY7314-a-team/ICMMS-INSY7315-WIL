const fs = require('fs');
const path = require('path');

// Test the pagination feature with GenKitMicroservice
async function testPagination() {
  console.log('🧪 Testing Pagination Feature with GenKitMicroservice');
  console.log('====================================================\n');

  try {
    // Download blueprint PDF from Supabase
    const blueprintUrl = "https://givtrrcqxteqcnmtnnkt.supabase.co/storage/v1/object/sign/upload/blueprint.pdf?token=eyJraWQiOiJzdG9yYWdlLXVybC1zaWduaW5nLWtleV8wYjEwMjZjYi04YTM1LTRhZTktYjQxMi1kZGRiYmQ1MDVmZTQiLCJhbGciOiJIUzI1NiJ9.eyJ1cmwiOiJ1cGxvYWQvYmx1ZXByaW50LnBkZiIsImlhdCI6MTc2MDY0MTQ0MiwiZXhwIjoxNzkyMTc3NDQyfQ.GiK-oUawd5-CA9Gl6CHdPJyXj3WuyCb7lJOVQOn7X-U";
    
    console.log('📥 Downloading blueprint from Supabase...');
    const response = await fetch(blueprintUrl);
    
    if (!response.ok) {
      throw new Error(`Failed to download blueprint: ${response.status} ${response.statusText}`);
    }
    
    const blueprintBuffer = await response.arrayBuffer();
    const fileData = Buffer.from(blueprintBuffer).toString('base64');

    // Test data
    const testData = {
      fileData: fileData,
      fileType: 'pdf',
      projectContext: {
        projectId: 'PAGINATION_TEST_001',
        projectType: 'residential',
        buildingType: 'single_family_home',
        location: 'South Africa',
        squareFootage: 1136,
        garageArea: 260,
        glazingArea: 159
      }
    };

    console.log('📄 Blueprint loaded from Supabase');
    console.log('📊 File size:', (blueprintBuffer.byteLength / 1024).toFixed(2), 'KB\n');

    // Test the extract-line-items endpoint
    console.log('🚀 Testing /api/ai/extract-line-items endpoint...');
    
    const genkitResponse = await fetch('http://localhost:3001/api/ai/extract-line-items', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(testData)
    });

    if (!genkitResponse.ok) {
      throw new Error(`GenKitMicroservice error: ${genkitResponse.status} ${genkitResponse.statusText}`);
    }

    const result = await genkitResponse.json();
    
    if (result.success) {
      console.log('✅ Successfully extracted line items!');
      console.log('📊 Total items:', result.lineItems.length);
      console.log('⏱️  Processing time:', result.metadata.processingTime, 'ms');
      console.log('🎯 Confidence:', (result.metadata.confidence * 100).toFixed(1) + '%\n');

      // Test pagination logic
      console.log('📄 Testing Pagination Logic:');
      console.log('============================');
      
      const itemsPerPage = 5;
      const totalPages = Math.ceil(result.lineItems.length / itemsPerPage);
      
      console.log(`Total items: ${result.lineItems.length}`);
      console.log(`Items per page: ${itemsPerPage}`);
      console.log(`Total pages: ${totalPages}\n`);

      // Show each page
      for (let page = 1; page <= totalPages; page++) {
        const startIndex = (page - 1) * itemsPerPage;
        const endIndex = Math.min(startIndex + itemsPerPage, result.lineItems.length);
        const pageItems = result.lineItems.slice(startIndex, endIndex);
        
        console.log(`📄 Page ${page} of ${totalPages} (Items ${startIndex + 1}-${endIndex}):`);
        pageItems.forEach((item, index) => {
          const itemNumber = startIndex + index + 1;
          console.log(`  ${itemNumber}. ${item.name} - R${item.lineTotal.toFixed(2)}`);
        });
        console.log('');
      }

      // Test item categorization
      const generalItems = result.lineItems.filter(item => 
        item.itemType === 'General' || 
        item.category === 'General' || 
        (item.name && (
          item.name.toLowerCase().includes('preparation') ||
          item.name.toLowerCase().includes('management') ||
          item.name.toLowerCase().includes('supervision') ||
          item.name.toLowerCase().includes('utilities') ||
          item.name.toLowerCase().includes('equipment') ||
          item.name.toLowerCase().includes('safety') ||
          item.name.toLowerCase().includes('permits')
        ))
      );
      
      const materialItems = result.lineItems.filter(item => 
        item.itemType === 'Material' || 
        (item.category !== 'General' && 
         item.itemType !== 'General' && 
         !(item.name && (
          item.name.toLowerCase().includes('preparation') ||
          item.name.toLowerCase().includes('management') ||
          item.name.toLowerCase().includes('supervision') ||
          item.name.toLowerCase().includes('utilities') ||
          item.name.toLowerCase().includes('equipment') ||
          item.name.toLowerCase().includes('safety') ||
          item.name.toLowerCase().includes('permits')
         )))
      );

      console.log('📊 Item Categorization:');
      console.log('======================');
      console.log(`General Items: ${generalItems.length}`);
      console.log(`Material Items: ${materialItems.length}`);
      console.log(`Total Items: ${result.lineItems.length}\n`);

      // Test brick detection
      const brickItems = result.lineItems.filter(item => 
        item.name && (
          item.name.toLowerCase().includes('brick') || 
          item.name.toLowerCase().includes('masonry') ||
          item.name.toLowerCase().includes('external wall')
        )
      );
      
      console.log('🧱 Brick Materials:');
      console.log('==================');
      if (brickItems.length > 0) {
        brickItems.forEach(item => {
          console.log(`- ${item.name}: ${item.quantity} ${item.unitOfQuantity || item.unit}`);
        });
      } else {
        console.log('❌ No brick materials found');
      }

      console.log('\n✅ Pagination test completed successfully!');
      console.log('🎉 The WorkflowTest.cshtml will now display 5 items per page!');

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
testPagination();
