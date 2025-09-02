package com.example.iccms_mobile.data.models

data class LoginResponse(
    val Success: Boolean,
    val Message: String,
    val User: UserInfo
)

data class UserInfo(
    val UserId: String,
    val Email: String,
    val FullName: String,
    val Role: String
)
