const { googleAI } = require("@genkit-ai/googleai");

module.exports = {
  plugins: [googleAI()],
  model: googleAI.model("gemini-2.0-flash"), // Using free tier model
  // Enable development UI
  enableDevUI: true,
  // Configure the development server
  devUI: {
    port: 4000,
    host: "localhost",
  },
};
