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
        Task<string> AddDocumentAsync<T>(string collection, T document)
            where T : class;
        Task AddDocumentWithIdAsync<T>(string collection, string documentId, T document)
            where T : class;
        Task UpdateDocumentAsync<T>(string collection, string documentId, T document)
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
    }
}
