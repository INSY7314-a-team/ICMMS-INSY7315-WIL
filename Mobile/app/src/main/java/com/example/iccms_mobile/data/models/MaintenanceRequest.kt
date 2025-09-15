package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class MaintenanceRequest(
    @SerializedName("MaintenanceRequestId")
    val MaintenanceRequestId: String = "",
    
    @SerializedName("ClientId")
    val ClientId: String = "",
    
    @SerializedName("ProjectId")
    val ProjectId: String = "",
    
    @SerializedName("Description")
    val Description: String = "",
    
    @SerializedName("Priority")
    val Priority: String = "",
    
    @SerializedName("Status")
    val Status: String = "",
    
    @SerializedName("MediaUrl")
    val MediaUrl: String = "",
    
    @SerializedName("RequestedBy")
    val RequestedBy: String = "",
    
    @SerializedName("AssignedTo")
    val AssignedTo: String = "",
    
    @SerializedName("CreatedAt")
    val CreatedAt: String = "",
    
    @SerializedName("ResolvedAt")
    val ResolvedAt: String? = null
)
