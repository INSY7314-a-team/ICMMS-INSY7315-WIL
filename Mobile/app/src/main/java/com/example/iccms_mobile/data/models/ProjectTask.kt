package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class ProjectTask(
    @SerializedName("TaskId")
    val TaskId: String = "",
    
    @SerializedName("ProjectId")
    val ProjectId: String = "",
    
    @SerializedName("PhaseId")
    val PhaseId: String = "",
    
    @SerializedName("Name")
    val Name: String = "",
    
    @SerializedName("Description")
    val Description: String = "",
    
    @SerializedName("AssignedTo")
    val AssignedTo: String = "",
    
    @SerializedName("Priority")
    val Priority: String = "",
    
    @SerializedName("Status")
    val Status: String = "",
    
    @SerializedName("StartDate")
    val StartDate: String = "",
    
    @SerializedName("DueDate")
    val DueDate: String = "",
    
    @SerializedName("CompletedDate")
    val CompletedDate: String? = null,
    
    @SerializedName("Progress")
    val Progress: Int = 0,
    
    @SerializedName("EstimatedHours")
    val EstimatedHours: Double = 0.0,
    
    @SerializedName("ActualHours")
    val ActualHours: Double = 0.0
)
