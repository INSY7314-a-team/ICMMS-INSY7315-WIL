package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Payment(
    @SerializedName("paymentId")
    val paymentId: String = "",
    
    @SerializedName("invoiceId")
    val invoiceId: String = "",
    
    @SerializedName("amount")
    val amount: Double = 0.0,
    
    @SerializedName("paymentDate")
    val paymentDate: String = "",
    
    @SerializedName("method")
    val method: String = "",
    
    @SerializedName("status")
    val status: String = "",
    
    @SerializedName("transactionId")
    val transactionId: String = "",
    
    @SerializedName("notes")
    val notes: String = "",
    
    @SerializedName("processedAt")
    val processedAt: String = "",
    
    @SerializedName("projectId")
    val projectId: String = "",
    
    @SerializedName("clientId")
    val clientId: String = ""
)
