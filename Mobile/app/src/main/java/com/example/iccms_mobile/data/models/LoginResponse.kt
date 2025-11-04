package com.example.iccms_mobile.data.models

data class LoginResponse(
    val success: Boolean,
    val message: String,
    val user: UserInfo
)

data class UserInfo(
    val userId: String,
    val email: String,
    val fullName: String,
    val role: String
)
