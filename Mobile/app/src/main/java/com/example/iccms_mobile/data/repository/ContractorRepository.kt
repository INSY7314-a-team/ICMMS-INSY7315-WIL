package com.example.iccms_mobile.data.repository

import com.example.iccms_mobile.data.api.ContractorApiService
import com.example.iccms_mobile.data.models.*
import com.example.iccms_mobile.data.network.NetworkModule
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class ContractorRepository {
    private val apiService: ContractorApiService = NetworkModule.contractorApiService

    // ===================== PROJECTS & TASKS =====================

    suspend fun getProjectTasks(): Result<List<ProjectTask>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getProjectTasks()
            if (response.isSuccessful) {
                Result.success(response.body() ?: emptyList())
            } else {
                Result.failure(Exception("Failed to fetch project tasks: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun getTaskDetails(taskId: String): Result<ProjectTask> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getTaskDetails(taskId)
            if (response.isSuccessful) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to fetch task details: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun getProjectPhases(): Result<List<Phase>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getProjectPhases()
            if (response.isSuccessful) {
                Result.success(response.body() ?: emptyList())
            } else {
                Result.failure(Exception("Failed to fetch project phases: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun getProjectDocuments(): Result<List<Document>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getProjectDocuments()
            if (response.isSuccessful) {
                Result.success(response.body() ?: emptyList())
            } else {
                Result.failure(Exception("Failed to fetch project documents: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun getAllProjectDocuments(): Result<List<Document>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getAllProjectDocuments()
            if (response.isSuccessful) {
                Result.success(response.body() ?: emptyList())
            } else {
                Result.failure(Exception("Failed to fetch project documents: ${response.message()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun uploadDocument(projectId: String, document: Document): Result<Document> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.uploadDocument(projectId, document)
                if (response.isSuccessful) {
                    Result.success(response.body()!!)
                } else {
                    Result.failure(Exception("Failed to upload document: ${response.message()}"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    suspend fun updateProjectTask(taskId: String, task: ProjectTask): Result<ProjectTask> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.updateProjectTask(taskId, task)
                if (response.isSuccessful) {
                    Result.success(response.body()!!)
                } else {
                    Result.failure(Exception("Failed to update project task: ${response.message()}"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    suspend fun updateDocument(documentId: String, document: Document): Result<Document> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.updateDocument(documentId, document)
                if (response.isSuccessful) {
                    Result.success(response.body()!!)
                } else {
                    Result.failure(Exception("Failed to update document: ${response.message()}"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    suspend fun deleteDocument(documentId: String): Result<Map<String, String>> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.deleteDocument(documentId)
                if (response.isSuccessful) {
                    Result.success(response.body() ?: emptyMap())
                } else {
                    Result.failure(Exception("Failed to delete document: ${response.message()}"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    // ===================== TASK MANAGEMENT =====================
    /*
    suspend fun getAssignedTasks(page: Int = 1, pageSize: Int = 20): Result<PaginatedResponse<ContractorTaskDto>> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.getAssignedTasks(page, pageSize)
                if (response.isSuccessful) {
                    Result.success(response.body()!!)
                } else {
                    Result.failure(Exception("Failed to fetch assigned tasks: ${response.message()}"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    suspend fun getTaskProgressReports(taskId: String): Result<List<ProgressReport>> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.getTaskProgressReports(taskId)
                if (response.isSuccessful) {
                    Result.success(response.body() ?: emptyList())
                } else {
                    Result.failure(Exception("Failed to fetch task progress reports: ${response.message()}"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    suspend fun submitProgressReport(taskId: String, report: ProgressReport): Result<ProgressReport> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.submitProgressReport(taskId, report)
                if (response.isSuccessful) {
                    Result.success(response.body()!!)
                } else {
                    Result.failure(Exception("Failed to submit progress report: ${response.message()}"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    suspend fun requestCompletion(taskId: String, completionReport: CompletionReport): Result<Map<String, Any>> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.requestCompletion(taskId, completionReport)
                if (response.isSuccessful) {
                    Result.success(response.body() ?: emptyMap())
                } else {
                    Result.failure(Exception("Failed to request completion: ${response.message()}"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    suspend fun getTaskCompletionReports(taskId: String): Result<List<CompletionReport>> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.getTaskCompletionReports(taskId)
                if (response.isSuccessful) {
                    Result.success(response.body() ?: emptyList())
                } else {
                    Result.failure(Exception("Failed to fetch task completion reports: ${response.message()}"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }
    */
}
