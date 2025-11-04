using System.Collections.Concurrent;
using ICCMS_Web.Models;

namespace ICCMS_Web.Services
{
    public interface IProjectIndexService
    {
        void BuildOrUpdateIndex(string userId, IEnumerable<ProjectDto> projects);
        ProjectSearchIndex? GetIndex(string userId);
        IEnumerable<ProjectDto> Search(
            string userId,
            string? query,
            string? status = null,
            string? clientId = null
        );
    }

    /// <summary>
    /// Holds per-user project indexes for fast in-memory search and filtering
    /// </summary>
    public class ProjectIndexService : IProjectIndexService
    {
        private readonly ConcurrentDictionary<string, ProjectSearchIndex> _userIdToIndex = new();

        public void BuildOrUpdateIndex(string userId, IEnumerable<ProjectDto> projects)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;
            var snapshot = projects?.ToList() ?? new List<ProjectDto>();
            _userIdToIndex[userId] = new ProjectSearchIndex(snapshot);
        }

        public ProjectSearchIndex? GetIndex(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;
            _userIdToIndex.TryGetValue(userId, out var idx);
            return idx;
        }

        public IEnumerable<ProjectDto> Search(
            string userId,
            string? query,
            string? status = null,
            string? clientId = null
        )
        {
            var idx = GetIndex(userId);
            if (idx == null)
                return Enumerable.Empty<ProjectDto>();

            // Start with the appropriate base set
            IEnumerable<ProjectDto> seq;
            
            // If filtering by clientId, use the optimized ByClientId lookup
            if (!string.IsNullOrEmpty(clientId))
            {
                // Try exact match first (fastest)
                if (idx.ByClientId.TryGetValue(clientId, out var clientProjects))
                {
                    seq = clientProjects;
                }
                else
                {
                    // Try case-insensitive lookup
                    var matchingClientKey = idx.ByClientId.Keys
                        .FirstOrDefault(k => string.Equals(k, clientId, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingClientKey != null && idx.ByClientId.TryGetValue(matchingClientKey, out clientProjects))
                    {
                        seq = clientProjects;
                    }
                    else
                    {
                        // Fallback: search through all projects (in case ClientId is stored differently)
                        var allProjects = idx.ById.Values.ToList();
                        var allClientIds = allProjects
                            .Where(p => !string.IsNullOrEmpty(p.ClientId))
                            .Select(p => p.ClientId)
                            .Distinct()
                            .ToList();
                        
                        var filtered = allProjects.Where(p => 
                        {
                            if (string.IsNullOrEmpty(p.ClientId))
                                return false;
                            var matches = string.Equals(p.ClientId, clientId, StringComparison.OrdinalIgnoreCase);
                            return matches;
                        }).ToList();
                        
                        if (filtered.Any())
                        {
                            seq = filtered;
                        }
                        else
                        {
                            // No projects found for this clientId
                            return Enumerable.Empty<ProjectDto>();
                        }
                    }
                }
            }
            else
            {
                // No client filter - use all projects or search results
                seq = string.IsNullOrWhiteSpace(query)
                    ? idx.ById.Values
                    : idx.Search(query);
            }
            
            // Apply status filter if provided
            if (!string.IsNullOrEmpty(status))
            {
                seq = seq.Where(p =>
                    string.Equals(p.Status, status, StringComparison.OrdinalIgnoreCase)
                );
            }

            return seq;
        }
    }

    /// <summary>
    /// Tokenized, hash-based index for a user's projects enabling O(1) id lookup and efficient set intersections for search
    /// </summary>
    public class ProjectSearchIndex
    {
        public readonly Dictionary<string, ProjectDto> ById = new();
        public readonly Dictionary<string, List<ProjectDto>> ByClientId = new();
        public readonly Dictionary<string, List<ProjectDto>> ByStatus = new();
        public readonly Dictionary<string, HashSet<string>> TokenToProjectIds = new();

        public ProjectSearchIndex(IEnumerable<ProjectDto> projects)
        {
            foreach (var project in projects)
            {
                if (string.IsNullOrEmpty(project.ProjectId))
                    continue;
                ById[project.ProjectId] = project;

                var clientKey = project.ClientId ?? string.Empty;
                if (!ByClientId.TryGetValue(clientKey, out var byClientList))
                {
                    byClientList = new List<ProjectDto>();
                    ByClientId[clientKey] = byClientList;
                }
                byClientList.Add(project);

                var statusKey = project.Status ?? string.Empty;
                if (!ByStatus.TryGetValue(statusKey, out var byStatusList))
                {
                    byStatusList = new List<ProjectDto>();
                    ByStatus[statusKey] = byStatusList;
                }
                byStatusList.Add(project);

                foreach (var token in Tokenize(project.Name).Concat(Tokenize(project.Description)))
                {
                    if (!TokenToProjectIds.TryGetValue(token, out var idSet))
                    {
                        idSet = new HashSet<string>();
                        TokenToProjectIds[token] = idSet;
                    }
                    idSet.Add(project.ProjectId);
                }
            }
        }

        public IEnumerable<ProjectDto> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return ById.Values;
            var tokens = Tokenize(query).ToList();
            if (tokens.Count == 0)
                return ById.Values;

            HashSet<string>? intersection = null;
            foreach (var token in tokens)
            {
                if (!TokenToProjectIds.TryGetValue(token, out var idsForToken))
                {
                    return Enumerable.Empty<ProjectDto>();
                }
                intersection =
                    intersection == null
                        ? new HashSet<string>(idsForToken)
                        : intersection.Intersect(idsForToken).ToHashSet();
                if (intersection.Count == 0)
                    break;
            }

            return (intersection ?? new HashSet<string>()).Select(id => ById[id]);
        }

        private static IEnumerable<string> Tokenize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                yield break;
            var separators = new[]
            {
                ' ',
                '\t',
                '\n',
                '\r',
                ',',
                '.',
                ';',
                ':',
                '-',
                '_',
                '/',
                '\\',
                '(',
                ')',
                '[',
                ']',
                '{',
                '}',
            };
            foreach (
                var part in value
                    .ToLowerInvariant()
                    .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            )
            {
                yield return part;
            }
        }
    }
}
