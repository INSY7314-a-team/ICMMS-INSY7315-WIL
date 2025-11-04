package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Document(
    @SerializedName("documentId")
    val documentId: String = "",

    @SerializedName("projectId")
    val projectId: String = "",

    @SerializedName("fileName")
    val fileName: String = "",

    @SerializedName("status")
    val status: String = "active", // default matches API

    @SerializedName("fileType")
    val fileType: String = "",

    @SerializedName("fileSize")
    val fileSize: Long = 0L,

    @SerializedName("fileUrl")
    val fileUrl: String = "",

    @SerializedName("uploadedBy")
    val uploadedBy: String = "",

    @SerializedName("uploadedAt")
    val uploadedAt: String = "",

    @SerializedName("description")
    val description: String = ""
)
