package com.example.icmms.data.models

data class LoginRequest(
    val email: String,
    val password: String
)

data class LoginResponse(
    val success: Boolean,
    val token: String,
    val message: String,
    val user: UserInfo
)

data class UserInfo(
    val userId: String,
    val email: String,
    val fullName: String,
    val role: String
)

data class TokenVerificationRequest(
    val idToken: String
)

data class TokenVerificationResponse(
    val success: Boolean,
    val message: String,
    val user: UserInfo
)
