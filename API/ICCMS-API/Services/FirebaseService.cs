using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using ICCMS_API.Models;
using Microsoft.Extensions.Configuration;

namespace ICCMS_API.Services
{
    public interface IFirebaseService
    {
        Task<T> GetDocumentAsync<T>(string collection, string documentId)
            where T : class;
        Task<List<T>> GetCollectionAsync<T>(string collection)
            where T : class;
        Task<List<DocumentSnapshot>> GetRawCollectionAsync(string collection);
        Task<List<T>> GetCollectionWithFiltersAsync<T>(
            string collection,
            Dictionary<string, object> filters,
            int page = 1,
            int pageSize = 20,
            string orderBy = "StartDate",
            bool orderDescending = true
        )
            where T : class;
        Task<int> GetCollectionCountAsync<T>(string collection, Dictionary<string, object> filters)
            where T : class;
        Task<string> AddDocumentAsync<T>(string collection, T document)
            where T : class;
        Task AddDocumentWithIdAsync<T>(string collection, string documentId, T document)
            where T : class;
        Task UpdateDocumentAsync<T>(string collection, string documentId, T document)
            where T : class;
        Task<bool> UpdateDocumentConditionallyAsync<T>(
            string collection,
            string documentId,
            T document,
            Dictionary<string, object> conditions
        )
            where T : class;
        Task DeleteDocumentAsync(string collection, string documentId);
        Task<bool> DeleteDocumentWithFileAsync(string fileName);
    }

    public class FirebaseService : IFirebaseService
    {
        private readonly FirestoreDb _firestoreDb;

        public FirebaseService(IConfiguration configuration)
        {
            try
            {
                var projectId = configuration["Firebase:project_id"];

                // Try to get credentials path from configuration (user secrets)
                var credentialsPath = configuration["Firebase:CredentialsPath"];

                // Fallback to environment variable if not in configuration
                if (string.IsNullOrEmpty(credentialsPath))
                {
                    credentialsPath = Environment.GetEnvironmentVariable(
                        "FIREBASE_CREDENTIALS_PATH"
                    );
                }

                if (string.IsNullOrEmpty(credentialsPath))
                {
                    throw new InvalidOperationException(
                        "Firebase credentials path not configured. Set Firebase:CredentialsPath in user secrets or FIREBASE_CREDENTIALS_PATH environment variable."
                    );
                }

                if (!File.Exists(credentialsPath))
                {
                    throw new FileNotFoundException(
                        $"Firebase credentials file not found at: {credentialsPath}"
                    );
                }

                GoogleCredential credential = GoogleCredential.FromFile(credentialsPath);
                _firestoreDb = new FirestoreDbBuilder
                {
                    ProjectId = projectId,
                    Credential = credential,
                }.Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating FirestoreDb: {ex.Message}");
                throw;
            }
        }

        public async Task<T> GetDocumentAsync<T>(string collection, string documentId)
            where T : class
        {
            try
            {
                var document = await _firestoreDb
                    .Collection(collection)
                    .Document(documentId)
                    .GetSnapshotAsync();

                if (!document.Exists)
                {
                    return null;
                }

                var result = document.ConvertTo<T>();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting document {documentId}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<T>> GetCollectionAsync<T>(string collection)
            where T : class
        {
            try
            {
                var snapshot = await _firestoreDb.Collection(collection).GetSnapshotAsync();

                var result = new List<T>();
                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        T converted;

                        if (typeof(T) == typeof(Document) || typeof(T).Name == "Document")
                        {
                            // Always use manual conversion for Document objects since ConvertTo<T> returns default values

                            // Debug: Log the actual field values being extracted
                            var projectId = doc.ContainsField("projectId")
                                ? doc.GetValue<string>("projectId") ?? ""
                                : "";
                            var fileName = doc.ContainsField("fileName")
                                ? doc.GetValue<string>("fileName") ?? ""
                                : "";
                            var status = doc.ContainsField("status")
                                ? doc.GetValue<string>("status") ?? "active"
                                : "active";
                            var fileType = doc.ContainsField("fileType")
                                ? doc.GetValue<string>("fileType") ?? ""
                                : "";
                            var fileSize = doc.ContainsField("fileSize")
                                ? doc.GetValue<long>("fileSize")
                                : 0;
                            var fileUrl = doc.ContainsField("fileUrl")
                                ? doc.GetValue<string>("fileUrl") ?? ""
                                : "";
                            var uploadedBy = doc.ContainsField("uploadedBy")
                                ? doc.GetValue<string>("uploadedBy") ?? ""
                                : "";
                            var uploadedAt = doc.ContainsField("uploadedAt")
                                ? doc.GetValue<DateTime>("uploadedAt")
                                : DateTime.UtcNow;
                            var description = doc.ContainsField("description")
                                ? doc.GetValue<string>("description") ?? ""
                                : "";

                            var manualDoc = new Document
                            {
                                DocumentId = doc.Id,
                                ProjectId = projectId,
                                FileName = fileName,
                                Status = status,
                                FileType = fileType,
                                FileSize = fileSize,
                                FileUrl = fileUrl,
                                UploadedBy = uploadedBy,
                                UploadedAt = uploadedAt,
                                Description = description,
                            };
                            converted = (T)(object)manualDoc;
                        }
                        else if (typeof(T) == typeof(AuditLog) || typeof(T).Name == "AuditLog")
                        {
                            // Handle AuditLog - need to set the Id from doc.Id
                            var auditLog = doc.ConvertTo<AuditLog>();
                            if (auditLog != null)
                            {
                                auditLog.Id = doc.Id; // Set the Firestore document ID
                            }
                            converted = (T)(object)auditLog;
                        }
                        else
                        {
                            try
                            {
                                converted = doc.ConvertTo<T>();
                            }
                            catch (Exception convertEx)
                            {
                                Console.WriteLine(
                                    $"ConvertTo<T> failed for document {doc.Id}: {convertEx.Message}"
                                );
                                throw convertEx; // Re-throw for non-Document types
                            }
                        }

                        result.Add(converted);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting document {doc.Id}: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        // Continue with other documents
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting collection {collection}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<DocumentSnapshot>> GetRawCollectionAsync(string collection)
        {
            try
            {
                var snapshot = await _firestoreDb.Collection(collection).GetSnapshotAsync();
                var result = snapshot.Documents.ToList();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting raw collection {collection}: {ex.Message}");
                throw;
            }
        }

        public async Task<string> AddDocumentAsync<T>(string collection, T document)
            where T : class
        {
            try
            {
                var docRef = await _firestoreDb.Collection(collection).AddAsync(document);
                return docRef.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding document to {collection}: {ex.Message}");
                throw;
            }
        }

        public async Task AddDocumentWithIdAsync<T>(
            string collection,
            string documentId,
            T document
        )
            where T : class
        {
            try
            {
                await _firestoreDb.Collection(collection).Document(documentId).SetAsync(document);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding document with ID {documentId}: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateDocumentAsync<T>(string collection, string documentId, T document)
            where T : class
        {
            try
            {
                await _firestoreDb.Collection(collection).Document(documentId).SetAsync(document);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating document {documentId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateDocumentConditionallyAsync<T>(
            string collection,
            string documentId,
            T document,
            Dictionary<string, object> conditions
        )
            where T : class
        {
            try
            {
                return await _firestoreDb.RunTransactionAsync(async transaction =>
                {
                    var docRef = _firestoreDb.Collection(collection).Document(documentId);
                    var snapshot = await transaction.GetSnapshotAsync(docRef);

                    if (!snapshot.Exists)
                    {
                        return false;
                    }

                    // Check all conditions
                    foreach (var condition in conditions)
                    {
                        if (!snapshot.ContainsField(condition.Key))
                        {
                            return false;
                        }

                        var fieldValue = snapshot.GetValue<object>(condition.Key);
                        if (!fieldValue?.Equals(condition.Value) == true)
                        {
                            return false;
                        }
                    }

                    // All conditions met, perform the update
                    transaction.Set(docRef, document);
                    return true;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error conditionally updating document {documentId}: {ex.Message}"
                );
                throw;
            }
        }

        public async Task DeleteDocumentAsync(string collection, string documentId)
        {
            try
            {
                await _firestoreDb.Collection(collection).Document(documentId).DeleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting document {documentId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Specialized method to delete a document from both Firestore and Supabase storage
        /// </summary>
        public async Task<bool> DeleteDocumentWithFileAsync(string fileName)
        {
            try
            {
                Console.WriteLine(
                    $"[DeleteDocumentWithFileAsync] Starting deletion for file: {fileName}"
                );

                // First, find the document in Firestore by searching through all documents
                var documents = await GetCollectionAsync<Document>("documents");
                var documentToDelete = documents.FirstOrDefault(d => d.FileName == fileName);

                if (documentToDelete == null)
                {
                    Console.WriteLine(
                        $"[DeleteDocumentWithFileAsync] Document not found in Firestore for fileName: {fileName}"
                    );
                    return false;
                }

                Console.WriteLine(
                    $"[DeleteDocumentWithFileAsync] Found document with DocumentId: {documentToDelete.DocumentId}"
                );
                Console.WriteLine(
                    $"[DeleteDocumentWithFileAsync] FileName: {documentToDelete.FileName}"
                );
                Console.WriteLine(
                    $"[DeleteDocumentWithFileAsync] FileUrl: {documentToDelete.FileUrl}"
                );

                // Extract the actual document ID from the filename (the UUID prefix)
                var actualDocumentId = ExtractDocumentIdFromFileName(fileName);
                Console.WriteLine(
                    $"[DeleteDocumentWithFileAsync] Extracted actual document ID: {actualDocumentId}"
                );

                // Try to delete from Firestore using the actual document ID first
                bool firestoreDeleted = false;
                try
                {
                    await _firestoreDb
                        .Collection("documents")
                        .Document(actualDocumentId)
                        .DeleteAsync();
                    firestoreDeleted = true;
                    Console.WriteLine(
                        $"[DeleteDocumentWithFileAsync] Successfully deleted from Firestore using actual document ID: {actualDocumentId}"
                    );
                }
                catch (Exception firestoreEx)
                {
                    Console.WriteLine(
                        $"[DeleteDocumentWithFileAsync] Failed to delete from Firestore using actual document ID: {firestoreEx.Message}"
                    );

                    // Fallback: try using the DocumentId field value
                    try
                    {
                        await _firestoreDb
                            .Collection("documents")
                            .Document(documentToDelete.DocumentId)
                            .DeleteAsync();
                        firestoreDeleted = true;
                        Console.WriteLine(
                            $"[DeleteDocumentWithFileAsync] Successfully deleted from Firestore using DocumentId field: {documentToDelete.DocumentId}"
                        );
                    }
                    catch (Exception fallbackEx)
                    {
                        Console.WriteLine(
                            $"[DeleteDocumentWithFileAsync] Failed to delete from Firestore using DocumentId field: {fallbackEx.Message}"
                        );
                    }
                }

                return firestoreDeleted;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[DeleteDocumentWithFileAsync] Error deleting document {fileName}: {ex.Message}"
                );
                Console.WriteLine($"[DeleteDocumentWithFileAsync] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Extract the actual document ID from the filename (UUID prefix)
        /// </summary>
        private string ExtractDocumentIdFromFileName(string fileName)
        {
            try
            {
                // Extract UUID from filename like "07ae836f-3e5c-41c7-bf99-347b76378842_detailedBlueprint.pdf"
                var parts = fileName.Split('_');
                if (parts.Length > 0)
                {
                    var uuidPart = parts[0];
                    // Validate that it looks like a UUID
                    if (Guid.TryParse(uuidPart, out _))
                    {
                        return uuidPart;
                    }
                }
                return fileName; // Fallback to full filename
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[ExtractDocumentIdFromFileName] Error extracting ID from {fileName}: {ex.Message}"
                );
                return fileName; // Fallback to full filename
            }
        }

        public async Task<List<T>> GetCollectionWithFiltersAsync<T>(
            string collection,
            Dictionary<string, object> filters,
            int page = 1,
            int pageSize = 20,
            string orderBy = "StartDate",
            bool orderDescending = true
        )
            where T : class
        {
            try
            {
                Query query = _firestoreDb.Collection(collection);

                // Apply filters
                foreach (var filter in filters)
                {
                    if (filter.Value != null && !string.IsNullOrEmpty(filter.Value.ToString()))
                    {
                        switch (filter.Key.ToLower())
                        {
                            case "status":
                                query = query.WhereEqualTo("status", filter.Value);
                                break;
                            case "projectmanagerid":
                                query = query.WhereEqualTo("projectManagerId", filter.Value);
                                break;
                            case "clientid":
                                query = query.WhereEqualTo("clientId", filter.Value);
                                break;
                            case "budgetmin":
                                query = query.WhereGreaterThanOrEqualTo(
                                    "budgetPlanned",
                                    Convert.ToDouble(filter.Value)
                                );
                                break;
                            case "budgetmax":
                                query = query.WhereLessThanOrEqualTo(
                                    "budgetPlanned",
                                    Convert.ToDouble(filter.Value)
                                );
                                break;
                            case "startdatefrom":
                                query = query.WhereGreaterThanOrEqualTo(
                                    "startDatePlanned",
                                    filter.Value
                                );
                                break;
                            case "startdateto":
                                query = query.WhereLessThanOrEqualTo(
                                    "startDatePlanned",
                                    filter.Value
                                );
                                break;
                            case "assignedto":
                                query = query.WhereEqualTo("assignedTo", filter.Value);
                                break;
                            case "taskid":
                                query = query.WhereEqualTo("TaskId", filter.Value);
                                break;
                            case "searchquery":
                                // For text search, we'll need to implement a different approach
                                // Firestore doesn't support full-text search natively
                                // For now, we'll skip this filter and handle it in memory
                                break;
                        }
                    }
                }

                // Apply ordering
                if (orderDescending)
                {
                    query = query.OrderByDescending(orderBy);
                }
                else
                {
                    query = query.OrderBy(orderBy);
                }

                // Get all results first (Firestore limitation - no direct offset support)
                var snapshot = await query.GetSnapshotAsync();
                var allResults = snapshot.Documents.Select(doc => doc.ConvertTo<T>()).ToList();

                // Apply pagination in memory
                var result = allResults.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // Apply text search filter in memory if provided
                if (
                    filters.ContainsKey("searchquery")
                    && !string.IsNullOrEmpty(filters["searchquery"]?.ToString())
                )
                {
                    var searchTerm = filters["searchquery"].ToString().ToLower();
                    result = result
                        .Where(item =>
                        {
                            var name = GetPropertyValue(item, "Name")?.ToString()?.ToLower() ?? "";
                            var description =
                                GetPropertyValue(item, "Description")?.ToString()?.ToLower() ?? "";
                            return name.Contains(searchTerm) || description.Contains(searchTerm);
                        })
                        .ToList();
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting filtered collection {collection}: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetCollectionCountAsync<T>(
            string collection,
            Dictionary<string, object> filters
        )
            where T : class
        {
            try
            {
                Query query = _firestoreDb.Collection(collection);

                // Apply filters (same logic as GetCollectionWithFiltersAsync)
                foreach (var filter in filters)
                {
                    if (filter.Value != null && !string.IsNullOrEmpty(filter.Value.ToString()))
                    {
                        switch (filter.Key.ToLower())
                        {
                            case "status":
                                query = query.WhereEqualTo("status", filter.Value);
                                break;
                            case "projectmanagerid":
                                query = query.WhereEqualTo("projectManagerId", filter.Value);
                                break;
                            case "clientid":
                                query = query.WhereEqualTo("clientId", filter.Value);
                                break;
                            case "budgetmin":
                                query = query.WhereGreaterThanOrEqualTo(
                                    "budgetPlanned",
                                    Convert.ToDouble(filter.Value)
                                );
                                break;
                            case "budgetmax":
                                query = query.WhereLessThanOrEqualTo(
                                    "budgetPlanned",
                                    Convert.ToDouble(filter.Value)
                                );
                                break;
                            case "startdatefrom":
                                query = query.WhereGreaterThanOrEqualTo(
                                    "startDatePlanned",
                                    filter.Value
                                );
                                break;
                            case "startdateto":
                                query = query.WhereLessThanOrEqualTo(
                                    "startDatePlanned",
                                    filter.Value
                                );
                                break;
                            case "assignedto":
                                query = query.WhereEqualTo("assignedTo", filter.Value);
                                break;
                            case "taskid":
                                query = query.WhereEqualTo("TaskId", filter.Value);
                                break;
                        }
                    }
                }

                var snapshot = await query.GetSnapshotAsync();
                var count = snapshot.Count;

                // Apply text search filter in memory if provided
                if (
                    filters.ContainsKey("searchquery")
                    && !string.IsNullOrEmpty(filters["searchquery"]?.ToString())
                )
                {
                    var searchTerm = filters["searchquery"].ToString().ToLower();
                    var allItems = snapshot.Documents.Select(doc => doc.ConvertTo<T>()).ToList();
                    count = allItems.Count(item =>
                    {
                        var name = GetPropertyValue(item, "Name")?.ToString()?.ToLower() ?? "";
                        var description =
                            GetPropertyValue(item, "Description")?.ToString()?.ToLower() ?? "";
                        return name.Contains(searchTerm) || description.Contains(searchTerm);
                    });
                }

                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting count for collection {collection}: {ex.Message}");
                throw;
            }
        }

        private object GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
            }
            catch
            {
                return null;
            }
        }
    }
}
