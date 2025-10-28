package com.example.iccms_mobile.ui.screens.client

import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.runtime.collectAsState
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import com.example.iccms_mobile.data.models.UserInfo
import com.example.iccms_mobile.ui.viewmodel.ClientDashboardViewModel
import java.text.NumberFormat

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ClientDashboardOverviewScreen(
    user: UserInfo,
    viewModel: ClientDashboardViewModel
) {
    val uiState by viewModel.uiState.collectAsState()
    
    // Load data when screen is first displayed
    LaunchedEffect(Unit) {
        viewModel.loadDashboardData()
    }
    
    Scaffold(
       /* topBar = {
            TopAppBar(
                title = { 
                    Text(
                        "Dashboard",
                        style = MaterialTheme.typography.headlineMedium
                    )
                }
            )
        }*/
    ) { paddingValues ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            // Welcome Section
            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.primaryContainer
                    ),
                    elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
                ) {
                    Column(
                        modifier = Modifier.padding(20.dp),
                        horizontalAlignment = Alignment.Start
                    ) {
                        Text(
                            text = "👤   CLIENT DASHBOARD",
                            style = MaterialTheme.typography.headlineSmall,
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.onPrimaryContainer
                        )
                        Text(
                            text = "Welcome back, ${user.fullName}!",
                            style = MaterialTheme.typography.titleMedium,
                            color = MaterialTheme.colorScheme.onPrimaryContainer,
                            modifier = Modifier.padding(top = 10.dp)
                        )
                        Text(
                            text = "Role: ${user.role}",
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onPrimaryContainer,
                            modifier = Modifier.padding(top = 6.dp)
                        )
                    }
                }
            }
            
            // Quick Stats
            item {
                Text(
                    text = "Quick Overview",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.SemiBold,
                    color = MaterialTheme.colorScheme.onSurfaceVariant, // light grey from theme
                    modifier = Modifier.padding(vertical = 8.dp)
                )
                /*Text(
                    text = "Quick Overview",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.padding(vertical = 8.dp)
                )*/
            }
            
            item {
                LazyRow(
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    item {
                        StatCard(
                            title = "Projects",
                            value = uiState.projects.size.toString(),
                            color = MaterialTheme.colorScheme.primary
                        )
                    }
                    item {
                        StatCard(
                            title = "Active Projects",
                            value = uiState.projects.count { it.status.lowercase() == "active" }.toString(),
                            color = MaterialTheme.colorScheme.tertiary
                        )
                    }
                    item {
                        StatCard(
                            title = "Pending Quotes",
                            value = uiState.quotations.count { it.status.lowercase() == "pending" }.toString(),
                            color = MaterialTheme.colorScheme.secondary
                        )
                    }
                    item {
                        StatCard(
                            title = "Maintenance",
                            value = uiState.maintenanceRequests.size.toString(),
                            color = MaterialTheme.colorScheme.primary
                        )
                    }
                }
            }
            
            // Financial Overview
            // Financial Overview
            item {
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 12.dp),

                    shape = RoundedCornerShape(20.dp),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.surfaceVariant
                    ),
                    elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .background(
                                brush = Brush.linearGradient(
                                    colors = listOf(
                                        MaterialTheme.colorScheme.primary.copy(alpha = 0.05f),
                                        MaterialTheme.colorScheme.secondary.copy(alpha = 0.05f)
                                    ),
                                    start = Offset(0f, 0f),
                                    end = Offset.Infinite
                                )
                            )
                            .padding(24.dp),

                    ) {
                        // Header
                        Text(
                            text = "💰   Latest Project Financial Overview",
                            style = MaterialTheme.typography.titleLarge,
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.onSurface,
                            modifier = Modifier.padding(bottom = 20.dp),
                        )

                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            // Total Budget
                            Column {
                                Text(
                                    text = "Total Budget",
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                                Text(
                                    text = "R${NumberFormat.getNumberInstance().format(uiState.projects.sumOf { it.budgetPlanned.toDouble() })}",
                                    style = MaterialTheme.typography.titleLarge,
                                    fontWeight = FontWeight.Bold,
                                    color = MaterialTheme.colorScheme.onSurface//primary
                                )
                            }

                            // Outstanding
                            Column {
                                Text(
                                    text = "Outstanding",
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                                Text(
                                    text = "R${NumberFormat.getNumberInstance().format(uiState.invoices.filter { it.status.lowercase() == "pending" }.sumOf { it.totalAmount })}",
                                    style = MaterialTheme.typography.titleLarge,
                                    fontWeight = FontWeight.Bold,
                                    color = MaterialTheme.colorScheme.error
                                )
                            }
                        }

                       // Spacer(modifier = Modifier.height(16.dp))

                        /* Remove Code
                        // Optional subtle footer for context
                        Text(
                            text = "This overview gives you a snapshot of your latest project finances.",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )*/

                    }
                }
            }

            /* Old Financial Overview Code: Remove code in final
            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.surfaceVariant
                    )
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp)
                    ) {
                        Text(
                            text = "Latest Project Financial Overview",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.SemiBold,
                            modifier = Modifier.padding(bottom = 12.dp)
                        )
                        
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Column {
                                Text(
                                    text = "Total Budget",
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                                Text(
                                    text = "R ${NumberFormat.getNumberInstance().format(uiState.projects.sumOf { it.budgetPlanned.toDouble() })}",
                                    style = MaterialTheme.typography.titleLarge,
                                    fontWeight = FontWeight.Bold
                                )
                            }
                            Column {
                                Text(
                                    text = "Outstanding",
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                                Text(
                                    text = "R ${NumberFormat.getNumberInstance().format(uiState.invoices.filter { it.status.lowercase() == "pending" }.sumOf { it.totalAmount })}",
                                    style = MaterialTheme.typography.titleLarge,
                                    fontWeight = FontWeight.Bold,
                                    color = MaterialTheme.colorScheme.error
                                )
                            }
                        }
                    }
                }
            }
            */
            // Recent Activity
            item {
                Text(
                    text = "Recent Activity",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.SemiBold,
                    color = MaterialTheme.colorScheme.onSurfaceVariant, // light grey from theme
                    modifier = Modifier.padding(vertical = 8.dp)
                )

                /* Old Recent Activity: Remove code
                Text(

                    text = "Recent Activity",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.padding(vertical = 8.dp)
                )
                */
            }
            
            // Recent Projects
            // Recent Projects
            if (uiState.projects.isNotEmpty()) {
                item {
                    Card(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(vertical = 12.dp),
                           /* .shadow(
                                elevation = 12.dp,
                                shape = RoundedCornerShape(20.dp),
                                ambientColor = MaterialTheme.colorScheme.primary.copy(alpha = 0.1f),
                                spotColor = MaterialTheme.colorScheme.primary.copy(alpha = 0.15f)
                            ),*/
                        shape = RoundedCornerShape(20.dp),
                        colors = CardDefaults.cardColors(
                            containerColor = MaterialTheme.colorScheme.surface
                        ),
                        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
                    ) {
                        Column(
                            modifier = Modifier
                                .padding(20.dp)
                                .fillMaxWidth()
                        ) {
                            // Header
                            Text(
                                text = "📁   Recent Projects",
                                style = MaterialTheme.typography.titleLarge,
                                fontWeight = FontWeight.Bold,
                                color = MaterialTheme.colorScheme.onSurface,
                                modifier = Modifier.padding(bottom = 16.dp)
                            )

                            uiState.projects.take(3).forEachIndexed { index, project ->
                                Row(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(vertical = 8.dp)
                                        .background(
                                            brush = Brush.horizontalGradient(
                                                listOf(
                                                    MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.15f),
                                                    MaterialTheme.colorScheme.surface
                                                )
                                            ),
                                            shape = RoundedCornerShape(14.dp)
                                        )
                                        .padding(horizontal = 16.dp, vertical = 12.dp),
                                    horizontalArrangement = Arrangement.SpaceBetween,
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Column(
                                        modifier = Modifier.weight(1f)
                                    ) {
                                        Text(
                                            text = project.name,
                                            style = MaterialTheme.typography.bodyLarge,
                                            fontWeight = FontWeight.SemiBold,
                                            color = MaterialTheme.colorScheme.onSurface
                                        )
                                        Text(
                                            text = project.status,
                                            style = MaterialTheme.typography.bodySmall,
                                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                                            modifier = Modifier.padding(top = 2.dp)
                                        )
                                    }

                                    Spacer(modifier = Modifier.width(12.dp))

                                    Text(
                                        text = "R${NumberFormat.getNumberInstance().format(project.budgetPlanned)}",
                                        style = MaterialTheme.typography.bodyMedium,
                                        fontWeight = FontWeight.Medium,
                                        color = Color(0xFF2E7D32) // Dark green
                                    )

                                }

                                if (index < uiState.projects.take(3).lastIndex) {
                                    Divider(
                                        modifier = Modifier.padding(vertical = 10.dp),
                                        color = MaterialTheme.colorScheme.outlineVariant.copy(alpha = 0.3f)
                                    )
                                }

                            }
                        }
                    }
                }
            }

            /* Old Recent Projects Section: Remove code in final
            if (uiState.projects.isNotEmpty()) {
                item {
                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        // Card colour
                        colors = CardDefaults.cardColors(
                            containerColor = MaterialTheme.colorScheme.surface
                        ),


                    ) {
                        Column(
                            modifier = Modifier.padding(16.dp)
                        ) {
                            Text(
                                text = "Recent Projects",
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.SemiBold,
                                modifier = Modifier.padding(bottom = 12.dp)
                            )
                            
                            uiState.projects.take(3).forEach { project ->
                                Row(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(vertical = 4.dp),
                                    horizontalArrangement = Arrangement.SpaceBetween,
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Column(modifier = Modifier.weight(1f)) {
                                        Text(
                                            text = project.name,
                                            style = MaterialTheme.typography.bodyMedium,
                                            fontWeight = FontWeight.Medium
                                        )
                                        Text(
                                            text = project.status,
                                            style = MaterialTheme.typography.bodySmall,
                                            color = MaterialTheme.colorScheme.onSurfaceVariant
                                        )
                                    }
                                    Text(
                                        text = "R ${NumberFormat.getNumberInstance().format(project.budgetPlanned)}",
                                        style = MaterialTheme.typography.bodySmall,
                                        fontWeight = FontWeight.Medium
                                    )
                                }
                                if (project != uiState.projects.take(3).last()) {
                                    Divider(modifier = Modifier.padding(vertical = 8.dp))
                                }
                            }
                        }
                    }
                }
            }
            */

            // Recent Maintenance Requests
            if (uiState.maintenanceRequests.isNotEmpty()) {
                item {
                    Card(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(vertical = 12.dp),

                        shape = RoundedCornerShape(20.dp),
                        colors = CardDefaults.cardColors(
                            containerColor = MaterialTheme.colorScheme.surface
                        ),
                        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
                    ) {
                        Column(
                            modifier = Modifier
                                .padding(20.dp)
                                .fillMaxWidth()
                        ) {
                            // Header
                            Text(
                                text = "🛠️   Recent Maintenance Requests",
                                style = MaterialTheme.typography.titleLarge,
                                fontWeight = FontWeight.Bold,
                                color = MaterialTheme.colorScheme.onSurface,
                                modifier = Modifier.padding(bottom = 16.dp)
                            )

                            uiState.maintenanceRequests.take(3).forEachIndexed { index, request ->
                                Row(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(vertical = 8.dp)
                                        .background(
                                            brush = Brush.horizontalGradient(
                                                listOf(
                                                    MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.15f),
                                                    MaterialTheme.colorScheme.surface
                                                )
                                            ),
                                            shape = RoundedCornerShape(14.dp)
                                        )
                                        .padding(horizontal = 16.dp, vertical = 12.dp),
                                    horizontalArrangement = Arrangement.SpaceBetween,
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Column(modifier = Modifier.weight(1f)) {
                                        Text(
                                            text = request.description,
                                            style = MaterialTheme.typography.bodyLarge,
                                            fontWeight = FontWeight.SemiBold,
                                            color = MaterialTheme.colorScheme.onSurface
                                        )
                                        Text(
                                            text = "${request.priority} Priority",
                                            style = MaterialTheme.typography.bodySmall,
                                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                                            modifier = Modifier.padding(top = 6.dp)
                                        )
                                    }

                                    Spacer(modifier = Modifier.width(12.dp))

                                    StatusChip(status = request.status)
                                }

                                if (index < uiState.maintenanceRequests.take(3).lastIndex) {
                                    Divider(
                                        modifier = Modifier.padding(vertical = 10.dp),
                                        color = MaterialTheme.colorScheme.outlineVariant.copy(alpha = 0.3f)
                                    )
                                }
                            }
                        }
                    }
                }
            }

            /* Old Recent maintenenac Requests Section: Remove code in final
            if (uiState.maintenanceRequests.isNotEmpty()) {
                item {
                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        colors = CardDefaults.cardColors(
                            containerColor = MaterialTheme.colorScheme.surface
                        )
                    ) {
                        Column(
                            modifier = Modifier.padding(16.dp)
                        ) {
                            Text(
                                text = "Recent Maintenance Requests",
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.SemiBold,
                                modifier = Modifier.padding(bottom = 12.dp)
                            )
                            
                            uiState.maintenanceRequests.take(3).forEach { request ->
                                Row(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(vertical = 4.dp),
                                    horizontalArrangement = Arrangement.SpaceBetween,
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Column(modifier = Modifier.weight(1f)) {
                                        Text(
                                            text = request.description,
                                            style = MaterialTheme.typography.bodyMedium,
                                            fontWeight = FontWeight.Medium
                                        )
                                        Text(
                                            text = "${request.priority} Priority",
                                            style = MaterialTheme.typography.bodySmall,
                                            color = MaterialTheme.colorScheme.onSurfaceVariant
                                        )
                                    }
                                    StatusChip(status = request.status)
                                }
                                if (request != uiState.maintenanceRequests.take(3).last()) {
                                    Divider(modifier = Modifier.padding(vertical = 8.dp))
                                }
                            }
                        }
                    }
                }
            }
             */

        }
    }
    
    // Show loading indicator
    if (uiState.isLoading) {
        Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = Alignment.Center
        ) {
            CircularProgressIndicator()
        }
    }
}
