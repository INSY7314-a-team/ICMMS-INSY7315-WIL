package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class MaintenanceRequest(
    @SerializedName("maintenanceRequestId")
    val maintenanceRequestId: String = "",
    
    @SerializedName("clientId")
    val clientId: String = "",
    
    @SerializedName("projectId")
    val projectId: String = "",
    
    @SerializedName("description")
    val description: String = "",
    
    @SerializedName("priority")
    val priority: String = "",
    
    @SerializedName("status")
    val status: String = "",
    
    @SerializedName("mediaUrl")
    val mediaUrl: String = "",
    
    @SerializedName("requestedBy")
    val requestedBy: String = "",
    
    @SerializedName("assignedTo")
    val assignedTo: String = "",
    
    @SerializedName("createdAt")
    val createdAt: String = "",
    
    @SerializedName("resolvedAt")
    val resolvedAt: String? = null
)
