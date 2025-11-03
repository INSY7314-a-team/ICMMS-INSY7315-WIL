package com.example.iccms_mobile.ui.navigation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.example.iccms_mobile.ui.screens.ClientDashboardScreen
import com.example.iccms_mobile.ui.screens.ContractorDashboardScreen
import com.example.iccms_mobile.ui.screens.LoginScreen
import com.example.iccms_mobile.ui.screens.client.CreateMaintenanceRequestScreen
import com.example.iccms_mobile.ui.viewmodel.AuthViewModel
import com.example.iccms_mobile.ui.viewmodel.ClientDashboardViewModel

@Composable
fun AppNavigation(
    authViewModel: AuthViewModel,
    modifier: Modifier = Modifier,
    navController: NavHostController = rememberNavController()
) {
    // Helper function to check user role more robustly
    fun checkUserRole(role: String?): String? {
        if (role == null) return null
        
        val cleanRole = role.trim().lowercase().replace(" ", "")
        println("DEBUG: Role checking - Original: '$role', Cleaned: '$cleanRole'")
        
        return when (cleanRole) {
            "client" -> "client"
            "contractor" -> "contractor"
            else -> null
        }
    }
    
    NavHost(
        navController = navController,
        startDestination = "login",
        modifier = modifier
    ) {
        composable("login") {
            val authState by authViewModel.uiState.collectAsState()
            
            LoginScreen(
                authViewModel = authViewModel,
                onLoginSuccess = {
                    // Navigate based on user role - only client and contractor allowed
                    // Use the authViewModel's current state directly to avoid timing issues
                    val currentUser = authViewModel.uiState.value.user
                    val userRole = checkUserRole(currentUser?.role)
                    println("DEBUG: Navigation - User role: ${currentUser?.role}, processed: $userRole")
                    println("DEBUG: Navigation - Full user data: $currentUser")
                    println("DEBUG: Navigation - Role comparison: '$userRole' == 'client' = ${userRole == "client"}")
                    println("DEBUG: Navigation - Role comparison: '$userRole' == 'contractor' = ${userRole == "contractor"}")
                    
                    when (userRole) {
                        "client" -> {
                            println("DEBUG: Navigating to client dashboard")
                            navController.navigate("client_dashboard") {
                                popUpTo("login") { inclusive = true }
                            }
                        }
                        "contractor" -> {
                            println("DEBUG: Navigating to contractor dashboard")
                            navController.navigate("contractor_dashboard") {
                                popUpTo("login") { inclusive = true }
                            }
                        }
                        else -> {
                            // Unknown role or unauthorized role, stay on login
                            println("DEBUG: Unknown role: '$userRole', staying on login")
                            println("DEBUG: Available roles: client, contractor")
                            println("DEBUG: Current user: $currentUser")
                            // This will prevent admin, project manager, and other roles from accessing the app
                        }
                    }
                }
            )
        }
        
        composable("client_dashboard") {
            val authState by authViewModel.uiState.collectAsState()
            
            authState.user?.let { user ->
                ClientDashboardScreen(
                    user = user,
                    onLogout = {
                        authViewModel.logout()
                        navController.navigate("login") {
                            popUpTo("client_dashboard") { inclusive = true }
                        }
                    },
                    onNavigateToCreateRequest = {
                        navController.navigate("create_maintenance_request")
                    }
                )
            } ?: run {
                // User is null, redirect to login
                navController.navigate("login") {
                    popUpTo("client_dashboard") { inclusive = true }
                }
            }
        }
        
        composable("create_maintenance_request") {
            val authState by authViewModel.uiState.collectAsState()
            
            authState.user?.let { user ->
                val clientDashboardViewModel: ClientDashboardViewModel = viewModel()
                
                CreateMaintenanceRequestScreen(
                    viewModel = clientDashboardViewModel,
                    onNavigateBack = {
                        navController.popBackStack()
                    },
                    onRequestCreated = {
                        navController.popBackStack()
                    }
                )
            } ?: run {
                // User is null, redirect to login
                navController.navigate("login") {
                    popUpTo("create_maintenance_request") { inclusive = true }
                }
            }
        }
        
        // Contractor Dashboard
        composable("contractor_dashboard") {
            val authState by authViewModel.uiState.collectAsState()
            
            authState.user?.let { user ->
                ContractorDashboardScreen(
                    user = user,
                    onLogout = {
                        authViewModel.logout()
                        navController.navigate("login") {
                            popUpTo("contractor_dashboard") { inclusive = true }
                        }
                    }
                )
            } ?: run {
                navController.navigate("login") {
                    popUpTo("contractor_dashboard") { inclusive = true }
                }
            }
        }
    }
}
