package com.example.iccms_mobile.ui.screens.client

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.runtime.collectAsState
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import com.example.iccms_mobile.data.models.Invoice
import com.example.iccms_mobile.data.models.Payment
import com.example.iccms_mobile.ui.viewmodel.ClientDashboardViewModel
import java.text.NumberFormat
import java.text.SimpleDateFormat
import java.util.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ClientInvoicesScreen(
    viewModel: ClientDashboardViewModel,
    onNavigateToInvoiceDetails: (String) -> Unit = {}
) {
    val uiState by viewModel.uiState.collectAsState()
    
    // Load invoices data when screen is first displayed
    LaunchedEffect(Unit) {
        viewModel.loadDashboardData()
    }
    
    Scaffold(
        topBar = {
            TopAppBar(
                title = { 
                    Text(
                        "Invoices",
                        style = MaterialTheme.typography.headlineMedium
                    )
                }
            )
        }
    ) { paddingValues ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            // Statistics Cards
            item {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    StatCard(
                        title = "Total Invoices",
                        value = uiState.invoices.size.toString(),
                        color = MaterialTheme.colorScheme.primary,
                        modifier = Modifier.weight(1f)
                    )
                    StatCard(
                        title = "Pending",
                        value = uiState.invoices.count { it.status.lowercase() == "pending" }.toString(),
                        color = MaterialTheme.colorScheme.secondary,
                        modifier = Modifier.weight(1f)
                    )
                    StatCard(
                        title = "Paid",
                        value = uiState.invoices.count { it.status.lowercase() == "paid" }.toString(),
                        color = MaterialTheme.colorScheme.tertiary,
                        modifier = Modifier.weight(1f)
                    )
                }
            }
            
            // Total Amount Card
            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.surfaceVariant
                    ),
                    shape = RoundedCornerShape(25.dp),
                    elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
                ) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(start=25.dp, top=16.dp, bottom=16.dp, end=25.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(
                                text = "Total Outstanding",
                                style = MaterialTheme.typography.titleMedium,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.padding(bottom=2.dp)
                            )
                            Text(
                                text = "R${NumberFormat.getNumberInstance().format(uiState.invoices.filter { it.status.lowercase() == "pending" }.sumOf { it.totalAmount })}",
                                style = MaterialTheme.typography.headlineSmall,
                                fontWeight = FontWeight.Bold
                            )
                        }
                        Text(
                            text = "💳",
                            fontSize = 24.sp
                        )
                    }
                }
            }

            /*
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
                                text = "Total Outstanding",
                                style = MaterialTheme.typography.titleMedium,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                            Text(
                                text = "R ${NumberFormat.getNumberInstance().format(uiState.invoices.filter { it.status.lowercase() == "pending" }.sumOf { it.totalAmount })}",
                                style = MaterialTheme.typography.headlineSmall,
                                fontWeight = FontWeight.Bold
                            )
                        }
                        Text(
                            text = "💳",
                            fontSize = 24.sp
                        )
                    }
                }
            }
            */
            
            // Invoices List
            if (uiState.invoices.isEmpty() && !uiState.isLoading) {
                item {
                    Card(
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Column(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(32.dp),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Text(
                                text = "📄",
                                fontSize = 48.sp
                            )
                            Spacer(modifier = Modifier.height(16.dp))
                            Text(
                                text = "No Invoices Found",
                                style = MaterialTheme.typography.headlineSmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                            Text(
                                text = "Your invoices will appear here once they are generated",
                                style = MaterialTheme.typography.bodyMedium,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    }
                }
            } else {
                items(uiState.invoices) { invoice ->
                    InvoiceCard(
                        invoice = invoice,
                        onClick = { onNavigateToInvoiceDetails(invoice.invoiceId) },
                        onPay = { 
                            val payment = Payment(
                                paymentId = "",
                                invoiceId = invoice.invoiceId,
                                amount = invoice.totalAmount,
                                paymentDate = "",
                                method = "Card",
                                status = "Paid",
                                transactionId = "",
                                notes = "",
                                processedAt = "",
                                projectId = invoice.projectId,
                                clientId = invoice.clientId
                            )
                            viewModel.payInvoice(invoice.invoiceId, payment)
                        }
                    )
                }
            }
        }
    }
    
    // Show loading indicator
    /* Old Code: Remove code

    if (uiState.isLoading) {
        Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = Alignment.Center
        ) {
            CircularProgressIndicator()
        }
    }*/
}


// Invoice Card
@Composable
fun InvoiceCard(
    invoice: Invoice,
    onClick: () -> Unit,
    onPay: () -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 6.dp)
            .shadow(2.dp, shape = RoundedCornerShape(20.dp))
            .border(
                width = 1.5.dp,
                color = if (invoice.status.lowercase() == "paid")
                    Color(0xFF006400)//Color(0xFF2ECC71)
                else if (invoice.status.lowercase() == "pending")
                    MaterialTheme.colorScheme.secondary//Color(0xFFFFC107)
                else if (invoice.status.lowercase() == "overdue")
                    MaterialTheme.colorScheme.error
                else
                    MaterialTheme.colorScheme.outline.copy(alpha = 0.3f),
                shape = RoundedCornerShape(20.dp)
            ),
        onClick = onClick,
        shape = RoundedCornerShape(20.dp),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surface
        )
    ) {
        Box(
            modifier = Modifier
                .background(
                    brush = Brush.verticalGradient(
                        listOf(
                            MaterialTheme.colorScheme.surface,
                            MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.12f)
                        )
                    )
                )
                .padding(18.dp)
        ) {
            Column {
                // Header Row
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.Top
                ) {
                    Column(modifier = Modifier.weight(1f)) {
                        // Invoice title
                        Text(
                            text = invoice.description,
                            style = MaterialTheme.typography.titleLarge.copy(
                                fontWeight = FontWeight.Bold
                            ),
                            color = MaterialTheme.colorScheme.onSurface
                        )
                        // invoice id
                        Text(
                            text = "Invoice #${invoice.invoiceNumber}",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            modifier = Modifier.padding(top = 4.dp)
                        )
                    }
                    // Status chip
                    StatusChip(status = invoice.status)
                }

                Spacer(modifier = Modifier.height(16.dp))

                // Amount + Due Date
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Column {
                        Text(
                            text = "Amount",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                        Spacer(modifier = Modifier.height(4.dp))
                        Text(
                            text = "R${NumberFormat.getNumberInstance().format(invoice.totalAmount)}",
                            style = MaterialTheme.typography.headlineSmall.copy(
                                fontWeight = FontWeight.ExtraBold
                            ),
                            color = when (invoice.status.lowercase()) {
                                "paid" -> Color(0xFF006400)//Color(0xFF2ECC71)
                                "pending" -> MaterialTheme.colorScheme.secondary//Color(0xFFFFC107)
                                "overdue" -> MaterialTheme.colorScheme.error
                                "draft" -> Color(0xFF868686)//MaterialTheme.colorScheme.error
                                else -> MaterialTheme.colorScheme.primary
                            }
                        )
                    }

                    Column(horizontalAlignment = Alignment.End) {
                        Text(
                            text = "Due Date",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                        Spacer(modifier = Modifier.height(4.dp))
                        Text(
                            text = formatDate(invoice.dueDate),
                            style = MaterialTheme.typography.bodyMedium.copy(
                                fontWeight = FontWeight.Medium
                            ),
                            color = MaterialTheme.colorScheme.onSurface
                        )
                    }
                }

                Spacer(modifier = Modifier.height(12.dp))

                Divider(
                    modifier = Modifier
                        .fillMaxWidth()
                        .alpha(0.2f),
                    color = MaterialTheme.colorScheme.outline
                )

                Spacer(modifier = Modifier.height(10.dp))

                // Dates
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "📅 Issued: ${formatDate(invoice.issuedDate)}",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    if (invoice.paidDate != null) {
                        Text(
                            text = "Paid: ${formatDate(invoice.paidDate)}",
                            style = MaterialTheme.typography.bodySmall.copy(
                                fontWeight = FontWeight.Medium
                            ),
                            color = Color(0xFF006400)//Color(0xFF2ECC71)
                        )
                    }
                }

            /* Not required pay button: Remove code
                // Pay Button
                if (invoice.status.lowercase() == "pending") {
                    Spacer(modifier = Modifier.height(18.dp))
                    Button(
                        onClick = onPay,
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(48.dp)
                            .shadow(4.dp, shape = RoundedCornerShape(12.dp)),
                        shape = RoundedCornerShape(12.dp),
                        colors = ButtonDefaults.buttonColors(
                            containerColor = Color(0xFFFFC107),
                            contentColor = Color.Black
                        )
                    ) {
                       /* Icon(
                            imageVector = Icons.Default.Payment,
                            contentDescription = "Pay Now",
                            modifier = Modifier.size(18.dp)
                        )*/
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(
                            text = "Pay Now",
                            fontWeight = FontWeight.Bold
                        )
                    }
                }*/

            }
        }
    }
}

/* Old Invoice Card: Remove code
@Composable
fun InvoiceCard(
    invoice: Invoice,
    onClick: () -> Unit,
    onPay: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        onClick = onClick,
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surface
        )
    ) {
        Column(
            modifier = Modifier.padding(16.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.Top
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = invoice.description,
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.SemiBold
                    )
                    Text(
                        text = "Invoice #${invoice.invoiceNumber}",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.padding(top = 4.dp)
                    )
                }
                StatusChip(status = invoice.status)
            }
            
            Spacer(modifier = Modifier.height(12.dp))
            
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column {
                    Text(
                        text = "Amount",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(
                        text = "R ${NumberFormat.getNumberInstance().format(invoice.totalAmount)}",
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.primary
                    )
                }
                Column {
                    Text(
                        text = "Due Date",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(
                        text = formatDate(invoice.dueDate),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
            
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = 8.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "Issued: ${formatDate(invoice.issuedDate)}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                if (invoice.paidDate != null) {
                    Text(
                        text = "Paid: ${formatDate(invoice.paidDate)}",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.tertiary
                    )
                }
            }
            
            if (invoice.status.lowercase() == "pending") {
                Spacer(modifier = Modifier.height(16.dp))
                Button(
                    onClick = onPay,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.tertiary
                    ),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Pay Now")
                }
            }
        }
    }
}
*/

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
