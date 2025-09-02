package com.example.iccms_mobile.data.api

import com.example.iccms_mobile.data.models.*
import retrofit2.Response
import retrofit2.http.*

interface ClientsApiService {
    
    // Projects
    @GET("api/clients/projects")
    suspend fun getProjects(): Response<List<Project>>
    
    @GET("api/clients/project/{id}")
    suspend fun getProject(@Path("id") projectId: String): Response<Project>
    
    // Quotations
    @GET("api/clients/quotations")
    suspend fun getQuotations(): Response<List<Quotation>>
    
    @GET("api/clients/quotation/{id}")
    suspend fun getQuotation(@Path("id") quotationId: String): Response<Quotation>
    
    @PUT("api/clients/approve/quotation/{id}")
    suspend fun approveQuotation(@Path("id") quotationId: String): Response<Quotation>
    
    @PUT("api/clients/reject/quotation/{id}")
    suspend fun rejectQuotation(@Path("id") quotationId: String): Response<Quotation>
    
    // Invoices
    @GET("api/clients/invoices")
    suspend fun getInvoices(): Response<List<Invoice>>
    
    @GET("api/clients/invoice/{id}")
    suspend fun getInvoice(@Path("id") invoiceId: String): Response<Invoice>
    
    @POST("api/clients/pay/invoice/{id}")
    suspend fun payInvoice(@Path("id") invoiceId: String, @Body payment: Payment): Response<Payment>
    
    // Maintenance Requests
    @GET("api/clients/maintenanceRequests")
    suspend fun getMaintenanceRequests(): Response<List<MaintenanceRequest>>
    
    @GET("api/clients/maintenanceRequest/{id}")
    suspend fun getMaintenanceRequest(@Path("id") requestId: String): Response<MaintenanceRequest>
    
    @POST("api/clients/create/maintenanceRequest")
    suspend fun createMaintenanceRequest(@Body request: MaintenanceRequest): Response<MaintenanceRequest>
    
    @PUT("api/clients/update/maintenanceRequest/{id}")
    suspend fun updateMaintenanceRequest(@Path("id") requestId: String, @Body request: MaintenanceRequest): Response<Map<String, String>>
    
    @DELETE("api/clients/delete/maintenanceRequest/{id}")
    suspend fun deleteMaintenanceRequest(@Path("id") requestId: String): Response<Map<String, String>>
}
