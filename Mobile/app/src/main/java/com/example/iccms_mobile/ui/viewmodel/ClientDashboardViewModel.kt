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
                
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    projects = projects,
                    quotations = quotations,
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
                        if (quotation.QuotationId == quotationId) updatedQuotation else quotation
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
                        if (quotation.QuotationId == quotationId) updatedQuotation else quotation
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
                        if (invoice.InvoiceId == invoiceId) {
                            invoice.copy(Status = "Paid", PaidDate = newPayment.paymentDate)
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
                        if (req.MaintenanceRequestId == requestId) request else req
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
                        req.MaintenanceRequestId != requestId
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
}
