package com.example.iccms_mobile.data.repository

import com.example.iccms_mobile.data.api.AuthApiService
import com.example.iccms_mobile.data.models.LoginResponse
import com.example.iccms_mobile.data.models.TokenVerificationRequest
import com.example.iccms_mobile.data.network.NetworkModule
import com.example.iccms_mobile.data.services.FirebaseAuthService
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class AuthRepository {
    private val authApiService: AuthApiService = NetworkModule.authApiService
    private val firebaseAuthService = FirebaseAuthService()
    
    suspend fun login(email: String, password: String): Result<LoginResponse> {
        return withContext(Dispatchers.IO) {
            try {
                // Step 1: Authenticate with Firebase
                val firebaseResult = firebaseAuthService.signInWithEmailAndPassword(email, password)
                if (firebaseResult.isFailure) {
                    return@withContext Result.failure(firebaseResult.exceptionOrNull() ?: Exception("Firebase authentication failed"))
                }
                
                // Step 2: Get Firebase ID token
                val tokenResult = firebaseAuthService.getIdToken()
                if (tokenResult.isFailure) {
                    return@withContext Result.failure(tokenResult.exceptionOrNull() ?: Exception("Failed to get Firebase token"))
                }
                
                val firebaseToken = tokenResult.getOrNull()!!
                
                // Step 3: Create request object and call API with Firebase token to get user data
                val tokenRequest = TokenVerificationRequest(IdToken = firebaseToken)
                println("DEBUG: Calling API with token request: $tokenRequest")
                val response = authApiService.loginWithToken(tokenRequest)
                
                println("DEBUG: API response received - isSuccessful: ${response.isSuccessful}")
                println("DEBUG: API response body: ${response.body()}")
                println("DEBUG: API response body class: ${response.body()?.javaClass?.simpleName}")
                println("DEBUG: API error body: ${response.errorBody()?.string()}")
                
                if (response.isSuccessful && response.body() != null) {
                    val responseBody = response.body()!!
                    println("DEBUG: Response body Success: ${responseBody.Success}")
                    println("DEBUG: Response body Message: ${responseBody.Message}")
                    println("DEBUG: Response body User: ${responseBody.User}")
                    println("DEBUG: Response body User Role: ${responseBody.User?.Role}")
                    Result.success(responseBody)
                } else {
                    val errorMessage = response.errorBody()?.string() ?: "Login failed"
                    Result.failure(Exception(errorMessage))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }
    }
    
    suspend fun logout() {
        firebaseAuthService.signOut()
    }
    
    suspend fun getCurrentUserToken(): Result<String> {
        return firebaseAuthService.getIdToken()
    }
    
    fun isUserSignedIn(): Boolean {
        return firebaseAuthService.isUserSignedIn()
    }
}
