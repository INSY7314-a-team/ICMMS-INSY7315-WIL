package com.example.iccms_mobile.data.api

import com.example.iccms_mobile.data.models.LoginResponse
import com.example.iccms_mobile.data.models.TokenVerificationRequest
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

interface AuthApiService {
    // New endpoint for Firebase token-based authentication
    @POST("api/auth/verify-token")
    suspend fun loginWithToken(@Body request: TokenVerificationRequest): Response<LoginResponse>
    
    // Keep the old endpoint for backward compatibility (can be removed later)
    @POST("api/auth/login")
    suspend fun login(@Body request: TokenVerificationRequest): Response<LoginResponse>
}
