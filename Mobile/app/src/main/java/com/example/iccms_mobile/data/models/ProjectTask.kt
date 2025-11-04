package com.example.iccms_mobile.data.models

import com.google.gson.annotations.SerializedName

data class ProjectTask(
    @SerializedName("taskId")
    val taskId: String = "",

    @SerializedName("projectId")
    val projectId: String = "",

    @SerializedName("phaseId")
    val phaseId: String = "",

    @SerializedName("name")
    val name: String = "",

    @SerializedName("description")
    val description: String = "",

    @SerializedName("assignedTo")
    val assignedTo: String = "",

    @SerializedName("priority")
    val priority: String = "",

    @SerializedName("status")
    val status: String = "",

    @SerializedName("startDate")
    val startDate: String = "",

    @SerializedName("dueDate")
    val dueDate: String = "",

    @SerializedName("completedDate")
    val completedDate: String? = null,

    @SerializedName("progress")
    val progress: Int = 0,

    @SerializedName("estimatedHours")
    val estimatedHours: Double = 0.0,

    @SerializedName("actualHours")
    val actualHours: Double = 0.0,

    @SerializedName("budget")
    val budget: Double = 0.0
)


/*
package com.example.iccms_mobile.data.models


import com.google.gson.annotations.SerializedName

data class ProjectTask(
    @SerializedName("TaskId")
    val TaskId: String = "",
    
    @SerializedName("ProjectId")
    val ProjectId: String = "",
    
    @SerializedName("PhaseId")
    val PhaseId: String = "",
    
    @SerializedName("Name")
    val Name: String = "",
    
    @SerializedName("Description")
    val Description: String = "",
    
    @SerializedName("AssignedTo")
    val AssignedTo: String = "",
    
    @SerializedName("Priority")
    val Priority: String = "",
    
    @SerializedName("Status")
    val Status: String = "",
    
    @SerializedName("StartDate")
    val StartDate: String = "",
    
    @SerializedName("DueDate")
    val DueDate: String = "",
    
    @SerializedName("CompletedDate")
    val CompletedDate: String? = null,
    
    @SerializedName("Progress")
    val Progress: Int = 0,
    
    @SerializedName("EstimatedHours")
    val EstimatedHours: Double = 0.0,
    
    @SerializedName("ActualHours")
    val ActualHours: Double = 0.0
)
*/