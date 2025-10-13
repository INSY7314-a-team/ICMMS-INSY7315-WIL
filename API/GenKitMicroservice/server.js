const express = require("express");
const cors = require("cors");
const helmet = require("helmet");
const morgan = require("morgan");
require("dotenv").config();

// Import GenKit development server
const { genkit } = require("genkit");
const { googleAI } = require("@genkit-ai/googleai");

const aiRoutes = require("./routes/ai");
const healthRoutes = require("./routes/health");

const app = express();
const PORT = process.env.PORT || 3001;

// Configure GenKit with development UI
const ai = genkit({
  plugins: [googleAI()],
  model: "gemini-2.5-flash",
  enableDevUI: true,
});

// Middleware
app.use(helmet());
app.use(
  cors({
    origin: process.env.ALLOWED_ORIGINS?.split(",") || [
      "http://localhost:5000",
      "http://localhost:4000",
    ],
    credentials: true,
  })
);
app.use(morgan("combined"));
app.use(express.json({ limit: "10mb" }));
app.use(express.urlencoded({ extended: true }));

// Routes
app.use("/api/ai", aiRoutes);
app.use("/health", healthRoutes);

// Error handling middleware
app.use((err, req, res, next) => {
  console.error(err.stack);
  res.status(500).json({
    success: false,
    error: "Internal server error",
    message:
      process.env.NODE_ENV === "development"
        ? err.message
        : "Something went wrong",
  });
});

// 404 handler
app.use("*", (req, res) => {
  res.status(404).json({
    success: false,
    error: "Route not found",
  });
});

app.listen(PORT, () => {
  console.log(`GenKit Microservice running on port ${PORT}`);
  console.log(`GenKit Dev UI should be available at http://localhost:4000`);
  console.log(`Environment: ${process.env.NODE_ENV || "development"}`);
});
