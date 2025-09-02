package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Quotation(
    @SerializedName("quotationId")
    val quotationId: String = "",
    
    @SerializedName("projectId")
    val projectId: String = "",
    
    @SerializedName("maintenanceRequestId")
    val maintenanceRequestId: String = "",
    
    @SerializedName("clientId")
    val clientId: String = "",
    
    @SerializedName("contractorId")
    val contractorId: String = "",
    
    @SerializedName("adminApproverUserId")
    val adminApproverUserId: String = "",
    
    @SerializedName("description")
    val description: String = "",
    
    @SerializedName("total")
    val total: Double = 0.0,
    
    @SerializedName("status")
    val status: String = "",
    
    @SerializedName("validUntil")
    val validUntil: String = "",
    
    @SerializedName("createdAt")
    val createdAt: String = "",
    
    @SerializedName("sentAt")
    val sentAt: String? = null,
    
    @SerializedName("approvedAt")
    val approvedAt: String? = null
)
