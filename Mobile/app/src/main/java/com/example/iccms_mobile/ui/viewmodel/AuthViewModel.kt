package com.example.iccms_mobile.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.iccms_mobile.data.models.LoginResponse
import com.example.iccms_mobile.data.models.UserInfo
import com.example.iccms_mobile.data.repository.AuthRepository
import com.example.iccms_mobile.data.services.FirebaseAuthService
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class AuthUiState(
    val isLoading: Boolean = false,
    val isLoggedIn: Boolean = false,
    val user: UserInfo? = null,
    val errorMessage: String? = null
)

class AuthViewModel : ViewModel() {
    private val authRepository = AuthRepository()
    private val firebaseAuthService = FirebaseAuthService()
    
    private val _uiState = MutableStateFlow(AuthUiState())
    val uiState: StateFlow<AuthUiState> = _uiState.asStateFlow()
    
    init {
        // Check if user is already signed in with Firebase
        checkCurrentUser()
    }
    
    private fun checkCurrentUser() {
        if (firebaseAuthService.isUserSignedIn()) {
            // User is already signed in, try to get user data from API
            refreshUserData()
        }
    }
    
    private fun refreshUserData() {
        viewModelScope.launch {
            try {
                val token = authRepository.getCurrentUserToken()
                if (token.isSuccess) {
                    // User has valid token, update UI state
                    _uiState.value = _uiState.value.copy(
                        isLoggedIn = true,
                        isLoading = false
                    )
                } else {
                    // Token is invalid, sign out
                    logout()
                }
            } catch (e: Exception) {
                logout()
            }
        }
    }
    
    fun login(email: String, password: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            authRepository.login(email, password)
                .onSuccess { response ->
                    println("DEBUG: Login successful, response: $response")
                    println("DEBUG: User data: ${response.user}")
                    println("DEBUG: User role: ${response.user.role}")
                    println("DEBUG: User role length: ${response.user.role.length}")
                    println("DEBUG: User role bytes: ${response.user.role.toByteArray().contentToString()}")
                    println("DEBUG: User role trimmed: '${response.user.role.trim()}'")
                    
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        isLoggedIn = true,
                        user = response.user
                    )
                    
                    println("DEBUG: UI state updated - isLoggedIn: ${_uiState.value.isLoggedIn}, user: ${_uiState.value.user}")
                }
                .onFailure { exception ->
                    println("DEBUG: Login failed: ${exception.message}")
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        errorMessage = exception.message ?: "Login failed"
                    )
                }
        }
    }
    
    fun logout() {
        viewModelScope.launch {
            authRepository.logout()
            _uiState.value = AuthUiState()
        }
    }
    
    fun clearError() {
        _uiState.value = _uiState.value.copy(errorMessage = null)
    }
}
