package com.example.iccms_mobile.data.repository

import com.example.iccms_mobile.data.api.AuthApiService
import com.example.iccms_mobile.data.models.LoginRequest
import com.example.iccms_mobile.data.models.LoginResponse
import com.example.iccms_mobile.data.network.NetworkModule
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class AuthRepository {
    private val authApiService: AuthApiService = NetworkModule.authApiService
    
    suspend fun login(email: String, password: String): Result<LoginResponse> {
        return withContext(Dispatchers.IO) {
            try {
                val request = LoginRequest(email, password)
                val response = authApiService.login(request)
                
                if (response.isSuccessful && response.body() != null) {
                    Result.success(response.body()!!)
                } else {
                    val errorMessage = response.errorBody()?.string() ?: "Login failed"
                    Result.failure(Exception(errorMessage))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }
    }
}
