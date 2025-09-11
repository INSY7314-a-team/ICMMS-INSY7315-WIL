package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

class Project(
    @SerializedName("projectId")
    val projectId: String = "",
    
    @SerializedName("projectManagerId")
    val projectManagerId: String = "",
    
    @SerializedName("clientId")
    val clientId: String = "",
    
    @SerializedName("name")
    val name: String = "",
    
    @SerializedName("description")
    val description: String = "",
    
    @SerializedName("budgetPlanned")
    val budgetPlanned: Double = 0.0,
    
    @SerializedName("budgetActual")
    val budgetActual: Double = 0.0,
    
    @SerializedName("status")
    val status: String = "",
    
    @SerializedName("startDatePlanned")
    val startDate: String = "",
    
    @SerializedName("endDatePlanned")
    val endDatePlanned: String = "",
    
    @SerializedName("endDateActual")
    val endDateActual: String? = null
)
