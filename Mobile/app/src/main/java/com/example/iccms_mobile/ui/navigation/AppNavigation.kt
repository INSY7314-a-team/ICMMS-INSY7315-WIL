package com.example.iccms_mobile.ui.navigation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.example.iccms_mobile.ui.screens.AdminDashboardScreen
import com.example.iccms_mobile.ui.screens.ClientDashboardScreen
import com.example.iccms_mobile.ui.screens.ContractorDashboardScreen
import com.example.iccms_mobile.ui.screens.LoginScreen
import com.example.iccms_mobile.ui.screens.ProjectManagerDashboardScreenNew
import com.example.iccms_mobile.ui.viewmodel.AuthViewModel

@Composable
fun AppNavigation(
    authViewModel: AuthViewModel,
    modifier: Modifier = Modifier,
    navController: NavHostController = rememberNavController()
) {
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
                    // Navigate based on user role
                    when (authState.user?.role?.lowercase()) {
                        "client" -> navController.navigate("client_dashboard") {
                            popUpTo("login") { inclusive = true }
                        }
                        "admin" -> navController.navigate("admin_dashboard") {
                            popUpTo("login") { inclusive = true }
                        }
                        "projectmanager" -> navController.navigate("pm_dashboard") {
                            popUpTo("login") { inclusive = true }
                        }
                        "contractor" -> navController.navigate("contractor_dashboard") {
                            popUpTo("login") { inclusive = true }
                        }
                        else -> {
                            // Unknown role, stay on login or show error
                            navController.navigate("login")
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
                    }
                )
            } ?: run {
                // User is null, redirect to login
                navController.navigate("login") {
                    popUpTo("client_dashboard") { inclusive = true }
                }
            }
        }
        
        // Admin Dashboard
        composable("admin_dashboard") {
            val authState by authViewModel.uiState.collectAsState()
            
            authState.user?.let { user ->
                AdminDashboardScreen(
                    user = user,
                    onLogout = {
                        authViewModel.logout()
                        navController.navigate("login") {
                            popUpTo("admin_dashboard") { inclusive = true }
                        }
                    }
                )
            } ?: run {
                navController.navigate("login") {
                    popUpTo("admin_dashboard") { inclusive = true }
                }
            }
        }
        
        // Project Manager Dashboard
        composable("pm_dashboard") {
            val authState by authViewModel.uiState.collectAsState()
            
            authState.user?.let { user ->
                ProjectManagerDashboardScreenNew(
                    user = user,
                    onLogout = {
                        authViewModel.logout()
                        navController.navigate("login") {
                            popUpTo("pm_dashboard") { inclusive = true }
                        }
                    }
                )
            } ?: run {
                navController.navigate("login") {
                    popUpTo("pm_dashboard") { inclusive = true }
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
