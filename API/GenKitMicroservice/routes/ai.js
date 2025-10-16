const express = require("express");
const {
  processText,
  processImage,
  generateResponse,
} = require("../services/genkitService");
const { validateRequest } = require("../middleware/validation");
const { googleAI } = require("@genkit-ai/googleai");

const router = express.Router();

// Text processing endpoint
router.post("/text", validateRequest, async (req, res) => {
  try {
    const { prompt, model = googleAI.model("gemini-2.0-flash") } = req.body;

    if (!prompt) {
      return res.status(400).json({
        success: false,
        error: "Prompt is required",
      });
    }

    const result = await processText(prompt, model);

    res.json({
      success: true,
      data: {
        response: result,
        model: model,
        timestamp: new Date().toISOString(),
      },
    });
  } catch (error) {
    console.error("Text processing error:", error);
    res.status(500).json({
      success: false,
      error: "Failed to process text",
      message: error.message,
    });
  }
});

// Image processing endpoint
router.post("/image", validateRequest, async (req, res) => {
  try {
    const {
      prompt,
      imageData,
      model = googleAI.model("gemini-2.0-flash"),
    } = req.body;

    if (!prompt || !imageData) {
      return res.status(400).json({
        success: false,
        error: "Prompt and image data are required",
      });
    }

    const result = await processImage(prompt, imageData, model);

    res.json({
      success: true,
      data: {
        response: result,
        model: model,
        timestamp: new Date().toISOString(),
      },
    });
  } catch (error) {
    console.error("Image processing error:", error);
    res.status(500).json({
      success: false,
      error: "Failed to process image",
      message: error.message,
    });
  }
});

// Generate structured response
router.post("/generate", validateRequest, async (req, res) => {
  try {
    const {
      prompt,
      schema,
      model = googleAI.model("gemini-2.0-flash"),
    } = req.body;

    if (!prompt) {
      return res.status(400).json({
        success: false,
        error: "Prompt is required",
      });
    }

    const result = await generateResponse(prompt, schema, model);

    res.json({
      success: true,
      data: {
        response: result,
        model: model,
        timestamp: new Date().toISOString(),
      },
    });
  } catch (error) {
    console.error("Generate response error:", error);
    res.status(500).json({
      success: false,
      error: "Failed to generate response",
      message: error.message,
    });
  }
});

// Blueprint processing endpoint with PM review workflow
router.post("/process-blueprint", validateRequest, async (req, res) => {
  try {
    const { fileData, fileType, projectContext } = req.body;

    if (!fileData || !fileType) {
      return res.status(400).json({
        success: false,
        error: "File data and file type are required",
      });
    }

    // Use the enhanced ParseBlueprintFlow
    const ParseBlueprintFlow = require("../flows/ParseBlueprintFlow");
    const result = await ParseBlueprintFlow({
      fileData,
      fileType,
      projectContext,
    });

    res.json({
      success: true,
      data: {
        result: result,
        timestamp: new Date().toISOString(),
        processingVersion: "enhanced-v1.0",
      },
    });
  } catch (error) {
    console.error("Blueprint processing error:", error);
    res.status(500).json({
      success: false,
      error: "Failed to process blueprint",
      message: error.message,
    });
  }
});

// Blueprint processing endpoint for .NET API integration
router.post("/extract-line-items", validateRequest, async (req, res) => {
  try {
    const { fileData, fileType, projectContext } = req.body;

    if (!fileData || !fileType) {
      return res.status(400).json({
        success: false,
        error: "File data and file type are required",
      });
    }

    // Use the enhanced ParseBlueprintFlow
    const ParseBlueprintFlow = require("../flows/ParseBlueprintFlow");
    const result = await ParseBlueprintFlow({
      fileData,
      fileType,
      projectContext,
    });

    // Convert to .NET API format
    const lineItems = result.lineItems.map(item => ({
      itemId: item.itemId || `LI_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      name: item.name,
      description: item.description || "",
      quantity: item.quantity || 0,
      unit: item.unitOfQuantity || "N/A",
      category: item.category || "General",
      unitPrice: item.unitPrice || 0,
      lineTotal: item.lineTotal || 0,
      isAiGenerated: item.isAiGenerated || true,
      aiConfidence: item.aiConfidence || 0.8,
      notes: item.notes || "",
      materialDatabaseId: item.materialDatabaseId || null
    }));

    res.json({
      success: true,
      lineItems: lineItems,
      metadata: {
        totalItems: lineItems.length,
        processingTime: result.metadata?.processingTime || 0,
        confidence: result.metadata?.confidence || 0.8,
        blueprintTypes: result.metadata?.blueprintTypes || [],
        timestamp: new Date().toISOString(),
      }
    });
  } catch (error) {
    console.error("Blueprint line item extraction error:", error);
    res.status(500).json({
      success: false,
      error: "Failed to extract line items from blueprint",
      message: error.message,
    });
  }
});

// Note: PM Review endpoints removed - this should be handled by the .NET API
// The GenKitMicroservice should only handle AI processing, not business logic

module.exports = router;
