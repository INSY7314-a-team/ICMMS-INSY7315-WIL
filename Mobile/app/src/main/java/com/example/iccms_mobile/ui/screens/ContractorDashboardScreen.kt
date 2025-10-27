package com.example.iccms_mobile.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.iccms_mobile.data.models.UserInfo
import com.example.iccms_mobile.ui.screens.contractor.*
import com.example.iccms_mobile.ui.viewmodel.ContractorDashboardViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ContractorDashboardScreen(
    user: UserInfo,
    onLogout: () -> Unit,
    onNavigateToUploadDocument: () -> Unit = {},
    onNavigateToTaskDetails: (String) -> Unit = {},
    onNavigateToPhaseDetails: (String) -> Unit = {},
    onNavigateToDocumentDetails: (String) -> Unit = {}
) {
    var selectedTab by remember { mutableIntStateOf(0) }
    val contractorViewModel = remember { ContractorDashboardViewModel() }
    
    val tabs = listOf(
        TabItem("Overview", Icons.Default.Home),
        TabItem("Tasks", Icons.Default.List),
        TabItem("Phases", Icons.Default.Build),
        TabItem("Documents", Icons.Default.Menu)
    )
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Contractor Dashboard") },
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
                0 -> ContractorOverviewScreen(
                    user = user,
                    viewModel = contractorViewModel
                )
                1 -> ContractorTasksScreen(
                    viewModel = contractorViewModel,
                    onNavigateToTaskDetails = onNavigateToTaskDetails
                )
                2 -> ContractorPhasesScreen(
                    viewModel = contractorViewModel,
                    onNavigateToPhaseDetails = onNavigateToPhaseDetails
                )
                3 -> ContractorDocumentsScreen(
                    viewModel = contractorViewModel,
                    onNavigateToUploadDocument = onNavigateToUploadDocument,
                    onNavigateToDocumentDetails = onNavigateToDocumentDetails
                )
            }
        }
    }
}
