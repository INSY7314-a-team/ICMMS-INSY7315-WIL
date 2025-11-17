const ParseBlueprintFlow = require("../flows/ParseBlueprintFlow");

async function run() {
  console.log("🧪 test-dimensions-unavailable");

  // Use a minimalist PDF data URL unlikely to contain measurable blueprint content
  const minimalPdf =
    "data:application/pdf;base64,JVBERi0xLjQKJcKlwrHDqwoKMSAwIG9iago8PC9UeXBlIC9DYXRhbG9nL1BhZ2VzIDIgMCBSPj4KZW5kb2JqCjIgMCBvYmoKPDwvVHlwZSAvUGFnZXMvQ291bnQgMT4+CmVuZG9iagp4cmVmCjAgMwowMDAwMDAwMDAwIDY1NTM1IGYgCjAwMDAwMDAwMDEgMDAwMDAgbiAKdHJhaWxlcgo8PC9TaXplIDIvUm9vdCAxIDAgUj4+CnN0YXJ0eHJlZgoxNjYKJSVFT0YK";

  const input = {
    fileData: minimalPdf,
    fileType: "pdf",
    projectContext: { projectId: "TEST_DIM_001" },
  };

  try {
    const result = await ParseBlueprintFlow(input);
    if (result.success === false && result.errorCode === "DimensionsUnavailable") {
      console.log("✅ Stopped processing when dimensions were unavailable");
      process.exit(0);
    } else if (result.success === true) {
      console.error("❌ Expected DimensionsUnavailable, but processing succeeded");
      process.exit(1);
    } else {
      console.error("❌ Unexpected result:", result);
      process.exit(1);
    }
  } catch (err) {
    console.error("💥 Error executing flow:", err.message);
    process.exit(1);
  }
}

if (require.main === module) {
  run();
}


