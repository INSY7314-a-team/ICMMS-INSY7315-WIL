const express = require("express");
const {
  processText,
  processImage,
  generateResponse,
} = require("../services/genkitService");
const { validateRequest } = require("../middleware/validation");

const router = express.Router();

// Text processing endpoint
router.post("/text", validateRequest, async (req, res) => {
  try {
    const { prompt, model = "gemini-pro" } = req.body;

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
    const { prompt, imageData, model = "gemini-pro-vision" } = req.body;

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
    const { prompt, schema, model = "gemini-pro" } = req.body;

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

// PM Review and Line Item Adjustment endpoint
router.post("/review-line-items", validateRequest, async (req, res) => {
  try {
    const { lineItems, projectId, reviewAction, adjustments = [] } = req.body;

    if (!lineItems || !projectId || !reviewAction) {
      return res.status(400).json({
        success: false,
        error: "Line items, project ID, and review action are required",
      });
    }

    let reviewedItems = [...lineItems];

    switch (reviewAction) {
      case "approve":
        // Mark all items as approved
        reviewedItems = reviewedItems.map((item) => ({
          ...item,
          pmReviewed: true,
          pmApproved: true,
          reviewDate: new Date().toISOString(),
          reviewStatus: "approved",
        }));
        break;

      case "adjust":
        // Apply adjustments to specific line items
        if (adjustments.length > 0) {
          reviewedItems = applyAdjustments(reviewedItems, adjustments);
        }
        break;

      case "add":
        // Add new line items
        if (adjustments.length > 0) {
          const newItems = adjustments.map((adj) => ({
            itemId: `PM_${Date.now()}_${Math.random()
              .toString(36)
              .substr(2, 9)}`,
            name: adj.name,
            description: adj.description,
            quantity: parseFloat(adj.quantity) || 1,
            unit: adj.unit || "ea",
            category: adj.category || "General",
            unitPrice: parseFloat(adj.unitPrice) || 0,
            lineTotal:
              (parseFloat(adj.quantity) || 1) *
              (parseFloat(adj.unitPrice) || 0),
            isAiGenerated: false,
            pmReviewed: true,
            pmApproved: true,
            reviewDate: new Date().toISOString(),
            reviewStatus: "added_by_pm",
            notes: adj.notes || "Added by Project Manager",
          }));
          reviewedItems.push(...newItems);
        }
        break;

      case "remove":
        // Remove specified line items
        const itemIdsToRemove = adjustments.map((adj) => adj.itemId);
        reviewedItems = reviewedItems.filter(
          (item) => !itemIdsToRemove.includes(item.itemId)
        );
        break;

      default:
        return res.status(400).json({
          success: false,
          error:
            "Invalid review action. Must be approve, adjust, add, or remove",
        });
    }

    // Recalculate totals after adjustments
    const updatedResult = {
      success: true,
      lineItems: reviewedItems,
      metadata: {
        originalItemCount: lineItems.length,
        finalItemCount: reviewedItems.length,
        adjustmentsMade: adjustments.length,
        reviewDate: new Date().toISOString(),
        reviewedBy: "Project Manager", // In real implementation, get from auth token
        projectId: projectId,
      },
      summary: {
        totalItems: reviewedItems.length,
        totalValue: reviewedItems.reduce(
          (sum, item) => sum + (item.lineTotal || 0),
          0
        ),
        categories: [...new Set(reviewedItems.map((item) => item.category))],
        aiGeneratedItems: reviewedItems.filter((item) => item.isAiGenerated)
          .length,
        pmAddedItems: reviewedItems.filter(
          (item) => item.reviewStatus === "added_by_pm"
        ).length,
        adjustedItems: reviewedItems.filter(
          (item) => item.reviewStatus === "adjusted_by_pm"
        ).length,
      },
    };

    res.json({
      success: true,
      data: {
        result: updatedResult,
        timestamp: new Date().toISOString(),
      },
    });
  } catch (error) {
    console.error("PM review error:", error);
    res.status(500).json({
      success: false,
      error: "Failed to process PM review",
      message: error.message,
    });
  }
});

// Helper function to apply adjustments to line items
function applyAdjustments(lineItems, adjustments) {
  return lineItems.map((item) => {
    const adjustment = adjustments.find((adj) => adj.itemId === item.itemId);

    if (adjustment) {
      const updatedItem = {
        ...item,
        name: adjustment.name !== undefined ? adjustment.name : item.name,
        description:
          adjustment.description !== undefined
            ? adjustment.description
            : item.description,
        quantity:
          adjustment.quantity !== undefined
            ? parseFloat(adjustment.quantity)
            : item.quantity,
        unit: adjustment.unit !== undefined ? adjustment.unit : item.unit,
        category:
          adjustment.category !== undefined
            ? adjustment.category
            : item.category,
        unitPrice:
          adjustment.unitPrice !== undefined
            ? parseFloat(adjustment.unitPrice)
            : item.unitPrice,
        lineTotal: 0, // Will be recalculated
        pmReviewed: true,
        pmApproved: true,
        reviewDate: new Date().toISOString(),
        reviewStatus: "adjusted_by_pm",
        notes: adjustment.notes !== undefined ? adjustment.notes : item.notes,
        originalValues: {
          name: item.name,
          description: item.description,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          lineTotal: item.lineTotal,
        },
      };

      // Recalculate line total
      updatedItem.lineTotal = updatedItem.quantity * updatedItem.unitPrice;

      return updatedItem;
    }

    return item;
  });
}

module.exports = router;
