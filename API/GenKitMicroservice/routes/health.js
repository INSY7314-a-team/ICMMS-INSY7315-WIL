const router = require("express").Router();
const express = require("express");

router.get("/", (req, res) => {
  res.status(200).json({
    message: "Hello :P",
    status: "healthy",
    timestamp: new Date().toISOString(),
  });
});

module.exports = router;
