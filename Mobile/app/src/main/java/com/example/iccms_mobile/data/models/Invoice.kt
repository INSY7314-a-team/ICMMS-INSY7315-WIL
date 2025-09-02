package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Invoice(
    @SerializedName("invoiceId")
    val invoiceId: String = "",
    
    @SerializedName("projectId")
    val projectId: String = "",
    
    @SerializedName("clientId")
    val clientId: String = "",
    
    @SerializedName("contractorId")
    val contractorId: String = "",
    
    @SerializedName("invoiceNumber")
    val invoiceNumber: String = "",
    
    @SerializedName("description")
    val description: String = "",
    
    @SerializedName("amount")
    val amount: Double = 0.0,
    
    @SerializedName("taxAmount")
    val taxAmount: Double = 0.0,
    
    @SerializedName("totalAmount")
    val totalAmount: Double = 0.0,
    
    @SerializedName("status")
    val status: String = "",
    
    @SerializedName("dueDate")
    val dueDate: String = "",
    
    @SerializedName("issuedDate")
    val issuedDate: String = "",
    
    @SerializedName("paidDate")
    val paidDate: String? = null,
    
    @SerializedName("paidBy")
    val paidBy: String = ""
)
