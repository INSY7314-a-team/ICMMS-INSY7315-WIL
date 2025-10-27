package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class QuotationItem(
    @SerializedName("name")
    val name: String = "",

    @SerializedName("quantity")
    val quantity: Double = 0.0,

    @SerializedName("unitPrice")
    val unitPrice: Double = 0.0,

    @SerializedName("taxRate")
    val taxRate: Double = 0.0,

    @SerializedName("lineTotal")
    val lineTotal: Double = 0.0
)
