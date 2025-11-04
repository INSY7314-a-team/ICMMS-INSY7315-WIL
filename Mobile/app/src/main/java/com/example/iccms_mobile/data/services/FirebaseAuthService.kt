package com.example.iccms_mobile.data.services

import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.auth.FirebaseUser
import com.google.firebase.auth.UserProfileChangeRequest
import kotlinx.coroutines.tasks.await

class FirebaseAuthService {
    private val auth: FirebaseAuth = FirebaseAuth.getInstance()
    
    // Get current user
    fun getCurrentUser(): FirebaseUser? = auth.currentUser
    
    
    // Check if user is signed in
    fun isUserSignedIn(): Boolean = auth.currentUser != null
    
    // Sign in with email and password
    suspend fun signInWithEmailAndPassword(email: String, password: String): Result<FirebaseUser> {
        println("DEBUG: [FirebaseAuthService] Attempting Firebase sign in for: $email")
        println("DEBUG: [FirebaseAuthService] FirebaseApp name: ${auth.app.name}")
        println("DEBUG: [FirebaseAuthService] FirebaseAuth currentUser before login: ${auth.currentUser?.email}")

        return try {
            val result = auth.signInWithEmailAndPassword(email, password).await()
            println("DEBUG: [FirebaseAuthService] Firebase sign-in SUCCESS. UID: ${result.user?.uid}")
            Result.success(result.user!!)
        } catch (e: Exception) {
            println("ERROR: [FirebaseAuthService] Firebase sign-in FAILED.")
            println("ERROR: [FirebaseAuthService] Exception type: ${e::class.simpleName}")
            println("ERROR: [FirebaseAuthService] Exception message: ${e.message}")
            println("ERROR: [FirebaseAuthService] Exception cause: ${e.cause}")
            e.printStackTrace()
            Result.failure(e)
        }
    }

    /* Old code: Remove code:
    suspend fun signInWithEmailAndPassword(email: String, password: String): Result<FirebaseUser> {
        return try {
            val result = auth.signInWithEmailAndPassword(email, password).await()
            Result.success(result.user!!)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    */
    // Sign out
    fun signOut() {
        auth.signOut()
    }
    
    // Get ID token
    suspend fun getIdToken(): Result<String> {
        return try {
            val user = auth.currentUser
            if (user != null) {
                val token = user.getIdToken(false).await()
                Result.success(token.token!!)
            } else {
                Result.failure(Exception("No user signed in"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    // Get user UID
    fun getCurrentUserId(): String? = auth.currentUser?.uid
    
    // Get user email
    fun getCurrentUserEmail(): String? = auth.currentUser?.email
    
    // Get user display name
    fun getCurrentUserDisplayName(): String? = auth.currentUser?.displayName
}
