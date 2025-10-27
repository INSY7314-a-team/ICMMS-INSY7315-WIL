package com.example.iccms_mobile.ui.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.iccms_mobile.data.models.*
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.*

data class ContractorDashboardUiState(
    val isLoading: Boolean = false,
    val projectTasks: List<ProjectTask> = emptyList(),
    val projectPhases: List<Phase> = emptyList(),
    val documents: List<Document> = emptyList(),
    val errorMessage: String? = null,
    val successMessage: String? = null
)

class ContractorDashboardViewModel : ViewModel() {
    
    private val _uiState = MutableStateFlow(ContractorDashboardUiState())
    val uiState: StateFlow<ContractorDashboardUiState> = _uiState.asStateFlow()
    
    fun loadDashboardData() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            try {
                // Load mock data for contractor
                val mockTasks = createMockProjectTasks()
                val mockPhases = createMockProjectPhases()
                val mockDocuments = createMockDocuments()
                
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    projectTasks = mockTasks,
                    projectPhases = mockPhases,
                    documents = mockDocuments
                )
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = e.message ?: "Failed to load dashboard data"
                )
            }
        }
    }
    
    fun updateTaskProgress(taskId: String, progress: Int, status: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            try {
                // Mock task update
                val updatedTasks = _uiState.value.projectTasks.map { task ->
                    if (task.TaskId == taskId) {
                        task.copy(
                            Progress = progress,
                            Status = status,
                            CompletedDate = if (status.lowercase() == "completed") SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.getDefault()).format(Date()) else null
                        )
                    } else {
                        task
                    }
                }
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    projectTasks = updatedTasks,
                    successMessage = "Task updated successfully"
                )
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = e.message ?: "Failed to update task"
                )
            }
        }
    }
    
    fun uploadDocument(document: Document) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            try {
                // Mock document upload
                val newDocument = document.copy(
                    DocumentId = "DOC-${System.currentTimeMillis()}",
                    UploadedAt = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.getDefault()).format(Date()),
                    Status = "Pending"
                )
                val updatedDocuments = _uiState.value.documents + newDocument
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    documents = updatedDocuments,
                    successMessage = "Document uploaded successfully"
                )
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = e.message ?: "Failed to upload document"
                )
            }
        }
    }
    
    fun deleteDocument(documentId: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            
            try {
                // Mock document deletion
                val updatedDocuments = _uiState.value.documents.filter { it.DocumentId != documentId }
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    documents = updatedDocuments,
                    successMessage = "Document deleted successfully"
                )
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = e.message ?: "Failed to delete document"
                )
            }
        }
    }
    
    fun clearMessages() {
        _uiState.value = _uiState.value.copy(errorMessage = null, successMessage = null)
    }
    
    private fun createMockProjectTasks(): List<ProjectTask> {
        val currentDate = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.getDefault()).format(Date())
        val dueDate = Calendar.getInstance().apply {
            add(Calendar.DAY_OF_MONTH, 7)
        }.time
        val dueDateStr = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.getDefault()).format(dueDate)
        
        return listOf(
            ProjectTask(
                TaskId = "TASK-001",
                ProjectId = "PROJ-001",
                PhaseId = "PHASE-001",
                Name = "Install Kitchen Cabinets",
                Description = "Install upper and lower kitchen cabinets according to specifications",
                AssignedTo = "CONTRACTOR-001",
                Priority = "High",
                Status = "In Progress",
                StartDate = currentDate,
                DueDate = dueDateStr,
                CompletedDate = null,
                Progress = 65,
                EstimatedHours = 16.0,
                ActualHours = 10.5
            ),
            ProjectTask(
                TaskId = "TASK-002",
                ProjectId = "PROJ-001",
                PhaseId = "PHASE-001",
                Name = "Plumbing Installation",
                Description = "Install kitchen sink plumbing and connections",
                AssignedTo = "CONTRACTOR-001",
                Priority = "Medium",
                Status = "Pending",
                StartDate = currentDate,
                DueDate = dueDateStr,
                CompletedDate = null,
                Progress = 0,
                EstimatedHours = 8.0,
                ActualHours = 0.0
            ),
            ProjectTask(
                TaskId = "TASK-003",
                ProjectId = "PROJ-002",
                PhaseId = "PHASE-002",
                Name = "Bathroom Tile Work",
                Description = "Install ceramic tiles in bathroom walls and floor",
                AssignedTo = "CONTRACTOR-001",
                Priority = "High",
                Status = "Completed",
                StartDate = currentDate,
                DueDate = dueDateStr,
                CompletedDate = currentDate,
                Progress = 100,
                EstimatedHours = 12.0,
                ActualHours = 11.5
            )
        )
    }
    
    private fun createMockProjectPhases(): List<Phase> {
        val currentDate = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.getDefault()).format(Date())
        val endDate = Calendar.getInstance().apply {
            add(Calendar.DAY_OF_MONTH, 14)
        }.time
        val endDateStr = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.getDefault()).format(endDate)
        
        return listOf(
            Phase(
                PhaseId = "PHASE-001",
                ProjectId = "PROJ-001",
                Name = "Kitchen Installation",
                Description = "Complete kitchen renovation including cabinets, countertops, and plumbing",
                StartDate = currentDate,
                EndDate = endDateStr,
                Status = "In Progress",
                Progress = 45,
                Budget = 25000.0,
                AssignedTo = "CONTRACTOR-001"
            ),
            Phase(
                PhaseId = "PHASE-002",
                ProjectId = "PROJ-002",
                Name = "Bathroom Renovation",
                Description = "Complete bathroom renovation with new fixtures and tile work",
                StartDate = currentDate,
                EndDate = endDateStr,
                Status = "Completed",
                Progress = 100,
                Budget = 18000.0,
                AssignedTo = "CONTRACTOR-001"
            )
        )
    }
    
    private fun createMockDocuments(): List<Document> {
        val currentDate = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.getDefault()).format(Date())
        
        return listOf(
            Document(
                DocumentId = "DOC-001",
                ProjectId = "PROJ-001",
                FileName = "Kitchen_Plan.pdf",
                Status = "Approved",
                FileType = "PDF",
                FileSize = 2048576L,
                FileUrl = "https://example.com/documents/kitchen_plan.pdf",
                UploadedBy = "CONTRACTOR-001",
                UploadedAt = currentDate,
                Description = "Kitchen renovation floor plan and specifications"
            ),
            Document(
                DocumentId = "DOC-002",
                ProjectId = "PROJ-001",
                FileName = "Progress_Photos.zip",
                Status = "Pending",
                FileType = "ZIP",
                FileSize = 15728640L,
                FileUrl = "https://example.com/documents/progress_photos.zip",
                UploadedBy = "CONTRACTOR-001",
                UploadedAt = currentDate,
                Description = "Weekly progress photos of kitchen installation"
            ),
            Document(
                DocumentId = "DOC-003",
                ProjectId = "PROJ-002",
                FileName = "Material_Receipt.pdf",
                Status = "Approved",
                FileType = "PDF",
                FileSize = 512000L,
                FileUrl = "https://example.com/documents/material_receipt.pdf",
                UploadedBy = "CONTRACTOR-001",
                UploadedAt = currentDate,
                Description = "Receipt for bathroom renovation materials"
            )
        )
    }
}
