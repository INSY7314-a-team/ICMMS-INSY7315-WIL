package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class Quotation(
    @SerializedName("quotationId")
    val quotationId: String = "",

    @SerializedName("projectId")
    val projectId: String = "",

    @SerializedName("maintenanceRequestId")
    val maintenanceRequestId: String = "",

    @SerializedName("clientId")
    val clientId: String = "",

    @SerializedName("contractorId")
    val contractorId: String = "",

    @SerializedName("adminApproverUserId")
    val adminApproverUserId: String = "",

    @SerializedName("description")
    val description: String = "",

    @SerializedName("total")
    val total: Double = 0.0,

    @SerializedName("totalWithMarkup")
    val totalWithMarkup: Double = 0.0,

    @SerializedName("status")
    val status: String = "",

    @SerializedName("validUntil")
    val validUntil: String = "",

    @SerializedName("createdAt")
    val createdAt: String = "",

    @SerializedName("sentAt")
    val sentAt: String? = null,

    @SerializedName("approvedAt")
    val approvedAt: String? = null,

    @SerializedName("items")
    val items: List<QuotationItem> = emptyList(),

    @SerializedName("subtotal")
    val subtotal: Double = 0.0,

    @SerializedName("taxTotal")
    val taxTotal: Double = 0.0,

    @SerializedName("taxTotalWithMarkup")
    val taxTotalWithMarkup: Double = 0.0,

    @SerializedName("grandTotal")
    val grandTotal: Double = 0.0,

    @SerializedName("markupRate")
    val markupRate: Double = 1.0,

    @SerializedName("currency")
    val currency: String = "ZAR",

    @SerializedName("adminApprovedAt")
    val adminApprovedAt: String? = null,

    @SerializedName("clientRespondedAt")
    val clientRespondedAt: String? = null,

    @SerializedName("clientDecisionNote")
    val clientDecisionNote: String? = null,

    @SerializedName("updatedAt")
    val updatedAt: String = "",

    @SerializedName("isAiGenerated")
    val isAiGenerated: Boolean = false,

    @SerializedName("estimateId")
    val estimateId: String? = null,

    @SerializedName("pmEditedAt")
    val pmEditedAt: String? = null,

    @SerializedName("pmEditNotes")
    val pmEditNotes: String? = null,

    @SerializedName("pmRejectedAt")
    val pmRejectedAt: String? = null,

    @SerializedName("pmRejectReason")
    val pmRejectReason: String? = null
)
