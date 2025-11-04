package com.example.iccms_mobile.data.api

import com.example.iccms_mobile.data.models.*
import retrofit2.Response
import retrofit2.http.*

interface ContractorApiService {

    // ===================== PROJECTS & TASKS =====================

    @GET("api/contractors/project/tasks")
    suspend fun getProjectTasks(): Response<List<ProjectTask>>

    @GET("api/contractors/task/{taskId}")
    suspend fun getTaskDetails(@Path("taskId") taskId: String): Response<ProjectTask>

    @GET("api/contractors/project/phases")
    suspend fun getProjectPhases(): Response<List<Phase>>

    @GET("api/contractors/project/documents")
    suspend fun getProjectDocuments(): Response<List<Document>>

    @GET("api/contractors/project/documents/all")
    suspend fun getAllProjectDocuments(): Response<List<Document>>

    @POST("api/contractors/upload/project/{projectId}/document")
    suspend fun uploadDocument(
        @Path("projectId") projectId: String,
        @Body document: Document
    ): Response<Document>

    @PUT("api/contractors/update/project/task/{id}")
    suspend fun updateProjectTask(
        @Path("id") taskId: String,
        @Body task: ProjectTask
    ): Response<ProjectTask>

    @PUT("api/contractors/update/document/{id}")
    suspend fun updateDocument(
        @Path("id") documentId: String,
        @Body document: Document
    ): Response<Document>

    @DELETE("api/contractors/delete/document/{id}")
    suspend fun deleteDocument(@Path("id") documentId: String): Response<Map<String, String>>


    // ===================== TASK MANAGEMENT =====================
/*
    @GET("api/contractors/tasks/assigned")
    suspend fun getAssignedTasks(
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 20
    ): Response<PaginatedResponse<ContractorTaskDto>>

    @GET("api/contractors/task/{taskId}/progress-reports")
    suspend fun getTaskProgressReports(@Path("taskId") taskId: String): Response<List<ProgressReport>>

    @POST("api/contractors/task/{taskId}/progress-report")
    suspend fun submitProgressReport(
        @Path("taskId") taskId: String,
        @Body report: ProgressReport
    ): Response<ProgressReport>

    @PUT("api/contractors/task/{taskId}/request-completion")
    suspend fun requestCompletion(
        @Path("taskId") taskId: String,
        @Body completionReport: CompletionReport
    ): Response<Map<String, Any>>

    @GET("api/contractors/task/{taskId}/completion-reports")
    suspend fun getTaskCompletionReports(@Path("taskId") taskId: String): Response<List<CompletionReport>>
*/
}

