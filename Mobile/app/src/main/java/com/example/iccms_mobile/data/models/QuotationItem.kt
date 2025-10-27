package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class QuotationItem(
    @SerializedName("itemId")
    val itemId: String = "",

    @SerializedName("name")
    val name: String = "",

    @SerializedName("description")
    val description: String = "",

    @SerializedName("quantity")
    val quantity: Int = 0,

    @SerializedName("unitPrice")
    val unitPrice: Double = 0.0,

    @SerializedName("totalPrice")
    val totalPrice: Double = 0.0
)
