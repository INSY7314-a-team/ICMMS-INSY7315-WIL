package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Document(
    @SerializedName("DocumentId")
    val DocumentId: String = "",
    
    @SerializedName("ProjectId")
    val ProjectId: String = "",
    
    @SerializedName("FileName")
    val FileName: String = "",
    
    @SerializedName("Status")
    val Status: String = "",
    
    @SerializedName("FileType")
    val FileType: String = "",
    
    @SerializedName("FileSize")
    val FileSize: Long = 0L,
    
    @SerializedName("FileUrl")
    val FileUrl: String = "",
    
    @SerializedName("UploadedBy")
    val UploadedBy: String = "",
    
    @SerializedName("UploadedAt")
    val UploadedAt: String = "",
    
    @SerializedName("Description")
    val Description: String = ""
)
