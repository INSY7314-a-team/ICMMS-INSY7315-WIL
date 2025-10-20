const validateRequest = (req, res, next) => {
  // Basic request validation
  if (!req.body) {
    return res.status(400).json({
      success: false,
      error: "Request body is required",
    });
  }

  // Check for required headers
  if (!req.headers["content-type"]?.includes("application/json")) {
    return res.status(400).json({
      success: false,
      error: "Content-Type must be application/json",
    });
  }

  next();
};

module.exports = {
  validateRequest,
};
