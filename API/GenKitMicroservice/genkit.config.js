const { googleAI } = require("@genkit-ai/googleai");

module.exports = {
  plugins: [googleAI()],
  model: "gemini-pro", // Using free tier model
  // Enable development UI
  enableDevUI: true,
  // Configure the development server
  devUI: {
    port: 4000,
    host: "localhost",
  },
};
