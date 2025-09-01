package com.example.icmms.data.api

import com.example.icmms.data.models.LoginRequest
import com.example.icmms.data.models.LoginResponse
import com.example.icmms.data.models.TokenVerificationRequest
import com.example.icmms.data.models.TokenVerificationResponse
import com.example.icmms.data.models.UserInfo
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST

interface AuthApiService {
    @POST("api/auth/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>
    
    @POST("api/auth/verify-token")
    suspend fun verifyToken(@Body request: TokenVerificationRequest): Response<TokenVerificationResponse>
    
    @GET("api/auth/profile")
    suspend fun getProfile(@Header("Authorization") token: String): Response<UserInfo>
}
