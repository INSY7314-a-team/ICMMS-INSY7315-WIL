using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;

namespace ICCMS_API.Services
{
    public interface IFirebaseService
    {
        Task<T> GetDocumentAsync<T>(string collection, string documentId)
            where T : class;
        Task<List<T>> GetCollectionAsync<T>(string collection)
            where T : class;
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
                Console.WriteLine("FirestoreDb created successfully");
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
                Console.WriteLine($"Getting document: {collection}/{documentId}");

                var document = await _firestoreDb
                    .Collection(collection)
                    .Document(documentId)
                    .GetSnapshotAsync();

                if (!document.Exists)
                {
                    Console.WriteLine($"Document {documentId} does not exist");
                    return null;
                }

                Console.WriteLine($"Document {documentId} exists, converting to {typeof(T).Name}");
                var result = document.ConvertTo<T>();
                Console.WriteLine($"Successfully converted document {documentId}");
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
                Console.WriteLine($"Getting collection: {collection}");
                var snapshot = await _firestoreDb.Collection(collection).GetSnapshotAsync();
                var result = snapshot.Documents.Select(doc => doc.ConvertTo<T>()).ToList();
                Console.WriteLine($"Retrieved {result.Count} documents from {collection}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting collection {collection}: {ex.Message}");
                throw;
            }
        }

        public async Task<string> AddDocumentAsync<T>(string collection, T document)
            where T : class
        {
            try
            {
                Console.WriteLine($"Adding document to {collection}");
                var docRef = await _firestoreDb.Collection(collection).AddAsync(document);
                Console.WriteLine($"Added document with ID: {docRef.Id}");
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
                Console.WriteLine($"Adding document with ID: {collection}/{documentId}");
                await _firestoreDb.Collection(collection).Document(documentId).SetAsync(document);
                Console.WriteLine($"Added document with ID: {documentId}");
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
                Console.WriteLine($"Updating document: {collection}/{documentId}");
                await _firestoreDb.Collection(collection).Document(documentId).SetAsync(document);
                Console.WriteLine($"Updated document: {documentId}");
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
                Console.WriteLine($"Conditionally updating document: {collection}/{documentId}");

                return await _firestoreDb.RunTransactionAsync(async transaction =>
                {
                    var docRef = _firestoreDb.Collection(collection).Document(documentId);
                    var snapshot = await transaction.GetSnapshotAsync(docRef);

                    if (!snapshot.Exists)
                    {
                        Console.WriteLine($"Document {documentId} does not exist");
                        return false;
                    }

                    // Check all conditions
                    foreach (var condition in conditions)
                    {
                        if (!snapshot.ContainsField(condition.Key))
                        {
                            Console.WriteLine(
                                $"Document {documentId} does not contain field {condition.Key}"
                            );
                            return false;
                        }

                        var fieldValue = snapshot.GetValue<object>(condition.Key);
                        if (!fieldValue?.Equals(condition.Value) == true)
                        {
                            Console.WriteLine(
                                $"Condition failed: {condition.Key} = {fieldValue}, expected {condition.Value}"
                            );
                            return false;
                        }
                    }

                    // All conditions met, perform the update
                    transaction.Set(docRef, document);
                    Console.WriteLine($"Conditional update successful for document: {documentId}");
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
                Console.WriteLine($"Deleting document: {collection}/{documentId}");
                await _firestoreDb.Collection(collection).Document(documentId).DeleteAsync();
                Console.WriteLine($"Deleted document: {documentId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting document {documentId}: {ex.Message}");
                throw;
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
                Console.WriteLine(
                    $"Getting filtered collection: {collection} with {filters.Count} filters, page {page}, size {pageSize}"
                );

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

                Console.WriteLine($"Retrieved {result.Count} filtered documents from {collection}");
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
                Console.WriteLine(
                    $"Getting count for collection: {collection} with {filters.Count} filters"
                );

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

                Console.WriteLine($"Count for {collection}: {count}");
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
