package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Project(
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

    @SerializedName("isDraft")
    val isDraft: Boolean = false,

    @SerializedName("startDatePlanned")
    val startDatePlanned: String = "",

    @SerializedName("endDatePlanned")
    val endDatePlanned: String = "",

    @SerializedName("endDateActual")
    val endDateActual: String? = null,

    @SerializedName("createdByUserId")
    val createdByUserId: String = "",

    @SerializedName("updatedAt")
    val updatedAt: String = ""
)
