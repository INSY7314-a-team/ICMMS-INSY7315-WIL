/**
 * Document AI Service for processing blueprints and extracting structured data
 */
const { DocumentProcessorServiceClient } =
  require("@google-cloud/documentai").v1;

// Configuration - can be loaded from config.json or environment variables
const projectId = "819687425765";
const location = "eu"; // Format is 'us' or 'eu'
const processorId = "d80a71033aa0c335"; // Create processor in Cloud Console

// Instantiates a client
// apiEndpoint regions available: eu-documentai.googleapis.com, us-documentai.googleapis.com (Required if using eu based processor)
const client = new DocumentProcessorServiceClient({
  apiEndpoint: "eu-documentai.googleapis.com",
});

/**
 * Extract text from textAnchor
 */
function getTextFromAnchor(textAnchor, documentText) {
  if (
    !textAnchor ||
    !textAnchor.textSegments ||
    textAnchor.textSegments.length === 0
  ) {
    return "";
  }

  // First shard in document doesn't have startIndex property
  const startIndex = textAnchor.textSegments[0].startIndex || 0;
  const endIndex = textAnchor.textSegments[0].endIndex;

  return documentText.substring(startIndex, endIndex);
}

/**
 * Process a document using Google Cloud Document AI
 * @param {Buffer|string} fileData - File data as Buffer or base64 string
 * @param {string} mimeType - MIME type of the file (e.g., 'application/pdf', 'image/png')
 * @returns {Promise<Object>} - Object containing extracted text and entities
 */
async function processDocumentWithAI(fileData, mimeType = "application/pdf") {
  try {
    // The full resource name of the processor
    const name = `projects/${projectId}/locations/${location}/processors/${processorId}`;

    // Convert fileData to Buffer if it's a base64 string
    let fileBuffer;
    if (Buffer.isBuffer(fileData)) {
      fileBuffer = fileData;
    } else if (typeof fileData === "string") {
      // Assume it's base64 encoded
      fileBuffer = Buffer.from(fileData, "base64");
    } else {
      throw new Error(
        "Invalid fileData format. Expected Buffer or base64 string."
      );
    }

    // Convert the image data to base64 encode it
    const encodedImage = fileBuffer.toString("base64");

    const request = {
      name,
      rawDocument: {
        content: encodedImage,
        mimeType: mimeType,
      },
    };

    // Process the document
    const [result] = await client.processDocument(request);
    const { document } = result;

    // Get all of the document text as one big string
    const { text } = document;

    // Extract custom schema fields from entities
    const entitiesByType = {};

    if (document.entities && document.entities.length > 0) {
      for (const entity of document.entities) {
        const type = entity.type;
        if (!entitiesByType[type]) {
          entitiesByType[type] = [];
        }

        // Try to get text from textAnchor first, then fallback to other properties
        let entityText = getTextFromAnchor(entity.textAnchor, text);

        // If textAnchor didn't provide text, try other properties
        if (!entityText) {
          entityText =
            entity.mentionText ||
            entity.textValue ||
            entity.normalizedValue?.text ||
            entity.normalizedValue ||
            "";
        }

        entitiesByType[type].push({
          value: entityText,
          confidence: entity.confidence,
          normalizedValue: entity.normalizedValue,
        });
      }
    }

    return {
      text: text || "",
      entities: entitiesByType,
      pages: document.pages?.length || 0,
      metadata: {
        processorId: processorId,
        location: location,
        mimeType: mimeType,
        entityCount: document.entities?.length || 0,
      },
    };
  } catch (error) {
    console.error("Document AI processing error:", error);
    throw new Error(
      `Failed to process document with Document AI: ${error.message}`
    );
  }
}

module.exports = {
  processDocumentWithAI,
  getTextFromAnchor,
};
