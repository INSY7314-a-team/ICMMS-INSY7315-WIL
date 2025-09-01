package com.example.icmms.data.repository

import com.example.icmms.data.api.ApiClient
import com.example.icmms.data.models.LoginRequest
import com.example.icmms.data.models.LoginResponse
import com.example.icmms.data.models.TokenVerificationRequest
import com.example.icmms.data.models.TokenVerificationResponse
import com.example.icmms.data.models.UserInfo
import retrofit2.Response

class AuthRepository {
    private val authApiService = ApiClient.authApiService
    
    suspend fun login(email: String, password: String): Response<LoginResponse> {
        val request = LoginRequest(email, password)
        return authApiService.login(request)
    }
    
    suspend fun verifyToken(token: String): Response<TokenVerificationResponse> {
        val request = TokenVerificationRequest(token)
        return authApiService.verifyToken(request)
    }
    
    suspend fun getProfile(token: String): Response<UserInfo> {
        return authApiService.getProfile("Bearer $token")
    }
}
