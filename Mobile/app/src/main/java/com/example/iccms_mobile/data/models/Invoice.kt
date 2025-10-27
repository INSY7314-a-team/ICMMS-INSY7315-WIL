package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Invoice(
    @SerializedName("InvoiceId")
    val InvoiceId: String = "",
    
    @SerializedName("ProjectId")
    val ProjectId: String = "",
    
    @SerializedName("ClientId")
    val ClientId: String = "",
    
    @SerializedName("ContractorId")
    val ContractorId: String = "",
    
    @SerializedName("InvoiceNumber")
    val InvoiceNumber: String = "",
    
    @SerializedName("Description")
    val Description: String = "",
    
    @SerializedName("Amount")
    val Amount: Double = 0.0,
    
    @SerializedName("TaxAmount")
    val TaxAmount: Double = 0.0,
    
    @SerializedName("TotalAmount")
    val TotalAmount: Double = 0.0,
    
    @SerializedName("Status")
    val Status: String = "",
    
    @SerializedName("DueDate")
    val DueDate: String = "",
    
    @SerializedName("IssuedDate")
    val IssuedDate: String = "",
    
    @SerializedName("PaidDate")
    val PaidDate: String? = null,
    
    @SerializedName("PaidBy")
    val PaidBy: String = ""
)
