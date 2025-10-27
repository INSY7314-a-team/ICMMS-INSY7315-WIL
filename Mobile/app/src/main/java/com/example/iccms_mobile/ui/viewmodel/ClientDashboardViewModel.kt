package com.example.iccms_mobile.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.iccms_mobile.data.models.*
import com.example.iccms_mobile.data.repository.ClientsRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class ClientDashboardUiState(
    val isLoading: Boolean = false,
    val projects: List<Project> = emptyList(),
    val quotations: List<Quotation> = emptyList(),
    val invoices: List<Invoice> = emptyList(),
    val maintenanceRequests: List<MaintenanceRequest> = emptyList(),
    val errorMessage: String? = null,
    val successMessage: String? = null
)

class ClientDashboardViewModel : ViewModel() {
    private val clientsRepository = ClientsRepository()
    
    private val _uiState = MutableStateFlow(ClientDashboardUiState())
    val uiState: StateFlow<ClientDashboardUiState> = _uiState.asStateFlow()
    
    fun loadDashboardData() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            try {
                // Load all data in parallel
                val projectsResult = clientsRepository.getProjects()
                val quotationsResult = clientsRepository.getQuotations()
                val invoicesResult = clientsRepository.getInvoices()
                val maintenanceRequestsResult = clientsRepository.getMaintenanceRequests()
                
                val projects = projectsResult.getOrElse { emptyList() }
                val quotations = quotationsResult.getOrElse { emptyList() }
                val invoices = invoicesResult.getOrElse { emptyList() }
                val maintenanceRequests = maintenanceRequestsResult.getOrElse { emptyList() }
                
                // Add mock quotations for testing
                val mockQuotations = quotations + createMockQuotations()
                
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    projects = projects,
                    quotations = mockQuotations,
                    invoices = invoices,
                    maintenanceRequests = maintenanceRequests
                )
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = e.message ?: "Failed to load dashboard data"
                )
            }
        }
    }
    
    fun approveQuotation(quotationId: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            clientsRepository.approveQuotation(quotationId)
                .onSuccess { updatedQuotation ->
                    val updatedQuotations = _uiState.value.quotations.map { quotation ->
                        if (quotation.quotationId == quotationId) updatedQuotation else quotation
                    }
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        quotations = updatedQuotations,
                        successMessage = "Quotation approved successfully"
                    )
                }
                .onFailure { exception ->
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        errorMessage = exception.message ?: "Failed to approve quotation"
                    )
                }
        }
    }
    
    fun rejectQuotation(quotationId: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            clientsRepository.rejectQuotation(quotationId)
                .onSuccess { updatedQuotation ->
                    val updatedQuotations = _uiState.value.quotations.map { quotation ->
                        if (quotation.quotationId == quotationId) updatedQuotation else quotation
                    }
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        quotations = updatedQuotations,
                        successMessage = "Quotation rejected successfully"
                    )
                }
                .onFailure { exception ->
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        errorMessage = exception.message ?: "Failed to reject quotation"
                    )
                }
        }
    }
    
    fun payInvoice(invoiceId: String, payment: Payment) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            clientsRepository.payInvoice(invoiceId, payment)
                .onSuccess { newPayment ->
                    // Update the invoice status in the list
                    val updatedInvoices = _uiState.value.invoices.map { invoice ->
                        if (invoice.invoiceId == invoiceId) {
                            invoice.copy(status = "Paid", paidDate = newPayment.paymentDate)
                        } else {
                            invoice
                        }
                    }
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        invoices = updatedInvoices,
                        successMessage = "Invoice paid successfully"
                    )
                }
                .onFailure { exception ->
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        errorMessage = exception.message ?: "Failed to pay invoice"
                    )
                }
        }
    }
    
    fun createMaintenanceRequest(request: MaintenanceRequest) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            clientsRepository.createMaintenanceRequest(request)
                .onSuccess { newRequest ->
                    val updatedRequests = _uiState.value.maintenanceRequests + newRequest
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        maintenanceRequests = updatedRequests,
                        successMessage = "Maintenance request created successfully"
                    )
                }
                .onFailure { exception ->
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        errorMessage = exception.message ?: "Failed to create maintenance request"
                    )
                }
        }
    }
    
    fun updateMaintenanceRequest(requestId: String, request: MaintenanceRequest) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            clientsRepository.updateMaintenanceRequest(requestId, request)
                .onSuccess {
                    val updatedRequests = _uiState.value.maintenanceRequests.map { req ->
                        if (req.maintenanceRequestId == requestId) request else req
                    }
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        maintenanceRequests = updatedRequests,
                        successMessage = "Maintenance request updated successfully"
                    )
                }
                .onFailure { exception ->
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        errorMessage = exception.message ?: "Failed to update maintenance request"
                    )
                }
        }
    }
    
    fun deleteMaintenanceRequest(requestId: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            clientsRepository.deleteMaintenanceRequest(requestId)
                .onSuccess {
                    val updatedRequests = _uiState.value.maintenanceRequests.filter { req ->
                        req.maintenanceRequestId != requestId
                    }
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        maintenanceRequests = updatedRequests,
                        successMessage = "Maintenance request deleted successfully"
                    )
                }
                .onFailure { exception ->
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        errorMessage = exception.message ?: "Failed to delete maintenance request"
                    )
                }
        }
    }
    
    fun clearMessages() {
        _uiState.value = _uiState.value.copy(errorMessage = null, successMessage = null)
    }
    
    private fun createMockQuotations(): List<Quotation> {
        val currentDate = java.text.SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", java.util.Locale.getDefault()).format(java.util.Date())
        val validUntilDate = java.util.Calendar.getInstance().apply {
            add(java.util.Calendar.DAY_OF_MONTH, 30) // Valid for 30 days
        }.time
        val validUntil = java.text.SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", java.util.Locale.getDefault()).format(validUntilDate)
        
        val approvedDate = java.util.Calendar.getInstance().apply {
            add(java.util.Calendar.DAY_OF_MONTH, -5) // Approved 5 days ago
        }.time
        val approvedAt = java.text.SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", java.util.Locale.getDefault()).format(approvedDate)
        
        return listOf(
            Quotation(
                quotationId = "QUO-2024-001",
                projectId = "PROJ-001",
                maintenanceRequestId = "MR-2024-001",
                clientId = "CLIENT-001",
                contractorId = "CONTRACTOR-001",
                adminApproverUserId = "ADMIN-001",
                description = "Kitchen Renovation - Cabinet Installation and Plumbing Work",
                total = 45000.00,
                status = "Pending",
                validUntil = validUntil,
                createdAt = currentDate,
                sentAt = currentDate,
                approvedAt = null
            ),
            Quotation(
                quotationId = "QUO-2024-002",
                projectId = "PROJ-002",
                maintenanceRequestId = "MR-2024-002",
                clientId = "CLIENT-001",
                contractorId = "CONTRACTOR-002",
                adminApproverUserId = "ADMIN-001",
                description = "Bathroom Remodeling - Tile Work and Fixture Installation",
                total = 32000.00,
                status = "Approved",
                validUntil = validUntil,
                createdAt = currentDate,
                sentAt = currentDate,
                approvedAt = approvedAt
            ),
            Quotation(
                quotationId = "QUO-2024-003",
                projectId = "PROJ-003",
                maintenanceRequestId = "MR-2024-003",
                clientId = "CLIENT-001",
                contractorId = "CONTRACTOR-003",
                adminApproverUserId = "ADMIN-001",
                description = "Roof Repair and Gutter Installation",
                total = 18500.00,
                status = "Pending",
                validUntil = validUntil,
                createdAt = currentDate,
                sentAt = currentDate,
                approvedAt = null
            ),
            Quotation(
                quotationId = "QUO-2024-004",
                projectId = "PROJ-004",
                maintenanceRequestId = "MR-2024-004",
                clientId = "CLIENT-001",
                contractorId = "CONTRACTOR-004",
                adminApproverUserId = "ADMIN-001",
                description = "Electrical Panel Upgrade and Outlet Installation",
                total = 12500.00,
                status = "Rejected",
                validUntil = validUntil,
                createdAt = currentDate,
                sentAt = currentDate,
                approvedAt = null
            )
        )
    }
}
