package com.example.iccms_mobile.ui.screens

import android.util.Log
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.runtime.collectAsState
import com.example.iccms_mobile.data.models.*
import com.example.iccms_mobile.ui.viewmodel.ClientDashboardViewModel
import java.text.NumberFormat
import java.text.SimpleDateFormat
import java.util.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ClientDashboardScreen(
    user: UserInfo,
    onLogout: () -> Unit,
    viewModel: ClientDashboardViewModel = androidx.lifecycle.viewmodel.compose.viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    
    // Load data when screen is first displayed
    LaunchedEffect(Unit) {
        viewModel.loadDashboardData()
    }
    
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
        }
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
                    )
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(
                            text = "👤 CLIENT DASHBOARD",
                            fontSize = 20.sp,
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.onPrimaryContainer
                        )
                        Text(
                            text = "Role: ${user.Role}",
                            fontSize = 16.sp,
                            color = MaterialTheme.colorScheme.onPrimaryContainer
                        )
                        Text(
                            text = "Welcome, ${user.FullName}!",
                            fontSize = 14.sp,
                            color = MaterialTheme.colorScheme.onPrimaryContainer
                        )
                    }
                }
            }
            
            // Statistics Cards
            item {
                LazyRow(
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    item {
                        StatCard(
                            title = "Total Projects",
                            value = uiState.projects.size.toString(),
                            color = MaterialTheme.colorScheme.primary
                        )
                    }
                    item {
                        StatCard(
                            title = "Active Projects",
                            value = uiState.projects.count { it.Status.lowercase() == "active" }.toString(),
                            color = MaterialTheme.colorScheme.tertiary
                        )
                    }
                    item {
                        StatCard(
                            title = "Pending Quotes",
                            value = uiState.quotations.count { it.Status.lowercase() == "pending" }.toString(),
                            color = MaterialTheme.colorScheme.secondary
                        )
                    }
                    item {
                        StatCard(
                            title = "Maintenance",
                            value = uiState.maintenanceRequests.size.toString(),
                            color = Color(0xFFFF9800)
                        )
                    }
                }
            }
            
            // Total Budget Card
            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.surfaceVariant
                    )
                ) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(
                                text = "Total Budget",
                                fontSize = 14.sp,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                            Text(
                                text = "R ${NumberFormat.getNumberInstance().format(uiState.projects.sumOf { it.BudgetPlanned.toDouble() })}",
                                fontSize = 20.sp,
                                fontWeight = FontWeight.Bold
                            )
                        }
                        Text(
                            text = "💰",
                            fontSize = 24.sp
                        )
                    }
                }
            }
            
            // Projects Section
            item {
                ProjectsSection(
                    projects = uiState.projects,
                    isLoading = uiState.isLoading
                )
            }
            
            // Quotations Section
            item {
                QuotationsSection(
                    quotations = uiState.quotations,
                    isLoading = uiState.isLoading,
                    onApprove = { viewModel.approveQuotation(it) },
                    onReject = { viewModel.rejectQuotation(it) }
                )
            }
            
            // Maintenance Requests Section
            item {
                MaintenanceRequestsSection(
                    requests = uiState.maintenanceRequests,
                    isLoading = uiState.isLoading,
                    onCreateRequest = { /* TODO: Navigate to create request */ },
                    onDeleteRequest = { viewModel.deleteMaintenanceRequest(it) }
                )
            }
            
            // Invoices Section
            item {
                InvoicesSection(
                    invoices = uiState.invoices,
                    isLoading = uiState.isLoading,
                    onPayInvoice = { invoiceId, payment -> viewModel.payInvoice(invoiceId, payment) }
                )
            }
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
    
    // Show error message
    uiState.errorMessage?.let { error ->
        LaunchedEffect(error) {
            // You could show a snackbar here
            viewModel.clearMessages()
        }
    }
    
    // Show success message
    uiState.successMessage?.let { success ->
        LaunchedEffect(success) {
            // You could show a snackbar here
            viewModel.clearMessages()
        }
    }
}

@Composable
fun StatCard(
    title: String,
    value: String,
    color: Color
) {
    Card(
        modifier = Modifier.width(120.dp),
        colors = CardDefaults.cardColors(containerColor = color)
    ) {
        Column(
            modifier = Modifier.padding(12.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                text = value,
                fontSize = 24.sp,
                fontWeight = FontWeight.Bold,
                color = Color.White
            )
            Text(
                text = title,
                fontSize = 12.sp,
                color = Color.White,
                textAlign = TextAlign.Center
            )
        }
    }
}

@Composable
fun ProjectsSection(
    projects: List<Project>,
    isLoading: Boolean
) {
    Card(
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(
            modifier = Modifier.padding(16.dp)
        ) {
            Text(
                text = "My Projects",
                fontSize = 18.sp,
                fontWeight = FontWeight.SemiBold,
                modifier = Modifier.padding(bottom = 12.dp)
            )
            
            if (projects.isEmpty() && !isLoading) {
                Text(
                    text = "No projects found",
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(16.dp)
                )
            } else {
                projects.forEach { project ->
                    ProjectItem(project = project)
                    Log.d("ProjectItem", "Project: ${project.Name}")
                    if (project != projects.last()) {
                        Divider(modifier = Modifier.padding(vertical = 8.dp))
                    }
                }
            }
        }
    }
}

@Composable
fun ProjectItem(project: Project) {
    Column {
        Text(
            text = project.Name,
            fontSize = 16.sp,
            fontWeight = FontWeight.Medium
        )
        Text(
            text = project.Description,
            fontSize = 14.sp,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(
                text = "Status: ${project.Status}",
                fontSize = 12.sp,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Text(
                text = "R ${NumberFormat.getNumberInstance().format(project.BudgetPlanned)}",
                fontSize = 12.sp,
                fontWeight = FontWeight.Medium
            )
        }
    }
}

@Composable
fun QuotationsSection(
    quotations: List<Quotation>,
    isLoading: Boolean,
    onApprove: (String) -> Unit,
    onReject: (String) -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(
            modifier = Modifier.padding(16.dp)
        ) {
            Text(
                text = "Quotations",
                fontSize = 18.sp,
                fontWeight = FontWeight.SemiBold,
                modifier = Modifier.padding(bottom = 12.dp)
            )
            
            if (quotations.isEmpty() && !isLoading) {
                Text(
                    text = "No quotations found",
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(16.dp)
                )
            } else {
                quotations.forEach { quotation ->
                    QuotationItem(
                        quotation = quotation,
                        onApprove = { onApprove(quotation.QuotationId) },
                        onReject = { onReject(quotation.QuotationId) }
                    )
                    if (quotation != quotations.last()) {
                        Divider(modifier = Modifier.padding(vertical = 8.dp))
                    }
                }
            }
        }
    }
}

@Composable
fun QuotationItem(
    quotation: Quotation,
    onApprove: () -> Unit,
    onReject: () -> Unit
) {
    Column {
        Text(
            text = quotation.Description,
            fontSize = 16.sp,
            fontWeight = FontWeight.Medium
        )
        Text(
            text = "R ${NumberFormat.getNumberInstance().format(quotation.Total)}",
            fontSize = 14.sp,
            fontWeight = FontWeight.Medium,
            color = MaterialTheme.colorScheme.primary
        )
        Text(
            text = "Status: ${quotation.Status}",
            fontSize = 12.sp,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        
        if (quotation.Status.lowercase() == "pending") {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Button(
                    onClick = onApprove,
                    modifier = Modifier.weight(1f),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.tertiary
                    )
                ) {
                    Text("Approve", fontSize = 12.sp)
                }
                Button(
                    onClick = onReject,
                    modifier = Modifier.weight(1f),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.error
                    )
                ) {
                    Text("Reject", fontSize = 12.sp)
                }
            }
        }
    }
}

@Composable
fun MaintenanceRequestsSection(
    requests: List<MaintenanceRequest>,
    isLoading: Boolean,
    onCreateRequest: () -> Unit,
    onDeleteRequest: (String) -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(
            modifier = Modifier.padding(16.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "Maintenance Requests",
                    fontSize = 18.sp,
                    fontWeight = FontWeight.SemiBold
                )
                Button(
                    onClick = onCreateRequest,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = Color(0xFFFF9800)
                    )
                ) {
                    Text("New Request", fontSize = 12.sp)
                }
            }
            
            if (requests.isEmpty() && !isLoading) {
                Text(
                    text = "No maintenance requests found",
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(16.dp)
                )
            } else {
                requests.forEach { request ->
                    MaintenanceRequestItem(
                        request = request,
                        onDelete = { onDeleteRequest(request.MaintenanceRequestId) }
                    )
                    if (request != requests.last()) {
                        Divider(modifier = Modifier.padding(vertical = 8.dp))
                    }
                }
            }
        }
    }
}

@Composable
fun MaintenanceRequestItem(
    request: MaintenanceRequest,
    onDelete: () -> Unit
) {
    Column {
        Text(
            text = request.Description,
            fontSize = 16.sp,
            fontWeight = FontWeight.Medium
        )
        Text(
            text = "Priority: ${request.Priority}",
            fontSize = 12.sp,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Text(
            text = "Status: ${request.Status}",
            fontSize = 12.sp,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Text(
            text = "Created: ${formatDate(request.CreatedAt)}",
            fontSize = 12.sp,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        
        if (request.Status.lowercase() == "pending") {
            Button(
                onClick = onDelete,
                colors = ButtonDefaults.buttonColors(
                    containerColor = MaterialTheme.colorScheme.error
                ),
                modifier = Modifier.padding(top = 8.dp)
            ) {
                Text("Delete", fontSize = 12.sp)
            }
        }
    }
}

@Composable
fun InvoicesSection(
    invoices: List<Invoice>,
    isLoading: Boolean,
    onPayInvoice: (String, Payment) -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(
            modifier = Modifier.padding(16.dp)
        ) {
            Text(
                text = "Invoices",
                fontSize = 18.sp,
                fontWeight = FontWeight.SemiBold,
                modifier = Modifier.padding(bottom = 12.dp)
            )
            
            if (invoices.isEmpty() && !isLoading) {
                Text(
                    text = "No invoices found",
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(16.dp)
                )
            } else {
                invoices.forEach { invoice ->
                    InvoiceItem(
                        invoice = invoice,
                        onPay = { 
                            val payment = Payment(
                                paymentId = "",
                                invoiceId = invoice.InvoiceId,
                                amount = invoice.TotalAmount,
                                paymentDate = "",
                                method = "Card",
                                status = "Paid",
                                transactionId = "",
                                notes = "",
                                processedAt = "",
                                projectId = invoice.ProjectId,
                                clientId = invoice.ClientId
                            )
                            onPayInvoice(invoice.InvoiceId, payment)
                        }
                    )
                    if (invoice != invoices.last()) {
                        Divider(modifier = Modifier.padding(vertical = 8.dp))
                    }
                }
            }
        }
    }
}

@Composable
fun InvoiceItem(
    invoice: Invoice,
    onPay: () -> Unit
) {
    Column {
        Text(
            text = invoice.Description,
            fontSize = 16.sp,
            fontWeight = FontWeight.Medium
        )
        Text(
            text = "R ${NumberFormat.getNumberInstance().format(invoice.TotalAmount)}",
            fontSize = 14.sp,
            fontWeight = FontWeight.Medium,
            color = MaterialTheme.colorScheme.primary
        )
        Text(
            text = "Status: ${invoice.Status}",
            fontSize = 12.sp,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Text(
            text = "Due: ${formatDate(invoice.DueDate)}",
            fontSize = 12.sp,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        
        if (invoice.Status.lowercase() == "pending") {
            Button(
                onClick = onPay,
                colors = ButtonDefaults.buttonColors(
                    containerColor = MaterialTheme.colorScheme.tertiary
                ),
                modifier = Modifier.padding(top = 8.dp)
            ) {
                Text("Pay Now", fontSize = 12.sp)
            }
        }
    }
}

private fun formatDate(dateString: String): String {
    return try {
        val inputFormat = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.getDefault())
        val outputFormat = SimpleDateFormat("MMM dd, yyyy", Locale.getDefault())
        val date = inputFormat.parse(dateString)
        outputFormat.format(date ?: Date())
    } catch (e: Exception) {
        dateString
    }
}