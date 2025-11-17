// No need for pdf-parse - we'll upload PDFs directly to Gemini
const mammoth = require("mammoth");
const { googleAI } = require("@genkit-ai/googleai");
const { genkit } = require("genkit");
const { fromBuffer: pdfToPicFromBuffer } = require("pdf2pic");
const Tesseract = require("tesseract.js");
const { processDocumentWithAI } = require("../documentAI");

const ai = genkit({
  plugins: [googleAI()],
  systemMessage:
    "You are a construction analysis AI specialized in South African building practices, standards, and market conditions. Always provide analysis that is relevant to South African construction context.",
});

// Numeric sanitizer used across phases
function toNumberSafe(val) {
  if (val === null || val === undefined) return NaN;
  if (typeof val === "number") return Number.isFinite(val) ? val : NaN;
  if (typeof val === "string") {
    const m = val.replace(/,/g, "").match(/-?\d+(\.\d+)?/);
    return m ? Number(m[1] ? m[0] : m[0]) : NaN;
  }
  return NaN;
}

// Retry configuration
const RETRY_CONFIG = {
  maxRetries: 3,
  baseDelay: 1000, // 1 second
  maxDelay: 10000, // 10 seconds
  backoffMultiplier: 2,
};

// Available models in order of preference
const MODEL_OPTIONS = [
  "gemini-2.0-flash",
  "gemini-1.5-pro",
  "gemini-1.5-flash",
];

// Retry utility function
async function retryWithBackoff(fn, context = "API call") {
  let lastError;

  for (let attempt = 0; attempt <= RETRY_CONFIG.maxRetries; attempt++) {
    try {
      return await fn();
    } catch (error) {
      lastError = error;

      // Check if it's a retryable error
      if (
        error.status === 503 ||
        error.status === 429 ||
        error.status === 500
      ) {
        if (attempt < RETRY_CONFIG.maxRetries) {
          const delay = Math.min(
            RETRY_CONFIG.baseDelay *
              Math.pow(RETRY_CONFIG.backoffMultiplier, attempt),
            RETRY_CONFIG.maxDelay
          );

          console.log(
            `${context} failed (attempt ${attempt + 1}/${
              RETRY_CONFIG.maxRetries + 1
            }), retrying in ${delay}ms...`
          );
          await new Promise((resolve) => setTimeout(resolve, delay));
          continue;
        }
      }

      // Non-retryable error or max retries reached
      throw error;
    }
  }

  throw lastError;
}

// Optimized AI generation with model fallback
async function generateWithFallback(prompt, options = {}) {
  const { media, image, modelOverride } = options;

  for (let modelIndex = 0; modelIndex < MODEL_OPTIONS.length; modelIndex++) {
    const modelName = modelOverride || MODEL_OPTIONS[modelIndex];

    try {
      return await retryWithBackoff(async () => {
        const requestOptions = {
          model: googleAI.model(modelName),
          prompt: prompt,
        };

        // Attach both media and image if provided (no mutual exclusion)
        if (media) requestOptions.media = media;
        if (image) requestOptions.image = image;

        return await ai.generate(requestOptions);
      }, `AI generation with ${modelName}`);
    } catch (error) {
      console.warn(
        `Model ${modelName} failed, trying next model...`,
        error.message
      );

      // If this is the last model, throw the error
      if (modelIndex === MODEL_OPTIONS.length - 1) {
        throw error;
      }
    }
  }
}

// Enhanced Blueprint Processing Flow with Agent Orchestration
const ParseBlueprintFlow = ai.defineFlow(
  "EnhancedParseBlueprintFlow",
  async (input) => {
    const { fileData, fileType, projectContext = {} } = input;

    try {
      // Phase 0: Quick Document AI extraction for blueprint verification
      console.log(
        "🔍 [PHASE 0] Starting Document AI extraction for blueprint verification"
      );
      const extractedContent = await extractTextContent(fileData, fileType);

      // Store Document AI extracted entities
      const documentAIEntities = extractedContent.metadata?.entities || {};
      const entityCount = extractedContent.metadata?.entityCount || 0;
      const entityKeys = Object.keys(documentAIEntities);

      console.log("✅ [PHASE 0] Document AI extraction completed");
      console.log(
        "📋 [PHASE 0] Document AI entities extracted:",
        entityKeys.length > 0 ? entityKeys : "[]"
      );
      console.log("📊 [PHASE 0] Total entity count:", entityCount);

      // Phase 0: Strict property blueprint verification using Document AI entities + Gemini
      const verdict = await verifyIsPropertyBlueprint(
        fileData,
        fileType,
        documentAIEntities,
        extractedContent
      );

      // Reject if no Document AI entities found (strong indicator it's not a blueprint)
      if (entityCount === 0 || entityKeys.length === 0) {
        console.log(
          "❌ [PHASE 0] Rejected: No Document AI entities found - not a blueprint"
        );
        return {
          success: false,
          errorCode: "NotBlueprint",
          message:
            "File does not appear to be a property/building blueprint. No blueprint-specific data was extracted.",
          metadata: {
            confidence: 0.95,
            reason:
              "No Document AI entities extracted (roomName, roomSurfaceArea, Scale, surfaceArea, wallDimensions)",
            documentAIEntities: entityKeys,
            entityCount: entityCount,
            projectContext,
          },
        };
      }

      // Reject ONLY when we are very confident it's NOT a property blueprint
      if (
        (verdict?.isBlueprint === false || verdict?.isProperty === false) &&
        Number(verdict?.confidence || 0) >= 0.95
      ) {
        console.log("❌ [PHASE 0] Rejected as non-property blueprint", verdict);
        return {
          success: false,
          errorCode: "NotBlueprint",
          message:
            "File is not a property/building blueprint or confidence is too low",
          metadata: {
            confidence: verdict?.confidence ?? 0,
            reason: verdict?.reason || "Unspecified",
            documentAIEntities: entityKeys,
            entityCount: entityCount,
            projectContext,
          },
        };
      }

      console.log(
        `Processing blueprint: Type=${fileType}, Project=${
          projectContext.projectId || "Unknown"
        }`
      );

      // Ensure we have valid text content
      if (!extractedContent || typeof extractedContent.text !== "string") {
        console.warn("⚠️ [PHASE 1] Invalid extracted content, using fallback");
        extractedContent.text = "No text content available for analysis";
      }

      // Phase 2: Blueprint Analysis
      console.log("🔍 [PHASE 2] Starting blueprint analysis");
      const analysis = await analyzeBlueprint(extractedContent, projectContext);

      // Phase 3: Line Item Extraction
      const lineItems = await extractLineItems(analysis, projectContext);

      // Phase 4: Material Quantity Calculation
      console.log("🔍 [PHASE 4] Starting material quantity calculation");
      const quantifiedLineItems = await calculateMaterialQuantities(
        lineItems,
        analysis,
        projectContext,
        extractedContent, // Pass extracted content instead of raw fileData
        documentAIEntities // Pass Document AI entities
      );

      // Phase 5: Holistic Coverage Enhancement
      console.log("🔍 [PHASE 5] Starting holistic coverage enhancement");
      const completeLineItems = await enhanceHolisticCoverage(
        quantifiedLineItems,
        analysis,
        projectContext
      );

      // Deduplicate items based on name and category
      const deduplicatedItems = deduplicateLineItems(completeLineItems);
      console.log(
        `🔍 [PHASE 5] Deduplication: ${completeLineItems.length} → ${deduplicatedItems.length} items`
      );

      // Phase 6: Validation and Confidence Scoring
      console.log("🔍 [PHASE 6] Starting validation and scoring");
      const validatedItems = await validateAndScore(
        deduplicatedItems,
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
            (sum, item) =>
              sum + (item.itemType === "General" ? item.lineTotal || 0 : 0),
            0
          ),
          categories: [
            ...new Set(validatedItems.items.map((item) => item.category)),
          ],
          itemTypes: [
            ...new Set(validatedItems.items.map((item) => item.itemType)),
          ],
          generalItems: validatedItems.items.filter(
            (item) => item.itemType === "General"
          ).length,
          materialItems: validatedItems.items.filter(
            (item) => item.itemType === "Material"
          ).length,
          requiresPMReview: validatedItems.averageConfidence < 0.8,
        },
      };

      console.log(
        `✅ [COMPLETE] Blueprint processing completed: ${result.lineItems.length} line items generated`
      );

      // Clean summary logging
      const materialItems = result.lineItems.filter(
        (item) => item.itemType === "Material"
      );
      const generalItems = result.lineItems.filter(
        (item) => item.itemType === "General"
      );

      console.log(
        `📊 [SUMMARY] General items: ${generalItems.length}, Material items: ${
          materialItems.length
        }, Total value: R${result.summary.totalValue.toLocaleString()}`
      );

      if (materialItems.length > 0) {
        console.log("🔧 [MATERIALS] Found materials:");
        materialItems.slice(0, 5).forEach((item, index) => {
          console.log(`  ${index + 1}. ${item.name} (${item.category})`);
        });
        if (materialItems.length > 5) {
          console.log(`  ... and ${materialItems.length - 5} more materials`);
        }
      }

      return result;
    } catch (error) {
      console.error("Blueprint processing error:", error);

      // Short-circuit for strict dimension failure
      if (
        typeof error?.message === "string" &&
        error.message.startsWith("DimensionsUnavailable")
      ) {
        return {
          success: false,
          lineItems: [],
          errorCode: "DimensionsUnavailable",
          message:
            "Real dimensions could not be determined from the blueprint. Processing stopped.",
          metadata: {
            processingTime: Date.now(),
            projectContext: projectContext,
            reason: error.message,
          },
          summary: {
            totalItems: 0,
            totalValue: 0,
            categories: [],
            requiresPMReview: true,
          },
        };
      }

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

// Phase 0: Property/Building Blueprint Verifier
async function verifyIsPropertyBlueprint(
  fileData,
  fileType,
  documentAIEntities = {},
  extractedContent = null
) {
  console.log(
    "🔍 [PHASE 0] Verifying blueprint with Document AI entities and Gemini"
  );

  // Check Document AI entities first
  const entityKeys = Object.keys(documentAIEntities);
  const hasBlueprintEntities = entityKeys.length > 0;
  const blueprintEntityNames = [
    "roomName",
    "roomSurfaceArea",
    "Scale",
    "surfaceArea",
    "wallDimensions",
  ];
  const foundBlueprintEntities = entityKeys.filter((key) =>
    blueprintEntityNames.some((bpName) =>
      key.toLowerCase().includes(bpName.toLowerCase())
    )
  );

  console.log("📋 [PHASE 0] Found blueprint entities:", foundBlueprintEntities);

  // If we have Document AI entities, it's likely a blueprint
  let documentAIConfidence = 0;
  if (hasBlueprintEntities) {
    // Higher confidence if we found specific blueprint-related entities
    if (foundBlueprintEntities.length > 0) {
      documentAIConfidence = Math.min(
        0.9,
        0.5 + foundBlueprintEntities.length * 0.1
      );
    } else {
      documentAIConfidence = 0.6; // Some entities found but not blueprint-specific
    }
  } else {
    documentAIConfidence = 0.2; // No entities found - likely not a blueprint
  }

  const prompt = `You are a strict classifier. Determine if the uploaded file is a property/building architectural blueprint (floor plans, elevations, sections, site plans).

${
  hasBlueprintEntities
    ? `Document AI Analysis:
- Found ${entityKeys.length} entity types: ${entityKeys.join(", ")}
- Blueprint-specific entities found: ${
        foundBlueprintEntities.length > 0
          ? foundBlueprintEntities.join(", ")
          : "none"
      }
- This suggests the document ${
        hasBlueprintEntities && foundBlueprintEntities.length > 0
          ? "IS likely"
          : "may not be"
      } a blueprint.

${
  extractedContent?.text
    ? `Extracted Text Preview:\n${extractedContent.text.substring(0, 500)}...`
    : ""
}`
    : ""
}

Return ONLY valid JSON with these fields and values (no comments, no types, no backticks, no extra text):
{"isBlueprint": true|false, "isProperty": true|false, "confidence": 0.0-1.0, "reason": "short reason"}`;

  let options;
  if (String(fileType).toLowerCase() === "pdf") {
    // Strip data URL prefix if present
    const base64 =
      typeof fileData === "string" && fileData.startsWith("data:")
        ? fileData.substring(fileData.indexOf(",") + 1)
        : fileData;
    const pdfBuffer = Buffer.from(base64, "base64");
    options = { media: { content: pdfBuffer, mimeType: "application/pdf" } };
  } else {
    options = { image: fileData };
  }

  const response = await generateWithFallback(prompt, {
    ...options,
    modelOverride: "gemini-2.0-flash",
  });
  let text;
  if (typeof response === "string") {
    text = response;
  } else if (response?.message?.content?.[0]?.text) {
    text = response.message.content[0].text;
  } else if (typeof response?.text === "function") {
    text = response.text();
  } else {
    text = response?.text || response?.output || "";
  }

  if (!text || typeof text !== "string") {
    return {
      isBlueprint: false,
      isProperty: false,
      confidence: 0,
      reason: "Empty response",
    };
  }

  // Extract JSON from code fences if present
  const match = text.match(/```(?:json)?\s*(\{[\s\S]*?\})\s*```/);
  if (match) {
    text = match[1];
  }

  try {
    const parsed = JSON.parse(text);

    // Combine Document AI confidence with Gemini confidence
    const geminiConfidence = Number(parsed.confidence || 0);
    const combinedConfidence = hasBlueprintEntities
      ? Math.min(1.0, documentAIConfidence * 0.4 + geminiConfidence * 0.6)
      : geminiConfidence * 0.8; // Lower weight if no Document AI entities

    // If no Document AI entities and Gemini says it's not a blueprint, increase confidence
    let finalIsBlueprint = Boolean(parsed.isBlueprint);
    let finalConfidence = combinedConfidence;

    if (!hasBlueprintEntities && !parsed.isBlueprint) {
      finalConfidence = Math.max(combinedConfidence, 0.85);
      console.log(
        "⚠️ [PHASE 0] No Document AI entities + Gemini says not blueprint - high confidence rejection"
      );
    } else if (
      hasBlueprintEntities &&
      foundBlueprintEntities.length > 0 &&
      parsed.isBlueprint
    ) {
      finalConfidence = Math.max(combinedConfidence, 0.8);
      console.log(
        "✅ [PHASE 0] Document AI entities found + Gemini confirms blueprint"
      );
    }

    return {
      isBlueprint: finalIsBlueprint,
      isProperty: Boolean(parsed.isProperty),
      confidence: finalConfidence,
      reason: `${String(parsed.reason || "")}${
        hasBlueprintEntities
          ? ` | Document AI: ${entityKeys.length} entities found`
          : " | Document AI: No entities found"
      }`,
    };
  } catch (e) {
    console.warn("Verifier JSON parse failed:", e.message);

    // Attempt regex-based extraction as a fallback (handles loose key:value answers)
    try {
      const ib = /isBlueprint["']?\s*:\s*(true|false)/i.exec(text);
      const ip = /isProperty["']?\s*:\s*(true|false)/i.exec(text);
      const conf = /confidence["']?\s*:\s*([0-9]*\.?[0-9]+)/i.exec(text);
      const reas = /reason["']?\s*:\s*["']([^"']+)["']/i.exec(text);

      if (ib || ip || conf || reas) {
        const geminiConfidence = conf ? Number(conf[1]) : 0;
        const combinedConfidence = hasBlueprintEntities
          ? Math.min(1.0, documentAIConfidence * 0.4 + geminiConfidence * 0.6)
          : geminiConfidence * 0.8;

        return {
          isBlueprint: ib ? ib[1].toLowerCase() === "true" : false,
          isProperty: ip ? ip[1].toLowerCase() === "true" : false,
          confidence: combinedConfidence,
          reason: `${reas ? reas[1] : "Parsed from non-JSON response"}${
            hasBlueprintEntities
              ? ` | Document AI: ${entityKeys.length} entities found`
              : " | Document AI: No entities found"
          }`,
        };
      }
    } catch (rex) {
      console.warn("Regex fallback parse failed:", rex.message);
    }

    // Second attempt: re-query with an even stricter prompt and explicit example
    const strictPrompt = `Return ONLY this exact JSON shape (no extra text, no backticks):
{"isBlueprint": true|false, "isProperty": true|false, "confidence": 0.0-1.0, "reason": "short reason"}`;
    try {
      const second = await generateWithFallback(strictPrompt, {
        ...options,
        modelOverride: "gemini-1.5-pro",
      });
      let secondText;
      if (typeof second === "string") secondText = second;
      else if (second?.message?.content?.[0]?.text)
        secondText = second.message.content[0].text;
      else if (typeof second?.text === "function") secondText = second.text();
      else secondText = second?.text || second?.output || "";

      const m2 = secondText.match(/```(?:json)?\s*(\{[\s\S]*?\})\s*```/);
      if (m2) secondText = m2[1];

      const parsed2 = JSON.parse(secondText);

      const geminiConfidence2 = Number(parsed2.confidence || 0);
      const combinedConfidence2 = hasBlueprintEntities
        ? Math.min(1.0, documentAIConfidence * 0.4 + geminiConfidence2 * 0.6)
        : geminiConfidence2 * 0.8;

      return {
        isBlueprint: Boolean(parsed2.isBlueprint),
        isProperty: Boolean(parsed2.isProperty),
        confidence: combinedConfidence2,
        reason: `${String(parsed2.reason || "")}${
          hasBlueprintEntities
            ? ` | Document AI: ${entityKeys.length} entities found`
            : " | Document AI: No entities found"
        }`,
      };
    } catch (e2) {
      console.warn("Second-attempt verifier parse failed:", e2.message);
      // If no Document AI entities, high confidence it's not a blueprint
      const fallbackConfidence = hasBlueprintEntities ? 0.3 : 0.85;
      return {
        isBlueprint: false,
        isProperty: false,
        confidence: fallbackConfidence,
        reason: `Unparsable verifier response${
          hasBlueprintEntities
            ? ` | Document AI: ${entityKeys.length} entities found`
            : " | Document AI: No entities found"
        }`,
      };
    }
  }
}

// Phase 1: Enhanced Text Extraction Agent
async function extractTextContent(fileData, fileType) {
  try {
    switch (fileType.toLowerCase()) {
      case "pdf":
        // Use Document AI to process PDF blueprints
        try {
          console.log("🔍 [PHASE 1] Processing PDF with Document AI...");
          const pdfBuffer = Buffer.from(fileData, "base64");
          const documentAIResult = await processDocumentWithAI(
            pdfBuffer,
            "application/pdf"
          );

          // Combine extracted text with custom schema entities
          let extractedText = documentAIResult.text || "";

          // Append custom schema fields as structured text for downstream processing
          if (
            documentAIResult.entities &&
            Object.keys(documentAIResult.entities).length > 0
          ) {
            extractedText += "\n\n=== Document AI Extracted Fields ===\n";
            for (const [fieldName, values] of Object.entries(
              documentAIResult.entities
            )) {
              extractedText += `\n${fieldName}:\n`;
              values.forEach((item, index) => {
                extractedText += `  ${index + 1}. ${item.value}\n`;
              });
            }
          }

          return {
            text: extractedText || "No text extracted from PDF",
            pages: documentAIResult.pages || "Unknown",
            metadata: {
              title: "PDF Blueprint",
              author: "Unknown",
              creationDate: new Date().toISOString(),
              extractedBy: "Document AI",
              entities: documentAIResult.entities,
              entityCount: documentAIResult.metadata?.entityCount || 0,
            },
          };
        } catch (pdfError) {
          console.error("Document AI PDF processing error:", pdfError);
          // Fallback to Gemini if Document AI fails
          console.log(
            "⚠️ [PHASE 1] Falling back to Gemini for PDF processing..."
          );
          const pdfBuffer = Buffer.from(fileData, "base64");
          const pdfResponse = await generateWithFallback(
            `Extract construction details from this PDF blueprint:
            - Measurements and dimensions
            - Material specifications
            - Room layouts and structural elements
            - MEP details
            - Material lists
            
            Provide structured analysis.`,
            {
              media: {
                content: pdfBuffer,
                mimeType: "application/pdf",
              },
            }
          );

          // Handle different response formats
          let extractedText = "";
          if (typeof pdfResponse === "string") {
            extractedText = pdfResponse;
          } else if (pdfResponse && pdfResponse.output) {
            extractedText = pdfResponse.output;
          } else if (pdfResponse && pdfResponse.text) {
            extractedText = pdfResponse.text;
          } else if (pdfResponse && typeof pdfResponse.text === "function") {
            extractedText = pdfResponse.text();
          } else {
            extractedText = "No text extracted from PDF";
          }

          return {
            text: extractedText || "No text extracted from PDF",
            pages: "Unknown",
            metadata: {
              title: "PDF Blueprint (Fallback)",
              author: "Unknown",
              creationDate: new Date().toISOString(),
              extractedBy: "Gemini (Fallback)",
              error: pdfError.message,
            },
          };
        }

      case "docx":
        // For Word docs, we'll extract text using mammoth as fallback
        // In a production environment, you might want to convert to PDF first
        const docxResult = await mammoth.extractRawText({
          buffer: Buffer.from(fileData, "base64"),
        });

        // If we have substantial text, we can also send it to Gemini for analysis
        if (docxResult.value && docxResult.value.length > 100) {
          const docxAnalysis = await generateWithFallback(
            `Extract from construction document:
            - Measurements and dimensions
            - Material specifications
            - Room layouts and structural elements
            - Material lists
            
            Document: ${docxResult.value.substring(0, 1500)}...`
          );

          return {
            text: docxAnalysis.output || docxAnalysis.text || docxResult.value,
            metadata: {
              wordCount: docxResult.value.split(/\s+/).length,
              hasImages: docxResult.messages.some(
                (msg) => msg.type === "image"
              ),
            },
          };
        }

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

      case "png":
      case "jpg":
      case "jpeg":
      case "tiff":
      case "bmp":
      case "gif":
        // Use Document AI for image processing
        try {
          console.log(
            `🔍 [PHASE 1] Processing ${fileType.toUpperCase()} image with Document AI...`
          );
          const imageBuffer = Buffer.from(fileData, "base64");
          const mimeType = `image/${fileType === "jpg" ? "jpeg" : fileType}`;
          const documentAIResult = await processDocumentWithAI(
            imageBuffer,
            mimeType
          );

          // Combine extracted text with custom schema entities
          let extractedText = documentAIResult.text || "";

          // Append custom schema fields as structured text for downstream processing
          if (
            documentAIResult.entities &&
            Object.keys(documentAIResult.entities).length > 0
          ) {
            extractedText += "\n\n=== Document AI Extracted Fields ===\n";
            for (const [fieldName, values] of Object.entries(
              documentAIResult.entities
            )) {
              extractedText += `\n${fieldName}:\n`;
              values.forEach((item, index) => {
                extractedText += `  ${index + 1}. ${item.value}\n`;
              });
            }
          }

          return {
            text: extractedText || "No text extracted from image",
            metadata: {
              extractedBy: "Document AI",
              fileType: fileType,
              entities: documentAIResult.entities,
              entityCount: documentAIResult.metadata?.entityCount || 0,
            },
          };
        } catch (imageError) {
          console.error("Document AI image processing error:", imageError);
          // Fallback to Gemini vision if Document AI fails
          console.log(
            "⚠️ [PHASE 1] Falling back to Gemini Vision for image processing..."
          );
          const visionResponse = await generateWithFallback(
            `Extract from blueprint image:
            - Measurements and dimensions
            - Material specifications
            - Room layouts and structural elements
            - MEP details
            
            Return structured format.`,
            { image: fileData }
          );

          return {
            text:
              visionResponse.text ||
              visionResponse.output ||
              "No text extracted from image",
            metadata: {
              extractedBy: "Gemini Vision (Fallback)",
              fileType: fileType,
              confidence: "medium",
              error: imageError.message,
            },
          };
        }

      default: // Other formats - use existing methods
        const visionResponse = await generateWithFallback(
          `Extract from blueprint image:
          - Measurements and dimensions
          - Material specifications
          - Room layouts and structural elements
          - MEP details
          
          Return structured format.`,
          { image: fileData }
        );

        return {
          text:
            visionResponse.text ||
            visionResponse.output ||
            "No text extracted from image",
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
    // Ensure we have a string for text processing
    const textContent =
      typeof extractedContent.text === "string"
        ? extractedContent.text
        : JSON.stringify(extractedContent.text) || "No text content available";

    console.log("🔍 DEBUG: Text content type:", typeof extractedContent.text);
    console.log("🔍 DEBUG: Text content length:", textContent.length);

    const analysisPrompt = `Analyze blueprint and return JSON:
    {
      "blueprintTypes": ["architectural", "structural", "MEP"],
      "buildingType": "residential/commercial/industrial", 
      "squareFootage": number,
      "stories": number,
      "structuralElements": {foundation, walls, roof, floors},
      "mepSystems": {electrical, plumbing, hvac},
      "finishes": {flooring, walls, ceilings},
      "siteWork": {landscaping, parking, utilities}
    }
    
    Content: ${textContent.substring(0, 2000)}...
    Context: ${JSON.stringify(projectContext)}`;

    const analysisResponse = await generateWithFallback(analysisPrompt);

    let analysis;
    try {
      console.log(
        "🔍 [PHASE 2] Raw analysis response length:",
        analysisResponse.text.length
      );

      // Extract JSON from markdown code blocks if present
      let jsonText = analysisResponse.text;
      const jsonMatch = jsonText.match(/```(?:json)?\s*(\{[\s\S]*?\})\s*```/);
      if (jsonMatch) {
        jsonText = jsonMatch[1];
        console.log("✅ [PHASE 2] Extracted JSON from markdown");
      }

      analysis = JSON.parse(jsonText);
      console.log(
        "✅ [PHASE 2] Analysis parsed successfully - blueprint types:",
        analysis.blueprintTypes
      );
    } catch (parseError) {
      console.warn("❌ [PHASE 2] JSON parse failed:", parseError.message);
      analysis = { rawAnalysis: analysisResponse.text };
    }

    // Do not propagate rawContent to avoid noisy template text
    const { rawAnalysis, rawContent, ...cleanAnalysis } = analysis || {};
    return {
      ...cleanAnalysis,
      metadata: extractedContent.metadata,
      blueprintTypes:
        cleanAnalysis && cleanAnalysis.blueprintTypes
          ? cleanAnalysis.blueprintTypes
          : detectBlueprintTypes(extractedContent.text),
    };
  } catch (error) {
    console.error("Blueprint analysis error:", error);
    throw new Error(`Blueprint analysis failed: ${error.message}`);
  }
}

// Phase 3: Line Item Extraction Agent
async function extractLineItems(analysis, projectContext) {
  try {
    const extractionPrompt = `Generate construction line items as JSON array for SOUTH AFRICAN construction project. MUST include BOTH general work items AND material items:

    CRITICAL: ALL PRICING MUST BE IN SOUTH AFRICAN RAND (ZAR) - NO USD VALUES ALLOWED!

    REQUIRED GENERAL ITEMS (work/services with costs - ADJUST PRICING based on blueprint analysis using SOUTH AFRICAN RAND):
    - Site Preparation and Earthwork: Adjust quantity/price based on project size, site conditions, and excavation needs (Typical range: R15,000 - R80,000)
    - Temporary Utilities and Facilities: Adjust based on project duration, complexity, and utility requirements (Typical range: R8,000 - R25,000)
    - Project Management and Supervision: Adjust based on project value, complexity, and duration (Typical range: R12,000 - R60,000)
    - Permits and Inspections: Adjust based on project type, size, and local requirements (Typical range: R5,000 - R20,000)
    - Temporary Equipment: Adjust based on project scope, duration, and equipment needs (Typical range: R6,000 - R30,000)
    - Safety and Security: Adjust based on project size, duration, and safety requirements (Typical range: R4,000 - R15,000)
    
    Use realistic SOUTH AFRICAN RAND pricing based on:
    - Project square footage: ${analysis.squareFootage || "unknown"}
    - Building type: ${analysis.buildingType || "unknown"}
    - Project complexity: ${analysis.blueprintTypes?.join(", ") || "unknown"}
    - Construction scope: ${JSON.stringify(analysis.structuralElements || {})}
    - South African construction market rates (2024)
    
    Each general item should have: name, description, quantity (based on project needs), unit, category, itemType: "General", unitPrice (realistic ZAR market rates - NO $ SYMBOLS), lineTotal

    MATERIAL ITEMS (materials with NO costs - analyze blueprint for ALL materials mentioned):
    - Look for ANY materials mentioned in the blueprint analysis
    - CRITICAL: ALWAYS include BRICK materials for external walls (assume brick construction unless specified otherwise)
    - Include specific materials like: concrete, steel, wood, glass, insulation, drywall, etc.
    - Include fixtures like: doors, windows, lighting, plumbing fixtures, etc.
    - Include finishes like: paint, flooring, tiles, trim, etc.
    - Include systems like: electrical components, plumbing components, HVAC components, etc.
    - MANDATORY: Generate brick materials for external walls (e.g., "Brick", "Masonry", "External Wall Brick")
    - Each material should have: name, description, quantity: 0, unit: "N/A", category, itemType: "Material", unitPrice: 0, lineTotal: 0

    Analysis: ${JSON.stringify(analysis).substring(0, 1000)}...
    
    Return JSON array with BOTH general work items (with dynamic ZAR pricing - NO $ SYMBOLS) AND all materials found in the blueprint.`;

    console.log("🔍 [PHASE 3] Starting line item extraction");

    const extractionResponse = await generateWithFallback(extractionPrompt);

    let lineItems;
    try {
      console.log(
        "🔍 [PHASE 3] Raw extraction response length:",
        extractionResponse.text.length
      );

      // Extract JSON from markdown code blocks if present
      let jsonText = extractionResponse.text;
      const jsonMatch = jsonText.match(/```(?:json)?\s*(\[[\s\S]*?\])\s*```/);
      if (jsonMatch) {
        jsonText = jsonMatch[1];
        console.log("✅ [PHASE 3] Extracted JSON array from markdown");
      }

      lineItems = JSON.parse(jsonText);
      console.log(
        "✅ [PHASE 3] Line items parsed successfully - count:",
        lineItems.length
      );
      if (!Array.isArray(lineItems)) {
        throw new Error("Response is not an array");
      }
    } catch (parseError) {
      console.warn("❌ [PHASE 3] JSON parse failed:", parseError.message);
      lineItems = await parseLineItemsFromText(extractionResponse.text);
    }

    // Process and validate line items
    console.log("🔍 [PHASE 3] Processing line items, count:", lineItems.length);
    lineItems = lineItems.map((item, index) => {
      const itemId = `LI_${Date.now()}_${Math.random()
        .toString(36)
        .substr(2, 9)}`;
      const itemType = item.itemType || "General";

      if (itemType === "Material") {
        // Material items have no quantity, unit price, or line total
        const materialItem = {
          itemId,
          name: item.name || "Unnamed Material",
          description: item.description || "",
          quantity: 0,
          unit: "N/A",
          unitOfQuantity: "N/A", // New field for unit of quantity
          category: item.category || "General",
          itemType: "Material",
          unitPrice: 0,
          lineTotal: 0,
          isAiGenerated: true,
          aiConfidence: Math.min(
            1.0,
            Math.max(0.0, parseFloat(item.aiConfidence) || 0.7)
          ),
          materialDatabaseId: item.materialDatabaseId || null,
          materialSpecifications:
            item.materialSpecifications || item.description || "",
          notes:
            item.notes ||
            `Material identified from blueprint analysis - ${
              analysis.blueprintTypes?.join(", ") || "general construction"
            }`,
        };
        return materialItem;
      } else {
        // General items have quantities, unit prices, and line totals
        const quantity = parseFloat(item.quantity) || 1;
        let unitPrice = parseFloat(item.unitPrice) || 0;

        // Ensure pricing is in ZAR (convert if needed)
        if (unitPrice > 0 && unitPrice < 1000) {
          // Likely USD values, convert to ZAR (roughly 5:1 ratio)
          unitPrice = Math.round(unitPrice * 5);
        }

        const generalItem = {
          itemId,
          name: item.name || "Unnamed Item",
          description: item.description || "",
          quantity,
          unit: item.unit || "ea",
          category: item.category || "General",
          itemType: "General",
          unitPrice,
          lineTotal: quantity * unitPrice,
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
        };
        return generalItem;
      }
    });

    return lineItems;
  } catch (error) {
    console.error("Line item extraction error:", error);
    throw new Error(`Line item extraction failed: ${error.message}`);
  }
}

// Phase 4: Material Quantity Calculation Agent
async function calculateMaterialQuantities(
  lineItems,
  analysis,
  projectContext,
  extractedContent,
  documentAIEntities
) {
  try {
    console.log("🔍 [PHASE 4] Analyzing blueprint for dimensions and scale");

    // Extract dimensions and scale from blueprint analysis using Document AI data
    const dimensions = await extractBlueprintDimensions(
      analysis,
      projectContext,
      extractedContent,
      documentAIEntities
    );
    console.log("📐 [PHASE 4] Extracted dimensions:", dimensions);

    // Proceed if we have a numeric totalArea, even if walls are missing.
    // Only fail when both totalArea is not numeric AND no walls are available.
    if (
      !dimensions ||
      ((dimensions.totalArea === undefined ||
        isNaN(Number(dimensions.totalArea))) &&
        (!Array.isArray(dimensions.walls) || dimensions.walls.length === 0))
    ) {
      throw new Error("DimensionsUnavailable: Missing critical dimensions");
    }

    // Calculate quantities for each material item
    const quantifiedItems = lineItems.map((item) => {
      if (item.itemType === "Material") {
        const { quantity, unitOfQuantity } = calculateMaterialQuantity(
          item,
          dimensions,
          analysis
        );
        console.log(
          `🔢 [PHASE 4] ${item.name}: ${item.quantity} → ${quantity} ${unitOfQuantity}`
        );
        return {
          ...item,
          quantity: quantity,
          unitOfQuantity: unitOfQuantity,
          notes: item.notes
            ? `${item.notes} | Calculated quantity based on dimensions`
            : "Calculated quantity based on dimensions",
        };
      }
      return item;
    });

    console.log(
      `✅ [PHASE 4] Material quantity calculation completed for ${
        quantifiedItems.filter((item) => item.itemType === "Material").length
      } materials`
    );
    return quantifiedItems;
  } catch (error) {
    console.error("❌ [PHASE 4] Material quantity calculation error:", error);
    throw error;
  }
}

// Extract dimensions and scale from blueprint analysis
async function extractBlueprintDimensions(
  analysis,
  projectContext,
  extractedContent,
  documentAIEntities
) {
  try {
    const {
      rawContent: _rawContent,
      rawAnalysis: _rawAnalysis,
      ...analysisClean
    } = analysis || {};
    console.log(
      "🔍 [PHASE 4] Full blueprint analysis (sanitized):",
      JSON.stringify(analysisClean, null, 2)
    );

    // First, try to extract dimensions from Document AI entities
    console.log(
      "📋 [PHASE 4] Attempting to extract dimensions from Document AI entities"
    );
    let dimensions = {};
    let hasDocumentAIData = false;

    // Extract Scale from Document AI entities
    if (documentAIEntities.Scale && documentAIEntities.Scale.length > 0) {
      const scaleValue = documentAIEntities.Scale[0].value;
      dimensions.scale = scaleValue;
      hasDocumentAIData = true;
      console.log("✅ [PHASE 4] Found scale from Document AI:", scaleValue);
    }

    // Extract surfaceArea (total area) from Document AI entities
    if (
      documentAIEntities.surfaceArea &&
      documentAIEntities.surfaceArea.length > 0
    ) {
      const areaValue = documentAIEntities.surfaceArea[0].value;
      // Try to extract numeric value
      const areaMatch = areaValue.match(/(\d+(?:\.\d+)?)/);
      if (areaMatch) {
        dimensions.totalArea = parseFloat(areaMatch[1]);
        hasDocumentAIData = true;
        console.log(
          "✅ [PHASE 4] Found total area from Document AI:",
          dimensions.totalArea
        );
      }
    }

    // Extract wallDimensions from Document AI entities
    if (
      documentAIEntities.wallDimensions &&
      documentAIEntities.wallDimensions.length > 0
    ) {
      const wallDims = documentAIEntities.wallDimensions;
      dimensions.walls = [];
      wallDims.forEach((wallDim, index) => {
        const wallText = wallDim.value;
        // Try to parse wall dimensions from text like "Office right wall: 5m"
        const wallMatch = wallText.match(/(\d+(?:\.\d+)?)\s*m/i);
        if (wallMatch) {
          dimensions.walls.push({
            name: `External Wall ${index + 1}`,
            length: parseFloat(wallMatch[1]),
            height: 2.5, // Default height
            thickness: 22, // Default thickness in cm
            material: "brick",
          });
        }
      });
      if (dimensions.walls.length > 0) {
        hasDocumentAIData = true;
        console.log(
          "✅ [PHASE 4] Found wall dimensions from Document AI:",
          dimensions.walls.length
        );
      }
    }

    // Extract roomSurfaceArea for additional area data
    if (
      documentAIEntities.roomSurfaceArea &&
      documentAIEntities.roomSurfaceArea.length > 0
    ) {
      const roomAreas = documentAIEntities.roomSurfaceArea;
      let totalRoomArea = 0;
      roomAreas.forEach((roomArea) => {
        const areaText = roomArea.value;
        // Try to extract area from format like "roomName:surfaceArea"
        const areaMatch = areaText.match(/:?\s*(\d+(?:\.\d+)?)/);
        if (areaMatch) {
          totalRoomArea += parseFloat(areaMatch[1]);
        }
      });
      if (totalRoomArea > 0 && !dimensions.totalArea) {
        dimensions.totalArea = totalRoomArea;
        hasDocumentAIData = true;
        console.log(
          "✅ [PHASE 4] Calculated total area from room areas:",
          totalRoomArea
        );
      }
    }

    // If we have sufficient Document AI data, use it and enhance with AI analysis
    if (
      hasDocumentAIData &&
      (dimensions.scale ||
        dimensions.totalArea ||
        (dimensions.walls && dimensions.walls.length > 0))
    ) {
      console.log(
        "✅ [PHASE 4] Using Document AI extracted dimensions as base"
      );
    }

    const dimensionPrompt = `You are a construction estimator analyzing a blueprint. Extract the actual building dimensions from this blueprint drawing and the analysis context.

    Blueprint Analysis: ${JSON.stringify(analysisClean).substring(0, 1000)}...
    
    ${
      hasDocumentAIData
        ? `Document AI Extracted Data:
    - Scale: ${dimensions.scale || "not found"}
    - Total Area: ${dimensions.totalArea || "not found"} m²
    - Wall Dimensions: ${
      dimensions.walls ? JSON.stringify(dimensions.walls) : "not found"
    }
    
    Use the Document AI extracted data above as a starting point, but verify and enhance with additional details from the blueprint analysis.`
        : ""
    }
    
    IMPORTANT: Look at the actual blueprint content and extract REAL dimensions, not estimates. Look for:
    - Scale indicators (1:50, 1:100, etc.) - use the actual scale from the blueprint
    - Room dimensions and total area - calculate from actual measurements
    - Wall lengths and heights - measure from the blueprint
    - Window and door counts and sizes - count actual openings
    - Foundation specifications - use actual foundation details
    
    CRITICAL: Assume ALL external walls are built with BRICK unless specifically noted otherwise in the blueprint.
    
    If you cannot find specific dimensions, do not estimate. Return an error by refusing to fabricate any values.
    
    Return ONLY this JSON object (no other text):
    {
      "scale": "actual scale from blueprint",
      "totalArea": "actual calculated area in square meters",
      "walls": [
        {"name": "External Wall 1", "length": "actual length in meters", "height": "actual height in meters", "thickness": "actual thickness in cm", "material": "brick"},
        {"name": "External Wall 2", "length": "actual length in meters", "height": "actual height in meters", "thickness": "actual thickness in cm", "material": "brick"},
        {"name": "External Wall 3", "length": "actual length in meters", "height": "actual height in meters", "thickness": "actual thickness in cm", "material": "brick"},
        {"name": "External Wall 4", "length": "actual length in meters", "height": "actual height in meters", "thickness": "actual thickness in cm", "material": "brick"}
      ],
      "floors": [
        {"name": "Ground Floor", "area": "actual area in square meters", "thickness": "actual thickness in mm"}
      ],
      "roof": {
        "area": "actual roof area in square meters",
        "pitch": "actual pitch in degrees",
        "material": "actual material"
      },
      "openings": [
        {"type": "window", "width": "actual width in cm", "height": "actual height in cm", "quantity": "actual count"},
        {"type": "door", "width": "actual width in cm", "height": "actual height in cm", "quantity": "actual count"}
      ],
      "foundation": {
        "area": "actual foundation area in square meters",
        "thickness": "actual thickness in meters",
        "perimeter": "actual perimeter in meters"
      }
    }`;

    // Use extracted text content instead of raw file data
    const extractedText = extractedContent?.text || "";

    // Enhance the prompt with extracted text from Document AI
    const enhancedPrompt = `${dimensionPrompt}\n\nExtracted Blueprint Text:\n${extractedText.substring(
      0,
      3000
    )}`;

    console.log(
      "📎 [PHASE 4] Using Document AI extracted text for dimension extraction"
    );
    const dimensionResponse = await generateWithFallback(enhancedPrompt, {
      modelOverride: "gemini-2.0-flash",
    });

    // Extract JSON from response and merge with Document AI extracted dimensions
    let aiDimensions;
    try {
      let responseStr;
      if (typeof dimensionResponse === "string") {
        responseStr = dimensionResponse;
      } else if (dimensionResponse && typeof dimensionResponse === "object") {
        if (
          dimensionResponse.message &&
          dimensionResponse.message.content &&
          dimensionResponse.message.content[0] &&
          typeof dimensionResponse.message.content[0].text === "string"
        ) {
          responseStr = dimensionResponse.message.content[0].text;
          console.log(
            "🔍 [PHASE 4] Extracted text from GenKit response object"
          );
        } else if (typeof dimensionResponse.text === "function") {
          responseStr = dimensionResponse.text();
          console.log("🔍 [PHASE 4] Extracted text using text() function");
        } else {
          console.log(
            "🔍 [PHASE 4] Response object structure:",
            JSON.stringify(dimensionResponse, null, 2)
          );
          throw new Error("Cannot extract text from response object");
        }
      } else {
        responseStr = String(dimensionResponse);
      }
      const jsonMatch = responseStr.match(
        /```(?:json)?\s*(\{[\s\S]*?\})\s*```/
      );
      const toParse = jsonMatch ? jsonMatch[1] : responseStr;
      aiDimensions = JSON.parse(toParse);
      console.log("✅ [PHASE 4] Successfully parsed dimensions from AI");

      // Merge AI dimensions with Document AI extracted dimensions (Document AI takes precedence)
      dimensions = {
        ...aiDimensions,
        ...(dimensions.scale && { scale: dimensions.scale }),
        ...(dimensions.totalArea && { totalArea: dimensions.totalArea }),
        ...(dimensions.walls &&
          dimensions.walls.length > 0 && { walls: dimensions.walls }),
      };
    } catch (parseError) {
      console.log(
        "❌ [PHASE 4] Failed to parse dimensions JSON:",
        parseError.message
      );
      throw new Error("DimensionsUnavailable: Failed to parse dimensions JSON");
    }

    // Normalize numeric fields (strip units like 'm²', 'm', 'cm')
    const toNumber = (val) => {
      if (val === null || val === undefined) return NaN;
      if (typeof val === "number") return val;
      if (typeof val === "string") {
        const cleaned = val.replace(/,/g, "").match(/-?\d+(\.\d+)?/);
        return cleaned ? Number(cleaned[0]) : NaN;
      }
      return NaN;
    };

    if (dimensions) {
      const isPlaceholder = (s) =>
        typeof s === "string" &&
        /to be determined|cannot determine|error:/i.test(s);
      if (dimensions.totalArea !== undefined) {
        const n = isPlaceholder(dimensions.totalArea)
          ? NaN
          : toNumber(dimensions.totalArea);
        if (!Number.isNaN(n)) dimensions.totalArea = n;
      }
      if (dimensions.foundation && dimensions.foundation.area !== undefined) {
        const n = isPlaceholder(dimensions.foundation.area)
          ? NaN
          : toNumber(dimensions.foundation.area);
        if (!Number.isNaN(n)) dimensions.foundation.area = n;
      }
      if (
        dimensions.foundation &&
        dimensions.foundation.thickness !== undefined
      ) {
        const n = isPlaceholder(dimensions.foundation.thickness)
          ? NaN
          : toNumber(dimensions.foundation.thickness);
        if (!Number.isNaN(n)) dimensions.foundation.thickness = n;
      }
      if (Array.isArray(dimensions.walls)) {
        dimensions.walls = dimensions.walls.map((w) => {
          const ww = { ...w };
          if (ww.length !== undefined) {
            const n = isPlaceholder(ww.length) ? NaN : toNumber(ww.length);
            if (!Number.isNaN(n)) ww.length = n;
          }
          if (ww.height !== undefined) {
            const n = isPlaceholder(ww.height) ? NaN : toNumber(ww.height);
            if (!Number.isNaN(n)) ww.height = n;
            else ww.height = 2.5;
          }
          if (ww.thickness !== undefined) {
            const n = isPlaceholder(ww.thickness)
              ? NaN
              : toNumber(ww.thickness);
            if (!Number.isNaN(n)) ww.thickness = n;
            else ww.thickness = 22;
          }
          return ww;
        });
        dimensions.walls = dimensions.walls.filter(
          (w) =>
            Number.isFinite(Number(w.length)) &&
            Number.isFinite(Number(w.height))
        );
      }
    }

    // Derive floor area from walls if missing
    if (
      (dimensions?.totalArea === undefined ||
        Number.isNaN(Number(dimensions.totalArea))) &&
      Array.isArray(dimensions.walls) &&
      dimensions.walls.length >= 2
    ) {
      const lens = dimensions.walls
        .map((w) => toNumber(w.length))
        .filter((n) => Number.isFinite(n) && n > 0)
        .sort((a, b) => b - a);
      if (lens.length >= 2) {
        const approx = Number((lens[0] * lens[1]).toFixed(2));
        if (Number.isFinite(approx)) {
          dimensions.totalArea = approx;
          console.log(
            "📐 [PHASE 4] Derived totalArea from wall lengths:",
            approx
          );
        }
      }
    }

    // If totalArea still missing, try to derive from raw analysis content
    if (
      (dimensions?.totalArea === undefined ||
        Number.isNaN(Number(dimensions.totalArea))) &&
      typeof analysis?.rawContent === "string"
    ) {
      const areaMatch = analysis.rawContent.match(
        /total\s+area[^0-9]*([0-9]+(?:\.[0-9]+)?)\s*(m2|m²|sqm|square\s*meters?)/i
      );
      if (areaMatch) {
        dimensions.totalArea = Number(areaMatch[1]);
        console.log(
          "📐 [PHASE 4] Derived totalArea from text:",
          dimensions.totalArea
        );
      }
    }

    // Try to extract area/scale from extracted text if still missing
    if (
      (dimensions?.totalArea === undefined ||
        isNaN(Number(dimensions.totalArea)) ||
        dimensions?.scale === undefined ||
        /error/i.test(String(dimensions.scale || ""))) &&
      extractedText
    ) {
      try {
        // Extract area from text
        const areaMatchImg = extractedText.match(
          /total\s+area[^0-9]*([0-9]+(?:\.[0-9]+)?)\s*(m2|m²|sqm|square\s*meters?)/i
        );
        if (areaMatchImg && !dimensions.totalArea) {
          dimensions.totalArea = Number(areaMatchImg[1]);
          console.log(
            "📐 [PHASE 4] Derived totalArea from extracted text:",
            dimensions.totalArea
          );
        }
        // Extract scale from text
        const scaleMatch = extractedText.match(
          /scale[^0-9]*([1lI]\s*:\s*\d{1,4})/i
        );
        if (scaleMatch && !dimensions.scale) {
          dimensions.scale = scaleMatch[1].replace(/\s+/g, "");
          console.log(
            "📐 [PHASE 4] Derived scale from extracted text:",
            dimensions.scale
          );
        }
      } catch (textErr) {
        console.log(
          "⚠️ [PHASE 4] Text extraction fallback failed:",
          textErr.message
        );
      }
    }

    // If still missing scale, try focused read for scale + area from media
    if (
      dimensions?.scale === undefined ||
      /error/i.test(String(dimensions.scale || "")) ||
      dimensions?.totalArea === undefined ||
      Number.isNaN(Number(dimensions.totalArea))
    ) {
      try {
        const scalePrompt = `Read the blueprint. Return ONLY this JSON (no backticks):
{"scale": "e.g. 1:100", "totalArea": number}`;
        const scaleResp = await generateWithFallback(scalePrompt, {
          ...options,
          modelOverride: "gemini-2.0-flash",
        });
        let st;
        if (typeof scaleResp === "string") st = scaleResp;
        else if (scaleResp?.message?.content?.[0]?.text)
          st = scaleResp.message.content[0].text;
        else if (typeof scaleResp?.text === "function") st = scaleResp.text();
        else st = scaleResp?.text || scaleResp?.output || "";
        const m = st.match(/```(?:json)?\s*(\{[\s\S]*?\})\s*```/);
        if (m) st = m[1];
        const parsed = JSON.parse(st);
        if (parsed?.scale && typeof parsed.scale === "string") {
          dimensions.scale = parsed.scale;
        }
        const n = Number(parsed?.totalArea);
        if (!Number.isNaN(n) && n > 0) dimensions.totalArea = n;
      } catch (es) {
        console.log(
          "❌ [PHASE 4] Focused scale+area extraction failed:",
          es.message
        );
      }
    }

    // If totalArea still missing, try a focused second attempt to read only total area from the PDF/image
    if (
      dimensions?.totalArea === undefined ||
      isNaN(Number(dimensions.totalArea))
    ) {
      try {
        const areaOnlyPrompt = `Read the blueprint and return ONLY this JSON with the building total floor area in square meters (no text, no backticks):
{"totalArea": number}`;
        const areaResp = await generateWithFallback(areaOnlyPrompt, {
          ...options,
          modelOverride: "gemini-2.0-flash",
        });
        let areaText;
        if (typeof areaResp === "string") areaText = areaResp;
        else if (areaResp?.message?.content?.[0]?.text)
          areaText = areaResp.message.content[0].text;
        else if (typeof areaResp?.text === "function")
          areaText = areaResp.text();
        else areaText = areaResp?.text || areaResp?.output || "";
        const m = areaText.match(/```(?:json)?\s*(\{[\s\S]*?\})\s*```/);
        if (m) areaText = m[1];
        const parsedArea = JSON.parse(areaText);
        const n = Number(parsedArea?.totalArea);
        if (!Number.isNaN(n) && n > 0) {
          dimensions = { ...(dimensions || {}), totalArea: n };
          console.log("📐 [PHASE 4] Acquired totalArea via focused prompt:", n);
        }
      } catch (ea) {
        console.log("❌ [PHASE 4] Focused area extraction failed:", ea.message);
      }
    }

    // If walls are missing, try a focused walls-only pass and OCR derivation
    if (!Array.isArray(dimensions.walls) || dimensions.walls.length === 0) {
      try {
        const wallsPrompt = `Read the blueprint and return ONLY JSON exactly like this (no backticks):
{"walls":[{"name":"External Wall 1","length":number,"height":2.5,"thickness":22,"material":"brick"},
{"name":"External Wall 2","length":number,"height":2.5,"thickness":22,"material":"brick"},
{"name":"External Wall 3","length":number,"height":2.5,"thickness":22,"material":"brick"},
{"name":"External Wall 4","length":number,"height":2.5,"thickness":22,"material":"brick"}]}`;
        const wallsPromptWithText = `${wallsPrompt}\n\nExtracted Blueprint Text:\n${extractedText.substring(
          0,
          2000
        )}`;
        const wallsResp = await generateWithFallback(wallsPromptWithText, {
          modelOverride: "gemini-2.0-flash",
        });
        let wt =
          typeof wallsResp === "string"
            ? wallsResp
            : wallsResp?.message?.content?.[0]?.text ||
              (typeof wallsResp?.text === "function"
                ? wallsResp.text()
                : wallsResp?.text || wallsResp?.output || "");
        const mw = wt.match(/```(?:json)?\s*(\{[\s\S]*?\})\s*```/);
        if (mw) wt = mw[1];
        const parsedWalls = JSON.parse(wt);
        if (Array.isArray(parsedWalls?.walls) && parsedWalls.walls.length > 0) {
          dimensions.walls = parsedWalls.walls
            .map((w) => ({
              name: String(w.name || "External Wall"),
              length: Number(w.length),
              height: Number(w.height || 2.5),
              thickness: Number(w.thickness || 22),
              material: String(w.material || "brick"),
            }))
            .filter((w) => !Number.isNaN(w.length) && w.length > 0);
        }
      } catch (wErr) {
        console.log(
          "⚠️ [PHASE 4] Focused walls extraction failed:",
          wErr.message
        );
      }

      // Text-based derivation for perimeter if still empty
      if (
        (!dimensions.walls || dimensions.walls.length === 0) &&
        extractedText
      ) {
        try {
          // Extract dimension numbers from text
          const mmNums = [
            ...extractedText.matchAll(/(\d{3,5})\s*(mm|m)\b/gi),
          ].map((m) =>
            m[2].toLowerCase() === "mm" ? Number(m[1]) / 1000 : Number(m[1])
          );
          // Heuristic: pick two largest distinct lengths as width/length
          const sorted = mmNums
            .filter((n) => n > 0.5 && n < 200)
            .sort((a, b) => b - a);
          const unique = Array.from(
            new Set(sorted.map((n) => n.toFixed(2)))
          ).map((s) => Number(s));
          if (unique.length >= 2) {
            const a = unique[0],
              b = unique[1];
            dimensions.walls = [
              {
                name: "External Wall 1",
                length: a,
                height: 2.5,
                thickness: 22,
                material: "brick",
              },
              {
                name: "External Wall 2",
                length: a,
                height: 2.5,
                thickness: 22,
                material: "brick",
              },
              {
                name: "External Wall 3",
                length: b,
                height: 2.5,
                thickness: 22,
                material: "brick",
              },
              {
                name: "External Wall 4",
                length: b,
                height: 2.5,
                thickness: 22,
                material: "brick",
              },
            ];
            console.log(
              "📐 [PHASE 4] Derived walls from extracted text dimensions:",
              dimensions.walls
            );
          }
        } catch (perr) {
          console.log(
            "⚠️ [PHASE 4] Text-based perimeter derivation failed:",
            perr.message
          );
        }
      }
    }

    // Validate critical fields are present and appear numeric/arrays as required.
    // We allow proceeding if totalArea is numeric even when walls are unavailable.
    if (
      !dimensions ||
      ((dimensions.totalArea === undefined ||
        isNaN(Number(dimensions.totalArea))) &&
        (!Array.isArray(dimensions.walls) || dimensions.walls.length === 0))
    ) {
      throw new Error("DimensionsUnavailable: Missing critical dimensions");
    }

    return dimensions;
  } catch (error) {
    console.error("❌ [PHASE 4] Dimension extraction error:", error);
    throw new Error("DimensionsUnavailable: Dimension extraction failed");
  }
}

// Calculate quantity for a specific material
function calculateMaterialQuantity(materialItem, dimensions, analysis) {
  const materialName = materialItem.name.toLowerCase();
  const category = materialItem.category.toLowerCase();

  console.log(
    `🔍 [PHASE 4] Calculating quantity for ${materialName} (${category})`
  );

  // Define material specifications (typical South African construction)
  const materialSpecs = {
    // Bricks and masonry - assume brick for external walls
    brick: { width: 0.22, height: 0.07, depth: 0.11, unit: "m" },
    bricks: { width: 0.22, height: 0.07, depth: 0.11, unit: "m" },
    masonry: { width: 0.22, height: 0.07, depth: 0.11, unit: "m" },
    block: { width: 0.39, height: 0.19, depth: 0.19, unit: "m" },
    blocks: { width: 0.39, height: 0.19, depth: 0.19, unit: "m" },
    "concrete block": { width: 0.39, height: 0.19, depth: 0.19, unit: "m" },

    // Concrete - per cubic meter
    concrete: { volume: 1, unit: "m³" },
    cement: { volume: 1, unit: "m³" },

    // Steel - per meter
    steel: { length: 1, unit: "m" },
    rebar: { length: 1, unit: "m" },
    reinforcement: { length: 1, unit: "m" },

    // Roofing - per tile
    roofing: { width: 0.33, height: 0.42, unit: "m" },
    tiles: { width: 0.33, height: 0.42, unit: "m" },
    "roof tiles": { width: 0.33, height: 0.42, unit: "m" },

    // Windows and doors - count units
    window: { count: 1, unit: "ea" },
    windows: { count: 1, unit: "ea" },
    door: { count: 1, unit: "ea" },
    doors: { count: 1, unit: "ea" },

    // Flooring - per tile
    flooring: { width: 0.3, height: 0.3, unit: "m" },
    "floor tiles": { width: 0.3, height: 0.3, unit: "m" },

    // Paint - coverage per liter
    paint: { coverage: 10, unit: "m²/liter" },

    // Insulation - per square meter
    insulation: { area: 1, unit: "m²" },

    // Drywall - per sheet
    drywall: { width: 1.2, height: 2.4, unit: "m" },
    plasterboard: { width: 1.2, height: 2.4, unit: "m" },

    // Electrical - per meter
    electrical: { length: 1, unit: "m" },
    wiring: { length: 1, unit: "m" },

    // Plumbing - per meter
    plumbing: { length: 1, unit: "m" },
    pipes: { length: 1, unit: "m" },

    // Fixtures - count units
    fixtures: { count: 1, unit: "ea" },
    lighting: { count: 1, unit: "ea" },
    hvac: { count: 1, unit: "ea" },

    // Wood materials
    studs: { length: 2.4, unit: "m" },
    "2x4": { length: 2.4, unit: "m" },
    plywood: { width: 1.2, height: 2.4, unit: "m" },
    sheathing: { width: 1.2, height: 2.4, unit: "m" },
    subfloor: { width: 1.2, height: 2.4, unit: "m" },

    // Roofing materials
    shingles: { width: 0.33, height: 1.0, unit: "m" },
    asphalt: { width: 0.33, height: 1.0, unit: "m" },

    // Electrical components
    receptacles: { count: 1, unit: "ea" },
    outlets: { count: 1, unit: "ea" },
    switches: { count: 1, unit: "ea" },
    panel: { count: 1, unit: "ea" },
    service: { count: 1, unit: "ea" },
  };

  // Find matching material specification
  let spec = null;
  for (const [key, value] of Object.entries(materialSpecs)) {
    if (materialName.includes(key)) {
      spec = value;
      break;
    }
  }

  if (!spec) {
    console.log(
      `⚠️ [PHASE 4] No specification found for ${materialName}, returning 0 quantity`
    );
    return { quantity: 0, unitOfQuantity: "units" };
  }

  console.log(`📏 [PHASE 4] Using spec for ${materialName}:`, spec);

  // Calculate quantity based on material type and dimensions
  let quantity = 0;
  let calculationDetails = [];

  if (
    category.includes("foundation") ||
    materialName.includes("concrete") ||
    materialName.includes("cement")
  ) {
    // Foundation materials - calculate based on foundation area and thickness
    const foundationArea = parseFloat(
      dimensions.foundation?.area || dimensions.totalArea || "100"
    );
    const thickness = parseFloat(dimensions.foundation?.thickness || "0.15");
    quantity = Math.ceil(foundationArea * thickness);
    calculationDetails.push(
      `Foundation area: ${foundationArea}m² × thickness: ${thickness}m = ${quantity}m³`
    );
  } else if (
    category.includes("exterior") ||
    materialName.includes("brick") ||
    materialName.includes("block") ||
    materialName.includes("masonry")
  ) {
    // Wall materials - calculate based on wall dimensions (assume brick for external walls)
    const walls = dimensions.walls || [];
    let totalWallArea = 0;

    walls.forEach((wall) => {
      const length = parseFloat(wall.length || "0");
      const height = parseFloat(wall.height || "2.4");
      const area = length * height;
      totalWallArea += area;
      calculationDetails.push(
        `Wall ${wall.name}: ${length}m × ${height}m = ${area}m²`
      );
    });

    if (totalWallArea > 0) {
      // Calculate number of bricks/blocks needed
      const brickArea = spec.width * spec.height;
      quantity = Math.ceil(totalWallArea / brickArea);
      calculationDetails.push(
        `Total wall area: ${totalWallArea}m² ÷ brick area: ${brickArea}m² = ${quantity} bricks`
      );
    } else {
      // Fallback calculation
      const estimatedArea = parseFloat(dimensions.totalArea || "100") * 2.5; // Assume 2.5x floor area for walls
      const brickArea = spec.width * spec.height;
      quantity = Math.ceil(estimatedArea / brickArea);
      calculationDetails.push(
        `Estimated wall area: ${estimatedArea}m² ÷ brick area: ${brickArea}m² = ${quantity} bricks`
      );
    }
  } else if (
    category.includes("roofing") ||
    materialName.includes("roof") ||
    materialName.includes("tile") ||
    materialName.includes("shingle")
  ) {
    // Roofing materials
    const roofArea = parseFloat(
      dimensions.roof?.area || dimensions.totalArea || "100"
    );
    if (spec.width && spec.height) {
      const tileArea = spec.width * spec.height;
      quantity = Math.ceil(roofArea / tileArea);
      calculationDetails.push(
        `Roof area: ${roofArea}m² ÷ tile area: ${tileArea}m² = ${quantity} tiles`
      );
    } else {
      quantity = Math.ceil(roofArea);
      calculationDetails.push(`Roof area: ${roofArea}m² = ${quantity} units`);
    }
  } else if (
    materialName.includes("plywood") ||
    materialName.includes("sheathing") ||
    materialName.includes("subfloor")
  ) {
    // Plywood sheets - calculate based on area
    const area = parseFloat(dimensions.totalArea || "100");
    if (spec.width && spec.height) {
      const sheetArea = spec.width * spec.height;
      quantity = Math.ceil(area / sheetArea);
      calculationDetails.push(
        `Area: ${area}m² ÷ sheet area: ${sheetArea}m² = ${quantity} sheets`
      );
    } else {
      quantity = Math.ceil(area);
      calculationDetails.push(`Area: ${area}m² = ${quantity} units`);
    }
  } else if (category.includes("flooring") || materialName.includes("floor")) {
    // Flooring materials
    const floorArea = parseFloat(dimensions.totalArea || "100");
    if (spec.width && spec.height) {
      const tileArea = spec.width * spec.height;
      quantity = Math.ceil(floorArea / tileArea);
      calculationDetails.push(
        `Floor area: ${floorArea}m² ÷ tile area: ${tileArea}m² = ${quantity} tiles`
      );
    } else {
      quantity = Math.ceil(floorArea);
      calculationDetails.push(`Floor area: ${floorArea}m² = ${quantity} units`);
    }
  } else if (spec.count) {
    // Count-based materials (windows, doors, fixtures)
    if (materialName.includes("window") || materialName.includes("door")) {
      const openings = dimensions.openings || [];
      const targetType = materialName.includes("window") ? "window" : "door";
      const matchingOpenings = openings.filter(
        (opening) => opening.type === targetType
      );
      quantity = matchingOpenings.reduce(
        (sum, opening) => sum + parseInt(opening.quantity || "1"),
        0
      );
      calculationDetails.push(
        `Found ${matchingOpenings.length} ${targetType} openings = ${quantity} units`
      );
    } else if (
      materialName.includes("receptacle") ||
      materialName.includes("outlet")
    ) {
      // Electrical receptacles - estimate based on area (1 per 10m²)
      const area = parseFloat(dimensions.totalArea || "100");
      quantity = Math.ceil(area / 10);
      calculationDetails.push(
        `Estimated receptacles based on area: ${area}m² ÷ 10m²/outlet = ${quantity} outlets`
      );
    } else if (
      materialName.includes("panel") ||
      materialName.includes("service")
    ) {
      // Electrical panels - typically 1 per building
      quantity = 1;
      calculationDetails.push(
        `Electrical panel: 1 per building = ${quantity} panel`
      );
    } else {
      // Other count-based materials - estimate based on area
      const area = parseFloat(dimensions.totalArea || "100");
      quantity = Math.ceil(area / 50); // Rough estimate: 1 unit per 50m²
      calculationDetails.push(
        `Estimated count based on area: ${area}m² ÷ 50m²/unit = ${quantity} units`
      );
    }
  } else if (spec.length) {
    // Length-based materials (electrical, plumbing, wood studs)
    if (materialName.includes("stud") || materialName.includes("2x4")) {
      // Wood studs - calculate based on wall perimeter
      const walls = dimensions.walls || [];
      let totalPerimeter = 0;
      walls.forEach((wall) => {
        totalPerimeter += parseFloat(wall.length || "0");
      });
      if (totalPerimeter === 0) {
        totalPerimeter = parseFloat(dimensions.foundation?.perimeter || "36");
      }
      // Studs every 0.6m (600mm centers)
      quantity = Math.ceil(totalPerimeter / 0.6);
      calculationDetails.push(
        `Wall perimeter: ${totalPerimeter}m ÷ 0.6m spacing = ${quantity} studs`
      );
    } else {
      // Electrical, plumbing - estimate based on area
      const area = parseFloat(dimensions.totalArea || "100");
      quantity = Math.ceil(area / 2);
      calculationDetails.push(
        `Estimated length based on area: ${area}m² ÷ 2m²/m = ${quantity}m`
      );
    }
  } else if (materialName.includes("paint")) {
    // Paint - calculate based on wall area
    const walls = dimensions.walls || [];
    let totalWallArea = 0;

    walls.forEach((wall) => {
      const length = parseFloat(wall.length || "0");
      const height = parseFloat(wall.height || "2.4");
      totalWallArea += length * height;
    });

    if (totalWallArea === 0) {
      totalWallArea = parseFloat(dimensions.totalArea || "100") * 2.5; // Estimate
    }

    quantity = Math.ceil(totalWallArea / spec.coverage);
    calculationDetails.push(
      `Wall area: ${totalWallArea}m² ÷ coverage: ${spec.coverage}m²/liter = ${quantity} liters`
    );
  } else {
    // Default calculation based on total area
    const area = parseFloat(dimensions.totalArea || "100");
    if (spec.area) {
      quantity = Math.ceil(area / spec.area);
      calculationDetails.push(
        `Total area: ${area}m² ÷ unit area: ${spec.area}m² = ${quantity} units`
      );
    } else {
      quantity = Math.ceil(area);
      calculationDetails.push(`Total area: ${area}m² = ${quantity} units`);
    }
  }

  // Determine unit of quantity based on material type
  // Before determining unit, normalize quantity to avoid NaN/negatives
  if (!Number.isFinite(quantity)) {
    calculationDetails.push(
      "Insufficient dimensional data to compute quantity; defaulting to 0"
    );
    quantity = 0;
  }
  if (quantity < 0) quantity = 0;
  if (quantity > 0) {
    quantity = Math.ceil(quantity);
  }

  let unitOfQuantity = "units";
  if (spec.volume) {
    unitOfQuantity = "m³";
  } else if (spec.area) {
    unitOfQuantity = "m²";
  } else if (spec.length) {
    unitOfQuantity = "m";
  } else if (spec.count) {
    unitOfQuantity = "ea";
  } else if (spec.coverage) {
    unitOfQuantity = "liters";
  } else if (materialName.includes("brick") || materialName.includes("block")) {
    unitOfQuantity = "bricks";
  } else if (
    materialName.includes("tile") ||
    materialName.includes("shingle")
  ) {
    unitOfQuantity = "tiles";
  } else if (
    materialName.includes("sheet") ||
    materialName.includes("plywood")
  ) {
    unitOfQuantity = "sheets";
  } else if (materialName.includes("stud") || materialName.includes("2x4")) {
    unitOfQuantity = "studs";
  } else if (materialName.includes("paint")) {
    unitOfQuantity = "liters";
  } else if (
    materialName.includes("concrete") ||
    materialName.includes("cement")
  ) {
    unitOfQuantity = "m³";
  } else if (materialName.includes("steel") || materialName.includes("rebar")) {
    unitOfQuantity = " m";
  }

  console.log(
    `📊 [PHASE 4] ${materialName} calculation details:`,
    calculationDetails
  );
  console.log(
    `✅ [PHASE 4] ${materialName} final quantity: ${quantity} ${unitOfQuantity}`
  );

  return { quantity, unitOfQuantity };
}

// Get default dimensions if extraction fails
function getDefaultDimensions(analysis, projectContext) {
  const squareFootage = projectContext.squareFootage || 2500;
  const area = Math.round(squareFootage * 0.0929); // Convert sq ft to sq m

  // Calculate wall dimensions based on area (assume roughly square building)
  const sideLength = Math.sqrt(area);
  const wallLength1 = Math.round(sideLength);
  const wallLength2 = Math.round(area / sideLength);

  return {
    scale: "1:100",
    totalArea: area.toString(),
    walls: [
      {
        name: "External Wall 1",
        length: wallLength1.toString(),
        height: "2.4",
        thickness: "22",
        material: "brick",
      },
      {
        name: "External Wall 2",
        length: wallLength1.toString(),
        height: "2.4",
        thickness: "22",
        material: "brick",
      },
      {
        name: "External Wall 3",
        length: wallLength2.toString(),
        height: "2.4",
        thickness: "22",
        material: "brick",
      },
      {
        name: "External Wall 4",
        length: wallLength2.toString(),
        height: "2.4",
        thickness: "22",
        material: "brick",
      },
    ],
    floors: [{ name: "Ground Floor", area: area.toString(), thickness: "15" }],
    roof: {
      area: area.toString(),
      pitch: "30",
      material: "tiles",
    },
    openings: [
      { type: "window", width: "120", height: "120", quantity: "6" },
      { type: "door", width: "90", height: "210", quantity: "3" },
    ],
    foundation: {
      area: area.toString(),
      thickness: "0.15",
      perimeter: Math.round(2 * (wallLength1 + wallLength2)).toString(),
    },
  };
}

// Phase 5: Holistic Coverage Enhancement Agent
async function enhanceHolisticCoverage(lineItems, analysis, projectContext) {
  try {
    // Start with existing line items
    let enhancedItems = [...lineItems];

    // Add missing categories based on blueprint analysis
    const existingCategories = new Set(lineItems.map((item) => item.category));
    const existingItemNames = new Set(
      lineItems.map((item) => item.name?.toLowerCase().trim())
    );

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

    // Add site preparation if not present (check for specific items)
    if (
      !existingCategories.has("Site Preparation") ||
      (!existingItemNames.has("site preparation and earthwork") &&
        !existingItemNames.has("temporary utilities and facilities"))
    ) {
      const sitePrepItems = await generateSitePreparationItems(
        analysis,
        projectContext
      );
      // Only add items that don't already exist
      const newSitePrepItems = sitePrepItems.filter(
        (item) => !existingItemNames.has(item.name?.toLowerCase().trim())
      );
      if (newSitePrepItems.length > 0) {
        enhancedItems.push(...newSitePrepItems);
      }
    }

    // Add project overhead if not present (check for specific items)
    if (
      !existingCategories.has("Project Overhead") ||
      (!existingItemNames.has("project management and supervision") &&
        !existingItemNames.has("permits and inspections"))
    ) {
      const overheadItems = await generateProjectOverheadItems(
        analysis,
        projectContext
      );
      // Only add items that don't already exist
      const newOverheadItems = overheadItems.filter(
        (item) => !existingItemNames.has(item.name?.toLowerCase().trim())
      );
      if (newOverheadItems.length > 0) {
        enhancedItems.push(...newOverheadItems);
      }
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
      (sum, item) => sum + (item.itemType === "General" ? item.lineTotal : 0),
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

  // Handle null or undefined text
  if (!text || typeof text !== "string") {
    console.warn(
      "detectBlueprintTypes: text is null, undefined, or not a string"
    );
    return ["unknown"];
  }

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
  console.log("🔍 [FALLBACK] Using text parser");

  // First try to extract JSON from markdown
  const jsonMatch = textResponse.match(/```(?:json)?\s*(\[[\s\S]*?\])\s*```/);
  if (jsonMatch) {
    try {
      console.log("🔍 [FALLBACK] Found JSON in markdown, attempting to parse");
      const extractedJson = jsonMatch[1];
      const parsedItems = JSON.parse(extractedJson);
      if (Array.isArray(parsedItems)) {
        console.log("✅ [FALLBACK] Successfully parsed JSON from markdown");
        return parsedItems;
      }
    } catch (error) {
      console.log(
        "❌ [FALLBACK] Failed to parse extracted JSON:",
        error.message
      );
    }
  }

  // Strict mode: do not fabricate defaults; return empty to force earlier validation to stop
  return [];
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
    const demoResponse = await generateWithFallback(demoPrompt);
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
        unitPrice: 15000, // ZAR
        aiConfidence: 0.6,
      },
      {
        name: "Waste Disposal and Recycling",
        description: "Removal and disposal of construction waste",
        quantity: 1,
        unit: "ls",
        category: "Demolition",
        unitPrice: 6000, // ZAR
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
      unitPrice: 25000, // ZAR
      aiConfidence: 0.7,
    },
    {
      name: "Temporary Utilities and Facilities",
      description: "Temporary power, water, and sanitation facilities",
      quantity: 1,
      unit: "ls",
      category: "Site Preparation",
      unitPrice: 12000, // ZAR
      aiConfidence: 0.7,
    },
  ];
}

async function generateProjectOverheadItems(analysis, projectContext) {
  const totalValue = analysis.estimatedValue || 500000; // Default ZAR 500,000 if not available
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
  const subtotal = lineItems.reduce((sum, item) => {
    const lineTotal = parseFloat(item.lineTotal) || 0;
    return sum + lineTotal;
  }, 0);

  const contingencyPercentage = 0.1; // 10% contingency
  const weatherPercentage = 0.02; // 2% weather allowance

  return [
    {
      name: "Owner Contingency",
      description: "Owner contingency for unforeseen conditions and changes",
      quantity: Math.max(1, Math.ceil(subtotal * contingencyPercentage)),
      unit: "ea",
      category: "Contingencies",
      unitPrice: 1,
      aiConfidence: 0.9,
    },
    {
      name: "Weather Delay Allowance",
      description: "Allowance for weather-related delays and impacts",
      quantity: Math.max(1, Math.ceil(subtotal * weatherPercentage)),
      unit: "ea",
      category: "Contingencies",
      unitPrice: 1,
      aiConfidence: 0.7,
    },
  ];
}

// Deduplication Function
function deduplicateLineItems(items) {
  const seen = new Set();
  const deduplicated = [];

  for (const item of items) {
    // Create a unique key based on name and category
    const key = `${item.name?.toLowerCase().trim()}_${item.category
      ?.toLowerCase()
      .trim()}`;

    if (!seen.has(key)) {
      seen.add(key);
      deduplicated.push(item);
    } else {
      console.log(
        `🔍 [DEDUP] Removed duplicate: ${item.name} (${item.category})`
      );
    }
  }

  return deduplicated;
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
    return {
      success: false,
      lineItems: [],
      metadata: {
        blueprintTypes: ["unknown"],
        confidence: 0,
        coverage: 0,
        processingTime: Date.now(),
        fallbackUsed: true,
        error: originalError.message,
      },
      summary: {
        totalItems: 0,
        totalValue: 0,
        categories: [],
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
