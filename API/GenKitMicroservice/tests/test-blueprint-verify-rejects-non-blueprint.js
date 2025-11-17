const ParseBlueprintFlow = require("../flows/ParseBlueprintFlow");

async function run() {
  console.log("🧪 test-blueprint-verify-rejects-non-blueprint");

  // A tiny PNG pixel; clearly not a blueprint
  const nonBlueprintImage =
    "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHxAL9QZ6g3QAAAABJRU5ErkJggg==";

  const input = {
    fileData: nonBlueprintImage,
    fileType: "image",
    projectContext: { projectId: "TEST_NON_BP_001" },
  };

  try {
    const result = await ParseBlueprintFlow(input);
    if (result.success === false && result.errorCode === "NotBlueprint") {
      console.log("✅ Rejected non-blueprint with 422 semantics (NotBlueprint)");
      process.exit(0);
    } else {
      console.error("❌ Expected NotBlueprint rejection, got:", result);
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


