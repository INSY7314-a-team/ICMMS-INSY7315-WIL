package com.example.iccms_mobile.data.network

import com.example.iccms_mobile.data.api.AuthApiService
import com.example.iccms_mobile.data.api.ClientsApiService
import com.example.iccms_mobile.data.services.FirebaseAuthService
import kotlinx.coroutines.runBlocking
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.security.cert.X509Certificate
import java.util.concurrent.TimeUnit
import javax.net.ssl.SSLContext
import javax.net.ssl.TrustManager
import javax.net.ssl.X509TrustManager

object NetworkModule {
    // Use HTTPS for production, HTTP for development
    private const val BASE_URL = "http://10.0.2.2:5031" // Commented out by Denzel but was this: "https://10.0.2.2:7136/" // Use HTTPS port for secure communication
    
    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.BODY
    }
    
    // Content type interceptor to ensure proper headers for JSON requests
    private val contentTypeInterceptor = Interceptor { chain ->
        val originalRequest = chain.request()
        println("DEBUG: Making request to: ${originalRequest.url}")
        println("DEBUG: Request method: ${originalRequest.method}")
        println("DEBUG: Request headers: ${originalRequest.headers}")
        
        val newRequest = originalRequest.newBuilder()
            .header("Content-Type", "application/json")
            .header("Accept", "application/json")
            .build()
        
        println("DEBUG: Modified request headers: ${newRequest.headers}")
        
        val response = chain.proceed(newRequest)
        println("DEBUG: Response received - code: ${response.code}, message: ${response.message}")
        println("DEBUG: Response headers: ${response.headers}")
        
        response
    }
    
    // Authentication interceptor to add Firebase ID token to requests
    private val authInterceptor = Interceptor { chain ->
        val originalRequest = chain.request()
        val firebaseAuthService = FirebaseAuthService()
        
        // Check if this is an auth endpoint (don't add token to auth calls to avoid circular dependency)
        val isAuthEndpoint = originalRequest.url.encodedPath.contains("/auth/")
        
        if (!isAuthEndpoint && firebaseAuthService.isUserSignedIn()) {
            // Add Firebase ID token to the request
            val token = runCatching { 
                runBlocking { firebaseAuthService.getIdToken() }
            }.getOrNull()?.getOrNull()
            
            if (!token.isNullOrEmpty()) {
                val newRequest = originalRequest.newBuilder()
                    .header("Authorization", "Bearer $token")
                    .build()
                return@Interceptor chain.proceed(newRequest)
            }
        }
        
        chain.proceed(originalRequest)
    }
    
    // Create a trust manager that accepts all certificates (for development only)
    private val trustAllCerts = arrayOf<TrustManager>(object : X509TrustManager {
        override fun checkClientTrusted(chain: Array<X509Certificate>, authType: String) {}
        override fun checkServerTrusted(chain: Array<X509Certificate>, authType: String) {}
        override fun getAcceptedIssuers(): Array<X509Certificate> = arrayOf()
    })

    // Create SSL context that accepts all certificates (for development only)
    private val sslContext = SSLContext.getInstance("SSL").apply {
        init(null, trustAllCerts, java.security.SecureRandom())
    }

    private val okHttpClient = OkHttpClient.Builder()
        .addInterceptor(loggingInterceptor)
        .addInterceptor(contentTypeInterceptor) // Add content type interceptor first
        .addInterceptor(authInterceptor) // Add authentication interceptor
        .sslSocketFactory(sslContext.socketFactory, trustAllCerts[0] as X509TrustManager)
        .hostnameVerifier { _, _ -> true } // Accept all hostnames (for development only)
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .writeTimeout(30, TimeUnit.SECONDS)
        .build()
    
    private val retrofit = Retrofit.Builder()
        .baseUrl(BASE_URL)
        .client(okHttpClient)
        .addConverterFactory(GsonConverterFactory.create())
        .build()
    
    val authApiService: AuthApiService = retrofit.create(AuthApiService::class.java)
    val clientsApiService: ClientsApiService = retrofit.create(ClientsApiService::class.java)
}
