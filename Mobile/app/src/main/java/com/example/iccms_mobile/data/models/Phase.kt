package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Phase(
    @SerializedName("phaseId")
    val phaseId: String = "",

    @SerializedName("projectId")
    val projectId: String = "",

    @SerializedName("name")
    val name: String = "",

    @SerializedName("description")
    val description: String = "",

    @SerializedName("startDate")
    val startDate: String = "",

    @SerializedName("endDate")
    val endDate: String = "",

    @SerializedName("status")
    val status: String = "",

    @SerializedName("progress")
    val progress: Int = 0,

    @SerializedName("budget")
    val budget: Double = 0.0,

    @SerializedName("assignedTo")
    val assignedTo: String = ""
)
