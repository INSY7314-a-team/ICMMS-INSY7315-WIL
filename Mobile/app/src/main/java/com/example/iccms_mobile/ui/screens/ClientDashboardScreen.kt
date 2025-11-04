package com.example.iccms_mobile.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import com.example.iccms_mobile.data.models.UserInfo
import com.example.iccms_mobile.ui.screens.client.*
import com.example.iccms_mobile.ui.viewmodel.ClientDashboardViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ClientDashboardScreen(
    user: UserInfo,
    onLogout: () -> Unit,
    viewModel: ClientDashboardViewModel = androidx.lifecycle.viewmodel.compose.viewModel(),
    onNavigateToCreateRequest: () -> Unit = {},
    onNavigateToProjectDetails: (String) -> Unit = {},
    onNavigateToQuotationDetails: (String) -> Unit = {},
    onNavigateToInvoiceDetails: (String) -> Unit = {},
    onNavigateToRequestDetails: (String) -> Unit = {}
) {
    var selectedTab by remember { mutableStateOf(0) }
    
    val tabs = listOf(
        TabItem("Dashboard", Icons.Default.Home),
        TabItem("Projects", Icons.Default.Star),
        TabItem("Maintenance", Icons.Default.Build),
        TabItem("Quotations", Icons.Default.List),
        TabItem("Invoices", Icons.Default.Menu)
    )
    
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Client Dashboard") },
                actions = {
                    TextButton(onClick = onLogout) {
                        Text("Logout")
                    }
                }
            )
        },
        bottomBar = {
            NavigationBar(
                modifier = Modifier
                    .clip(RoundedCornerShape(topStart = 16.dp, topEnd = 16.dp)),
                containerColor = MaterialTheme.colorScheme.surface,
                tonalElevation = 8.dp
            ) {
                tabs.forEachIndexed { index, tab ->
                    NavigationBarItem(
                        icon = { 
                            Icon(
                                tab.icon, 
                                contentDescription = tab.title,
                                modifier = Modifier.size(24.dp)
                            ) 
                        },
                        label = { 
                            Text(
                                tab.title,
                                style = MaterialTheme.typography.labelSmall
                            ) 
                        },
                        selected = selectedTab == index,
                        onClick = { selectedTab = index },
                        colors = NavigationBarItemDefaults.colors(
                            selectedIconColor = MaterialTheme.colorScheme.primary,
                            selectedTextColor = MaterialTheme.colorScheme.primary,
                            unselectedIconColor = MaterialTheme.colorScheme.onSurfaceVariant,
                            unselectedTextColor = MaterialTheme.colorScheme.onSurfaceVariant,
                            indicatorColor = MaterialTheme.colorScheme.primaryContainer
                        )
                    )
                }
            }
        }
    ) { paddingValues ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
            when (selectedTab) {
                0 -> ClientDashboardOverviewScreen(
                    user = user,
                    viewModel = viewModel
                )
                1 -> ClientProjectsScreen(
                    viewModel = viewModel,
                    onNavigateToProjectDetails = onNavigateToProjectDetails
                )
                2 -> ClientMaintenanceScreen(
                    viewModel = viewModel,
                    onNavigateToCreateRequest = onNavigateToCreateRequest,
                    onNavigateToRequestDetails = onNavigateToRequestDetails
                )
                3 -> ClientQuotationsScreen(
                    viewModel = viewModel,
                    onNavigateToQuotationDetails = onNavigateToQuotationDetails
                )
                4 -> ClientInvoicesScreen(
                    viewModel = viewModel,
                    onNavigateToInvoiceDetails = onNavigateToInvoiceDetails
                )
            }
        }
    }
}

data class TabItem(
    val title: String,
    val icon: ImageVector
)
