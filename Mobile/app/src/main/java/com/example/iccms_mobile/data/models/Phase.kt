package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Phase(
    @SerializedName("PhaseId")
    val PhaseId: String = "",
    
    @SerializedName("ProjectId")
    val ProjectId: String = "",
    
    @SerializedName("Name")
    val Name: String = "",
    
    @SerializedName("Description")
    val Description: String = "",
    
    @SerializedName("StartDate")
    val StartDate: String = "",
    
    @SerializedName("EndDate")
    val EndDate: String = "",
    
    @SerializedName("Status")
    val Status: String = "",
    
    @SerializedName("Progress")
    val Progress: Int = 0,
    
    @SerializedName("Budget")
    val Budget: Double = 0.0,
    
    @SerializedName("AssignedTo")
    val AssignedTo: String = ""
)
