const pdfParser = require("pdf-parse");
const mammoth = require("mammoth");
const { googleAI } = require("@genkit-ai/googleai");
const { genkit } = require("genkit");

const ai = genkit({
  plugins: [googleAI()],
});

// Enhanced Blueprint Processing Flow with Agent Orchestration
const ParseBlueprintFlow = ai.defineFlow(
  "EnhancedParseBlueprintFlow",
  async (input) => {
    const { fileData, fileType, projectContext = {} } = input;

    try {
      console.log(
        `Processing blueprint: Type=${fileType}, Project=${
          projectContext.projectId || "Unknown"
        }`
      );

      // Phase 1: Enhanced Text Extraction
      const extractedContent = await extractTextContent(fileData, fileType);

      // Phase 2: Blueprint Analysis
      const analysis = await analyzeBlueprint(extractedContent, projectContext);

      // Phase 3: Line Item Extraction
      const lineItems = await extractLineItems(analysis, projectContext);

      // Phase 4: Holistic Coverage Enhancement
      const completeLineItems = await enhanceHolisticCoverage(
        lineItems,
        analysis,
        projectContext
      );

      // Phase 5: Validation and Confidence Scoring
      const validatedItems = await validateAndScore(
        completeLineItems,
        analysis
      );

      const result = {
        success: true,
        lineItems: validatedItems.items,
        metadata: {
          blueprintTypes: analysis.blueprintTypes,
          confidence: validatedItems.averageConfidence,
          coverage: validatedItems.coveragePercentage,
          processingTime: Date.now(),
          projectContext: projectContext,
        },
        summary: {
          totalItems: validatedItems.items.length,
          totalValue: validatedItems.items.reduce(
            (sum, item) => sum + (item.lineTotal || 0),
            0
          ),
          categories: [
            ...new Set(validatedItems.items.map((item) => item.category)),
          ],
          requiresPMReview: validatedItems.averageConfidence < 0.8,
        },
      };

      console.log(
        `Blueprint processing completed: ${result.lineItems.length} line items generated`
      );
      return result;
    } catch (error) {
      console.error("Blueprint processing error:", error);

      // Fallback to basic extraction
      return await fallbackProcessing(
        fileData,
        fileType,
        projectContext,
        error
      );
    }
  }
);

// Phase 1: Enhanced Text Extraction Agent
async function extractTextContent(fileData, fileType) {
  try {
    switch (fileType.toLowerCase()) {
      case "pdf":
        const pdfBuffer = Buffer.from(fileData, "base64");
        const pdfData = await pdfParser(pdfBuffer);
        return {
          text: pdfData.text,
          pages: pdfData.numpages,
          metadata: {
            title: pdfData.info?.Title || "Unknown",
            author: pdfData.info?.Author || "Unknown",
            creationDate: pdfData.info?.CreationDate || null,
          },
        };

      case "docx":
        const docxResult = await mammoth.extractRawText({
          buffer: Buffer.from(fileData, "base64"),
        });
        return {
          text: docxResult.value,
          metadata: {
            wordCount: docxResult.value.split(/\s+/).length,
            hasImages: docxResult.messages.some((msg) => msg.type === "image"),
          },
        };

      case "dwg":
      case "dxf":
        // For CAD files, we'd need additional libraries like node-dxf or similar
        // For now, return a placeholder that indicates CAD processing is needed
        return {
          text: "CAD file detected - manual processing may be required for accurate extraction",
          metadata: {
            fileType: fileType,
            requiresSpecialProcessing: true,
          },
        };

      default: // Images or other formats
        const visionResponse = await ai.generate({
          model: "gemini-pro-vision",
          prompt: `Extract all visible text, dimensions, specifications, and construction details from this blueprint image.
          Focus on:
          - All measurements and dimensions
          - Material specifications
          - Construction notes and annotations
          - Scale and legend information
          - Room names and areas
          - Structural elements
          - MEP system details

          Return the information in a structured format.`,
          image: fileData,
        });

        return {
          text: visionResponse.text,
          metadata: {
            extractedBy: "vision-ai",
            confidence: "medium", // Vision AI may miss some details
          },
        };
    }
  } catch (error) {
    console.error(`Text extraction error for ${fileType}:`, error);
    throw new Error(
      `Failed to extract text from ${fileType} file: ${error.message}`
    );
  }
}

// Phase 2: Blueprint Analysis Agent
async function analyzeBlueprint(extractedContent, projectContext) {
  try {
    const analysisPrompt = `
      Analyze this construction blueprint content and identify:

      1. BLUEPRINT TYPES (architectural, structural, MEP, civil, etc.)
      2. CONSTRUCTION SCOPE:
         - Building type (residential, commercial, industrial)
         - Square footage/meters
         - Number of stories/floors
         - Room types and quantities
      3. STRUCTURAL ELEMENTS:
         - Foundation types and sizes
         - Wall types and materials
         - Roof structure and materials
         - Floor systems
      4. MEP SYSTEMS:
         - Electrical systems and fixtures
         - Plumbing fixtures and systems
         - HVAC systems and equipment
         - Fire protection systems
      5. FINISHES:
         - Flooring types and areas
         - Wall finishes
         - Ceiling finishes
         - Interior doors and hardware
      6. SITE WORK:
         - Landscaping requirements
         - Parking areas
         - Utilities and connections

      CONTENT TO ANALYZE:
      ${extractedContent.text}

      PROJECT CONTEXT:
      ${JSON.stringify(projectContext, null, 2)}

      Return the analysis in JSON format with specific measurements and quantities where available.
    `;

    const analysisResponse = await ai.generate({
      model: "gemini-pro",
      prompt: analysisPrompt,
    });

    let analysis;
    try {
      analysis = JSON.parse(analysisResponse.text);
    } catch (parseError) {
      console.warn("Failed to parse AI analysis as JSON, using text response");
      analysis = { rawAnalysis: analysisResponse.text };
    }

    return {
      ...analysis,
      metadata: extractedContent.metadata,
      blueprintTypes:
        analysis.blueprintTypes || detectBlueprintTypes(extractedContent.text),
      rawContent: extractedContent.text,
    };
  } catch (error) {
    console.error("Blueprint analysis error:", error);
    throw new Error(`Blueprint analysis failed: ${error.message}`);
  }
}

// Phase 3: Line Item Extraction Agent
async function extractLineItems(analysis, projectContext) {
  try {
    const extractionPrompt = `
      Based on this blueprint analysis, generate detailed line items for construction estimation.
      Each line item should include:

      REQUIRED FIELDS:
      - name: Descriptive name of the work/item
      - description: Detailed description including specifications
      - quantity: Numerical quantity
      - unit: Unit of measure (ea, sq ft, ln ft, cy, etc.)
      - category: Construction category (Foundation, Framing, Electrical, etc.)
      - unitPrice: Estimated unit price (use reasonable market rates)
      - lineTotal: quantity × unitPrice
      - isAiGenerated: true
      - aiConfidence: Confidence level 0.0-1.0

      ANALYSIS DATA:
      ${JSON.stringify(analysis, null, 2)}

      Generate line items for ALL aspects including:
      - Site preparation and demolition
      - Foundation and structural work
      - Exterior and interior finishes
      - MEP systems (Mechanical, Electrical, Plumbing)
      - Specialties and equipment
      - Project overhead and contingencies

      Use realistic quantities based on the blueprint measurements and standard construction practices.
      Return as a JSON array of line items.
    `;

    const extractionResponse = await ai.generate({
      model: "gemini-pro",
      prompt: extractionPrompt,
    });

    let lineItems;
    try {
      lineItems = JSON.parse(extractionResponse.text);
      if (!Array.isArray(lineItems)) {
        throw new Error("Response is not an array");
      }
    } catch (parseError) {
      console.warn(
        "Failed to parse line items as JSON, attempting to extract from text"
      );
      lineItems = await parseLineItemsFromText(extractionResponse.text);
    }

    // Ensure all required fields are present
    lineItems = lineItems.map((item) => ({
      itemId: `LI_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      name: item.name || "Unnamed Item",
      description: item.description || "",
      quantity: parseFloat(item.quantity) || 1,
      unit: item.unit || "ea",
      category: item.category || "General",
      unitPrice: parseFloat(item.unitPrice) || 0,
      lineTotal:
        (parseFloat(item.quantity) || 1) * (parseFloat(item.unitPrice) || 0),
      isAiGenerated: true,
      aiConfidence: Math.min(
        1.0,
        Math.max(0.0, parseFloat(item.aiConfidence) || 0.7)
      ),
      materialDatabaseId: item.materialDatabaseId || null,
      notes:
        item.notes ||
        `Generated from blueprint analysis - ${
          analysis.blueprintTypes?.join(", ") || "general construction"
        }`,
    }));

    return lineItems;
  } catch (error) {
    console.error("Line item extraction error:", error);
    throw new Error(`Line item extraction failed: ${error.message}`);
  }
}

// Phase 4: Holistic Coverage Enhancement Agent
async function enhanceHolisticCoverage(lineItems, analysis, projectContext) {
  try {
    // Start with existing line items
    let enhancedItems = [...lineItems];

    // Add missing categories based on blueprint analysis
    const existingCategories = new Set(lineItems.map((item) => item.category));
    const requiredCategories = [
      "Demolition",
      "Site Preparation",
      "Foundation",
      "Structural",
      "Exterior",
      "Interior",
      "MEP",
      "Finishes",
      "Specialties",
      "Project Overhead",
      "Contingencies",
    ];

    // Add demolition items if not present and structural elements exist
    if (!existingCategories.has("Demolition") && analysis.structuralElements) {
      const demolitionItems = await generateDemolitionItems(
        analysis,
        projectContext
      );
      enhancedItems.push(...demolitionItems);
    }

    // Add site preparation if not present
    if (!existingCategories.has("Site Preparation")) {
      const sitePrepItems = await generateSitePreparationItems(
        analysis,
        projectContext
      );
      enhancedItems.push(...sitePrepItems);
    }

    // Add project overhead if not present
    if (!existingCategories.has("Project Overhead")) {
      const overheadItems = await generateProjectOverheadItems(
        analysis,
        projectContext
      );
      enhancedItems.push(...overheadItems);
    }

    // Add contingencies if not present
    if (!existingCategories.has("Contingencies")) {
      const contingencyItems = await generateContingencyItems(
        enhancedItems,
        projectContext
      );
      enhancedItems.push(...contingencyItems);
    }

    // Ensure all items have proper calculations
    enhancedItems = enhancedItems.map((item) => ({
      ...item,
      lineTotal: item.quantity * item.unitPrice,
      notes: item.notes || `Enhanced coverage item - ${item.category}`,
    }));

    return enhancedItems;
  } catch (error) {
    console.error("Holistic coverage enhancement error:", error);
    return lineItems; // Return original items if enhancement fails
  }
}

// Phase 5: Validation and Confidence Scoring Agent
async function validateAndScore(lineItems, analysis) {
  try {
    // Calculate confidence scores based on various factors
    const scoredItems = lineItems.map((item) => {
      let confidence = item.aiConfidence || 0.7;

      // Adjust confidence based on blueprint quality indicators
      if (analysis.metadata?.confidence === "low") {
        confidence *= 0.8;
      }

      // Adjust confidence based on category completeness
      const categoryItems = lineItems.filter(
        (li) => li.category === item.category
      );
      if (categoryItems.length === 1) {
        confidence *= 0.9; // Single items in category may be less reliable
      }

      // Adjust confidence based on quantity reasonableness
      if (item.quantity <= 0 || item.unitPrice <= 0) {
        confidence *= 0.5;
      }

      // Ensure confidence is within bounds
      confidence = Math.min(1.0, Math.max(0.0, confidence));

      return {
        ...item,
        aiConfidence: confidence,
      };
    });

    // Calculate overall statistics
    const averageConfidence =
      scoredItems.reduce((sum, item) => sum + item.aiConfidence, 0) /
      scoredItems.length;
    const totalValue = scoredItems.reduce(
      (sum, item) => sum + item.lineTotal,
      0
    );

    // Determine coverage percentage based on required categories
    const requiredCategories = ["Foundation", "Structural", "MEP", "Finishes"];
    const presentCategories = new Set(scoredItems.map((item) => item.category));
    const coveragePercentage =
      (presentCategories.size / requiredCategories.length) * 100;

    return {
      items: scoredItems,
      averageConfidence,
      coveragePercentage,
      totalValue,
      validationSummary: {
        totalItems: scoredItems.length,
        highConfidenceItems: scoredItems.filter(
          (item) => item.aiConfidence >= 0.8
        ).length,
        mediumConfidenceItems: scoredItems.filter(
          (item) => item.aiConfidence >= 0.5 && item.aiConfidence < 0.8
        ).length,
        lowConfidenceItems: scoredItems.filter(
          (item) => item.aiConfidence < 0.5
        ).length,
      },
    };
  } catch (error) {
    console.error("Validation and scoring error:", error);
    // Return items with default confidence if validation fails
    return {
      items: lineItems.map((item) => ({ ...item, aiConfidence: 0.7 })),
      averageConfidence: 0.7,
      coveragePercentage: 75,
      totalValue: lineItems.reduce((sum, item) => sum + item.lineTotal, 0),
    };
  }
}

// Helper Functions

function detectBlueprintTypes(text) {
  const types = [];
  const content = text.toLowerCase();

  if (
    content.includes("architectural") ||
    content.includes("floor plan") ||
    content.includes("elevation")
  ) {
    types.push("architectural");
  }
  if (
    content.includes("structural") ||
    content.includes("foundation") ||
    content.includes("beam") ||
    content.includes("column")
  ) {
    types.push("structural");
  }
  if (
    content.includes("electrical") ||
    content.includes("plumbing") ||
    content.includes("hvac") ||
    content.includes("mechanical")
  ) {
    types.push("MEP");
  }
  if (
    content.includes("site") ||
    content.includes("civil") ||
    content.includes("grading") ||
    content.includes("utility")
  ) {
    types.push("civil");
  }

  return types.length > 0 ? types : ["general"];
}

async function parseLineItemsFromText(textResponse) {
  // Fallback parser for when JSON parsing fails
  const lines = textResponse.split("\n");
  const items = [];

  for (const line of lines) {
    if (
      line.includes("-") &&
      (line.includes("ea") || line.includes("sq ft") || line.includes("ln ft"))
    ) {
      // Try to extract structured data from text line
      const parts = line.split("-");
      if (parts.length >= 2) {
        items.push({
          name: parts[0].trim(),
          description: parts[1].trim(),
          quantity: 1,
          unit: "ea",
          category: "General",
          unitPrice: 100, // Default price
          aiConfidence: 0.6,
        });
      }
    }
  }

  return items.length > 0
    ? items
    : [
        {
          name: "Blueprint Analysis Review Required",
          description: "AI parsing incomplete - manual review needed",
          quantity: 1,
          unit: "ea",
          category: "General",
          unitPrice: 0,
          aiConfidence: 0.3,
        },
      ];
}

async function generateDemolitionItems(analysis, projectContext) {
  const demoPrompt = `
    Based on this blueprint analysis, generate demolition line items for:
    ${JSON.stringify(analysis, null, 2)}

    Consider:
    - Existing structures to be removed
    - Hazardous material abatement (asbestos, lead)
    - Site clearing and preparation
    - Utility disconnections
    - Waste disposal and recycling

    Return as JSON array of line items.
  `;

  try {
    const demoResponse = await ai.generate({
      model: "gemini-pro",
      prompt: demoPrompt,
    });

    return JSON.parse(demoResponse.text) || [];
  } catch (error) {
    // Return basic demolition items as fallback
    return [
      {
        name: "Site Demolition and Clearing",
        description: "Demolition of existing structures and site clearing",
        quantity: 1,
        unit: "ls",
        category: "Demolition",
        unitPrice: 5000,
        aiConfidence: 0.6,
      },
      {
        name: "Waste Disposal and Recycling",
        description: "Removal and disposal of construction waste",
        quantity: 1,
        unit: "ls",
        category: "Demolition",
        unitPrice: 2000,
        aiConfidence: 0.6,
      },
    ];
  }
}

async function generateSitePreparationItems(analysis, projectContext) {
  return [
    {
      name: "Site Preparation and Earthwork",
      description: "Excavation, grading, and site preparation",
      quantity: 1,
      unit: "ls",
      category: "Site Preparation",
      unitPrice: 8000,
      aiConfidence: 0.7,
    },
    {
      name: "Temporary Utilities and Facilities",
      description: "Temporary power, water, and sanitation facilities",
      quantity: 1,
      unit: "ls",
      category: "Site Preparation",
      unitPrice: 3000,
      aiConfidence: 0.7,
    },
  ];
}

async function generateProjectOverheadItems(analysis, projectContext) {
  const totalValue = analysis.estimatedValue || 100000; // Default if not available
  const overheadPercentage = 0.15; // 15% overhead

  return [
    {
      name: "Project Management and Supervision",
      description: "Project management, supervision, and coordination",
      quantity: Math.ceil(totalValue * 0.08),
      unit: "ea",
      category: "Project Overhead",
      unitPrice: 1,
      aiConfidence: 0.8,
    },
    {
      name: "Permits and Inspections",
      description: "Building permits, inspections, and regulatory compliance",
      quantity: 1,
      unit: "ls",
      category: "Project Overhead",
      unitPrice: Math.ceil(totalValue * 0.03),
      aiConfidence: 0.8,
    },
    {
      name: "Temporary Facilities and Equipment",
      description:
        "Jobsite trailer, equipment rental, and temporary facilities",
      quantity: 1,
      unit: "ls",
      category: "Project Overhead",
      unitPrice: Math.ceil(totalValue * 0.02),
      aiConfidence: 0.8,
    },
    {
      name: "Safety and Security",
      description: "Jobsite safety equipment, security, and compliance",
      quantity: 1,
      unit: "ls",
      category: "Project Overhead",
      unitPrice: Math.ceil(totalValue * 0.02),
      aiConfidence: 0.8,
    },
  ];
}

async function generateContingencyItems(lineItems, projectContext) {
  const subtotal = lineItems.reduce((sum, item) => sum + item.lineTotal, 0);
  const contingencyPercentage = 0.1; // 10% contingency

  return [
    {
      name: "Owner Contingency",
      description: "Owner contingency for unforeseen conditions and changes",
      quantity: Math.ceil(subtotal * contingencyPercentage),
      unit: "ea",
      category: "Contingencies",
      unitPrice: 1,
      aiConfidence: 0.9,
    },
    {
      name: "Weather Delay Allowance",
      description: "Allowance for weather-related delays and impacts",
      quantity: Math.ceil(subtotal * 0.02),
      unit: "ea",
      category: "Contingencies",
      unitPrice: 1,
      aiConfidence: 0.7,
    },
  ];
}

// Fallback Processing Function
async function fallbackProcessing(
  fileData,
  fileType,
  projectContext,
  originalError
) {
  console.log("Using fallback processing due to error:", originalError.message);

  try {
    // Basic text extraction as fallback
    const basicContent = await extractTextContent(fileData, fileType);

    // Generate minimal line items
    const fallbackItems = [
      {
        itemId: `FB_${Date.now()}`,
        name: "Blueprint Analysis - Manual Review Required",
        description: `Automated processing failed: ${originalError.message}. Manual review and estimation required.`,
        quantity: 1,
        unit: "ls",
        category: "General",
        unitPrice: 0,
        lineTotal: 0,
        isAiGenerated: true,
        aiConfidence: 0.2,
        notes: `Fallback processing used. Original error: ${originalError.message}`,
      },
    ];

    return {
      success: false,
      lineItems: fallbackItems,
      metadata: {
        blueprintTypes: ["unknown"],
        confidence: 0.2,
        coverage: 10,
        processingTime: Date.now(),
        fallbackUsed: true,
        error: originalError.message,
      },
      summary: {
        totalItems: 1,
        totalValue: 0,
        categories: ["General"],
        requiresPMReview: true,
        manualReviewRequired: true,
      },
    };
  } catch (fallbackError) {
    console.error("Fallback processing also failed:", fallbackError);

    // Ultimate fallback
    return {
      success: false,
      lineItems: [],
      metadata: {
        blueprintTypes: ["error"],
        confidence: 0,
        coverage: 0,
        processingTime: Date.now(),
        fallbackUsed: true,
        errors: [originalError.message, fallbackError.message],
      },
      summary: {
        totalItems: 0,
        totalValue: 0,
        categories: [],
        requiresPMReview: true,
        manualReviewRequired: true,
        systemError: true,
      },
    };
  }
}

module.exports = ParseBlueprintFlow;
