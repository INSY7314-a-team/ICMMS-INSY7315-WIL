package com.example.iccms_mobile.data.api

import com.example.iccms_mobile.data.models.LoginRequest
import com.example.iccms_mobile.data.models.LoginResponse
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

interface AuthApiService {
    @POST("api/auth/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>
}
