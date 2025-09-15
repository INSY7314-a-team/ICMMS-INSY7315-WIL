package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Project(
    @SerializedName("ProjectId")
    val ProjectId: String = "",
    
    @SerializedName("ProjectManagerId")
    val ProjectManagerId: String = "",
    
    @SerializedName("ClientId")
    val ClientId: String = "",
    
    @SerializedName("Name")
    val Name: String = "",
    
    @SerializedName("Description")
    val Description: String = "",
    
    @SerializedName("BudgetPlanned")
    val BudgetPlanned: Double = 0.0,
    
    @SerializedName("BudgetActual")
    val BudgetActual: Double = 0.0,
    
    @SerializedName("Status")
    val Status: String = "",
    
    @SerializedName("StartDatePlanned")
    val StartDatePlanned: String = "",
    
    @SerializedName("EndDatePlanned")
    val EndDatePlanned: String = "",
    
    @SerializedName("EndDateActual")
    val EndDateActual: String? = null
)
