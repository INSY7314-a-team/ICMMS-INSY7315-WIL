package com.example.icmms.navigation

import androidx.compose.runtime.*
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.example.icmms.ui.screens.AdminScreen
import com.example.icmms.ui.screens.ClientsScreen
import com.example.icmms.ui.screens.ContractorScreen
import com.example.icmms.ui.screens.LoginScreen
import com.example.icmms.ui.screens.ProjectManagerScreen
import com.example.icmms.ui.screens.TesterScreen
import com.example.icmms.ui.viewmodel.AuthViewModel

@Composable
fun AppNavigation(
    navController: NavHostController = rememberNavController(),
    authViewModel: AuthViewModel = viewModel()
) {
    val uiState by authViewModel.uiState.collectAsState()
    
    NavHost(
        navController = navController,
        startDestination = "login"
    ) {
        composable("login") {
            LoginScreen(
                onLoginSuccess = { role ->
                    when (role.lowercase()) {
                        "admin" -> navController.navigate("admin") {
                            popUpTo("login") { inclusive = true }
                        }
                        "project manager" -> navController.navigate("projectmanager") {
                            popUpTo("login") { inclusive = true }
                        }
                        "contractor" -> navController.navigate("contractor") {
                            popUpTo("login") { inclusive = true }
                        }
                        "client" -> navController.navigate("clients") {
                            popUpTo("login") { inclusive = true }
                        }
                        "tester" -> navController.navigate("tester") {
                            popUpTo("login") { inclusive = true }
                        }
                        else -> {
                            // Unknown role - stay on login or show error
                        }
                    }
                },
                authViewModel = authViewModel
            )
        }
        
        composable("admin") {
            AdminScreen(
                onNavigateBack = {
                    authViewModel.logout()
                    navController.navigate("login") {
                        popUpTo("admin") { inclusive = true }
                    }
                }
            )
        }
        
        composable("projectmanager") {
            ProjectManagerScreen(
                onNavigateBack = {
                    authViewModel.logout()
                    navController.navigate("login") {
                        popUpTo("projectmanager") { inclusive = true }
                    }
                }
            )
        }
        
        composable("contractor") {
            ContractorScreen(
                onNavigateBack = {
                    authViewModel.logout()
                    navController.navigate("login") {
                        popUpTo("contractor") { inclusive = true }
                    }
                }
            )
        }
        
        composable("clients") {
            ClientsScreen(
                onNavigateBack = {
                    authViewModel.logout()
                    navController.navigate("login") {
                        popUpTo("clients") { inclusive = true }
                    }
                }
            )
        }
        
        composable("tester") {
            TesterScreen(
                onNavigateBack = {
                    authViewModel.logout()
                    navController.navigate("login") {
                        popUpTo("tester") { inclusive = true }
                    }
                }
            )
        }
    }
}
