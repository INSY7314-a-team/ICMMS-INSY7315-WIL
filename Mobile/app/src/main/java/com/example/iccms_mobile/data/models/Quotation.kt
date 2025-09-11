package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Quotation(
    @SerializedName("QuotationId")
    val QuotationId: String = "",
    
    @SerializedName("ProjectId")
    val ProjectId: String = "",
    
    @SerializedName("MaintenanceRequestId")
    val MaintenanceRequestId: String = "",
    
    @SerializedName("ClientId")
    val ClientId: String = "",
    
    @SerializedName("ContractorId")
    val ContractorId: String = "",
    
    @SerializedName("AdminApproverUserId")
    val AdminApproverUserId: String = "",
    
    @SerializedName("Description")
    val Description: String = "",
    
    @SerializedName("Total")
    val Total: Double = 0.0,
    
    @SerializedName("Status")
    val Status: String = "",
    
    @SerializedName("ValidUntil")
    val ValidUntil: String = "",
    
    @SerializedName("CreatedAt")
    val CreatedAt: String = "",
    
    @SerializedName("SentAt")
    val SentAt: String? = null,
    
    @SerializedName("ApprovedAt")
    val ApprovedAt: String? = null
)
