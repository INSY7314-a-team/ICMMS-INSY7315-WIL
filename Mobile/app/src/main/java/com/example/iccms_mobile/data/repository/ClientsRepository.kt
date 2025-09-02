package com.example.iccms_mobile.data.repository

import com.example.iccms_mobile.data.api.ClientsApiService
import com.example.iccms_mobile.data.models.*
import com.example.iccms_mobile.data.network.NetworkModule
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class ClientsRepository {
    private val apiService: ClientsApiService = NetworkModule.clientsApiService
    
    suspend fun getProjects(): Result<List<Project>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getProjects()
            if (response.isSuccessful) {
                Result.success(response.body() ?: emptyList())
            } else {
                Result.failure(Exception("Failed to fetch projects: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun getProject(projectId: String): Result<Project> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getProject(projectId)
            if (response.isSuccessful) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to fetch project: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun getQuotations(): Result<List<Quotation>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getQuotations()
            if (response.isSuccessful) {
                Result.success(response.body() ?: emptyList())
            } else {
                Result.failure(Exception("Failed to fetch quotations: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun getQuotation(quotationId: String): Result<Quotation> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getQuotation(quotationId)
            if (response.isSuccessful) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to fetch quotation: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun approveQuotation(quotationId: String): Result<Quotation> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.approveQuotation(quotationId)
            if (response.isSuccessful) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to approve quotation: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun rejectQuotation(quotationId: String): Result<Quotation> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.rejectQuotation(quotationId)
            if (response.isSuccessful) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to reject quotation: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun getInvoices(): Result<List<Invoice>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getInvoices()
            if (response.isSuccessful) {
                Result.success(response.body() ?: emptyList())
            } else {
                Result.failure(Exception("Failed to fetch invoices: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun getInvoice(invoiceId: String): Result<Invoice> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getInvoice(invoiceId)
            if (response.isSuccessful) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to fetch invoice: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun payInvoice(invoiceId: String, payment: Payment): Result<Payment> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.payInvoice(invoiceId, payment)
            if (response.isSuccessful) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to pay invoice: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun getMaintenanceRequests(): Result<List<MaintenanceRequest>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getMaintenanceRequests()
            if (response.isSuccessful) {
                Result.success(response.body() ?: emptyList())
            } else {
                Result.failure(Exception("Failed to fetch maintenance requests: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun getMaintenanceRequest(requestId: String): Result<MaintenanceRequest> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getMaintenanceRequest(requestId)
            if (response.isSuccessful) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to fetch maintenance request: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun createMaintenanceRequest(request: MaintenanceRequest): Result<MaintenanceRequest> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.createMaintenanceRequest(request)
            if (response.isSuccessful) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to create maintenance request: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun updateMaintenanceRequest(requestId: String, request: MaintenanceRequest): Result<Map<String, String>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.updateMaintenanceRequest(requestId, request)
            if (response.isSuccessful) {
                Result.success(response.body() ?: emptyMap())
            } else {
                Result.failure(Exception("Failed to update maintenance request: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun deleteMaintenanceRequest(requestId: String): Result<Map<String, String>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.deleteMaintenanceRequest(requestId)
            if (response.isSuccessful) {
                Result.success(response.body() ?: emptyMap())
            } else {
                Result.failure(Exception("Failed to delete maintenance request: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
