using ICCMS_API.Models;
using ICCMS_API.Models.Messages;
using Microsoft.Extensions.Logging;

namespace ICCMS_API.Services;

public class MessageDetailService : IMessageDetailService
{
    private readonly IFirebaseService _firebaseService;
    private readonly ILogger<MessageDetailService> _logger;

    public MessageDetailService(
        IFirebaseService firebaseService,
        ILogger<MessageDetailService> logger
    )
    {
        _firebaseService = firebaseService;
        _logger = logger;
    }

    public async Task<MessageDashboardDataDto> BuildAdminDashboardAsync(MessageDashboardQuery query)
    {
        var threadsTask = _firebaseService.GetCollectionAsync<MessageThread>("threads");
        var messagesTask = _firebaseService.GetCollectionAsync<Message>("messages");
        var usersTask = _firebaseService.GetCollectionAsync<User>("users");
        var projectsTask = _firebaseService.GetCollectionAsync<Project>("projects");

        await Task.WhenAll(threadsTask, messagesTask, usersTask, projectsTask);

        var allThreads = (await threadsTask).Where(t => t.IsActive).ToList();
        var messages = await messagesTask;
        var users = await usersTask;
        var projects = await projectsTask;

        var usersDict = users.ToDictionary(u => u.UserId, u => u, StringComparer.OrdinalIgnoreCase);
        var projectsDict = projects.ToDictionary(
            p => p.ProjectId,
            p => p,
            StringComparer.OrdinalIgnoreCase
        );
        var messagesByThread = GroupMessagesByThread(messages);

        var filtered = ApplyThreadFilters(
            allThreads,
            messagesByThread,
            usersDict,
            projectsDict,
            query
        );

        var totalCount = filtered.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)query.PageSize));
        var page = Math.Max(1, query.Page);
        var skip = (page - 1) * query.PageSize;
        var pagedThreads = filtered.Skip(skip).Take(query.PageSize).ToList();

        var summaries = pagedThreads
            .Select(thread =>
                BuildSummaryDto(thread, messagesByThread, usersDict, projectsDict, null)
            )
            .OrderByDescending(t => t.LastMessageAt)
            .ToList();

        var statistics = BuildStatistics(filtered, messagesByThread, null);
        var participantMap = BuildParticipantMap(pagedThreads, usersDict);

        return new MessageDashboardDataDto
        {
            Threads = summaries,
            Statistics = statistics,
            AvailableProjects = projects
                .Select(p => new ProjectSummaryDto
                {
                    ProjectId = p.ProjectId,
                    ProjectName = p.Name ?? p.ProjectId,
                })
                .OrderBy(p => p.ProjectName)
                .ToList(),
            AvailableUsers = users
                .Select(u => new UserSummaryDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName ?? u.Email ?? u.UserId,
                    Role = u.Role ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                })
                .OrderBy(u => u.FullName)
                .ToList(),
            ParticipantMap = participantMap,
            Page = page,
            PageSize = query.PageSize,
            TotalPages = totalPages,
            TotalCount = totalCount,
        };
    }

    public async Task<UserMessagesDetailDto> BuildUserMessagesDetailAsync(
        string userId,
        string userRole
    )
    {
        var threadsTask = _firebaseService.GetCollectionAsync<MessageThread>("threads");
        var messagesTask = _firebaseService.GetCollectionAsync<Message>("messages");
        var workflowTask = _firebaseService.GetCollectionAsync<WorkflowMessage>(
            "workflow-messages"
        );
        var usersTask = _firebaseService.GetCollectionAsync<User>("users");
        var projectsTask = _firebaseService.GetCollectionAsync<Project>("projects");

        await Task.WhenAll(threadsTask, messagesTask, workflowTask, usersTask, projectsTask);

        var threads = (await threadsTask)
            .Where(t =>
                t.IsActive
                && t.Participants.Any(p =>
                    string.Equals(p, userId, StringComparison.OrdinalIgnoreCase)
                )
            )
            .ToList();
        var messages = await messagesTask;
        var workflowMessages = (await workflowTask)
            .Where(wm =>
                wm.Recipients.Any(r => string.Equals(r, userId, StringComparison.OrdinalIgnoreCase))
            )
            .ToList();
        var users = await usersTask;
        var projects = await projectsTask;

        var usersDict = users.ToDictionary(u => u.UserId, u => u, StringComparer.OrdinalIgnoreCase);
        var projectsDict = projects.ToDictionary(
            p => p.ProjectId,
            p => p,
            StringComparer.OrdinalIgnoreCase
        );
        var messagesByThread = GroupMessagesByThread(messages);

        var directSummaries = threads
            .Select(t => BuildSummaryDto(t, messagesByThread, usersDict, projectsDict, userId))
            .OrderByDescending(t => t.LastMessageAt)
            .ToList();

        var workflowSummaries = workflowMessages
            .Select(wm => BuildWorkflowSummary(wm, usersDict, projectsDict))
            .OrderByDescending(t => t.LastMessageAt)
            .ToList();

        var unreadCount = directSummaries.Sum(t => t.UnreadCount);
        var statistics = BuildStatistics(threads, messagesByThread, userId);

        return new UserMessagesDetailDto
        {
            UserId = userId,
            Role = userRole,
            DirectThreads = directSummaries,
            WorkflowThreads = workflowSummaries,
            UnreadCount = unreadCount,
            Statistics = statistics,
        };
    }

    public async Task<MessageThreadDetailDto?> BuildThreadDetailAsync(string threadId)
    {
        var thread = await _firebaseService.GetDocumentAsync<MessageThread>("threads", threadId);
        if (thread == null)
        {
            return null;
        }

        var messagesTask = _firebaseService.GetCollectionAsync<Message>("messages");
        var usersTask = _firebaseService.GetCollectionAsync<User>("users");
        var projectsTask = _firebaseService.GetCollectionAsync<Project>("projects");

        await Task.WhenAll(messagesTask, usersTask, projectsTask);

        var users = (await usersTask).ToList();
        var projects = (await projectsTask).ToList();
        var messages = (await messagesTask)
            .Where(m => string.Equals(m.ThreadId, threadId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageWithSender
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                ProjectId = m.ProjectId,
                Subject = m.Subject,
                Content = m.Content,
                IsRead = m.IsRead,
                SentAt = m.SentAt,
                ReadAt = m.ReadAt,
                ThreadId = m.ThreadId,
                ParentMessageId = m.ParentMessageId,
                IsThreadStarter = m.IsThreadStarter,
                ThreadDepth = m.ThreadDepth,
                ReplyCount = m.ReplyCount,
                LastReplyAt = m.LastReplyAt,
                ThreadParticipants = m.ThreadParticipants,
                MessageType = m.MessageType,
                Attachments = m.Attachments,
                HasAttachments = m.HasAttachments,
                Status = m.Status,
                SenderName = GetUserName(m.SenderId, users),
            })
            .ToList();

        var usersDict = users.ToDictionary(u => u.UserId, u => u, StringComparer.OrdinalIgnoreCase);
        var projectsDict = projects.ToDictionary(
            p => p.ProjectId,
            p => p,
            StringComparer.OrdinalIgnoreCase
        );
        var messagesByThread = new Dictionary<string, List<Message>>
        {
            [thread.ThreadId] = messages.Cast<Message>().ToList(),
        };

        var summary = BuildSummaryDto(thread, messagesByThread, usersDict, projectsDict, null);
        var participants = BuildParticipantMap(new List<MessageThread> { thread }, usersDict);

        return new MessageThreadDetailDto
        {
            Thread = summary,
            Messages = messages,
            Participants = participants,
        };
    }

    private Dictionary<string, List<Message>> GroupMessagesByThread(List<Message> messages)
    {
        return messages
            .Where(m => !string.IsNullOrWhiteSpace(m.ThreadId))
            .GroupBy(m => m.ThreadId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(msg => msg.SentAt).ToList(),
                StringComparer.OrdinalIgnoreCase
            );
    }

    private List<MessageThread> ApplyThreadFilters(
        List<MessageThread> threads,
        Dictionary<string, List<Message>> messagesByThread,
        Dictionary<string, User> users,
        Dictionary<string, Project> projects,
        MessageDashboardQuery query
    )
    {
        var filtered = threads.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.ProjectId) && query.ProjectId != "all")
        {
            filtered = filtered.Where(t =>
                string.Equals(t.ProjectId, query.ProjectId, StringComparison.OrdinalIgnoreCase)
            );
        }

        if (!string.IsNullOrWhiteSpace(query.ThreadType) && query.ThreadType != "all")
        {
            filtered = filtered.Where(t =>
                string.Equals(t.ThreadType, query.ThreadType, StringComparison.OrdinalIgnoreCase)
            );
        }

        if (!string.IsNullOrWhiteSpace(query.UserId) && query.UserId != "all")
        {
            filtered = filtered.Where(t =>
                t.Participants.Any(p =>
                    string.Equals(p, query.UserId, StringComparison.OrdinalIgnoreCase)
                )
            );
        }

        if (query.StartDate.HasValue)
        {
            filtered = filtered.Where(t => t.LastMessageAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            filtered = filtered.Where(t => t.LastMessageAt <= query.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLowerInvariant();
            filtered = filtered.Where(t =>
                t.Subject.ToLowerInvariant().Contains(search)
                || (
                    projects.TryGetValue(t.ProjectId, out var project)
                    && (project.Name ?? project.ProjectId).ToLowerInvariant().Contains(search)
                )
                || t.Participants.Any(p =>
                    users.TryGetValue(p, out var participant)
                    && (participant.FullName ?? participant.Email ?? participant.UserId)
                        .ToLowerInvariant()
                        .Contains(search)
                )
            );
        }

        if (!string.IsNullOrWhiteSpace(query.ReadStatus) && query.ReadStatus != "all")
        {
            filtered = filtered.Where(t =>
            {
                if (!messagesByThread.TryGetValue(t.ThreadId, out var threadMessages))
                {
                    return false;
                }

                var hasUnread = threadMessages.Any(m => !m.IsRead);
                return query.ReadStatus == "unread" ? hasUnread : !hasUnread;
            });
        }

        return filtered.OrderByDescending(t => t.LastMessageAt).ToList();
    }

    private MessageThreadSummaryDto BuildSummaryDto(
        MessageThread thread,
        Dictionary<string, List<Message>> messagesByThread,
        Dictionary<string, User> users,
        Dictionary<string, Project> projects,
        string? perspectiveUserId
    )
    {
        messagesByThread.TryGetValue(thread.ThreadId, out var threadMessages);
        threadMessages ??= new List<Message>();

        var participantNames = thread
            .Participants.Select(p =>
                users.TryGetValue(p, out var participant)
                    ? participant.FullName ?? participant.Email ?? participant.UserId
                    : p
            )
            .ToList();

        var unreadCount =
            perspectiveUserId == null
                ? threadMessages.Count(m => !m.IsRead)
                : threadMessages.Count(m =>
                    !m.IsRead
                    && string.Equals(
                        m.ReceiverId,
                        perspectiveUserId,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

        var projectName = projects.TryGetValue(thread.ProjectId, out var project)
            ? project.Name ?? thread.ProjectId
            : "Unknown Project";

        return new MessageThreadSummaryDto
        {
            ThreadId = thread.ThreadId,
            Subject = thread.Subject,
            ProjectId = thread.ProjectId,
            ProjectName = projectName,
            ThreadType = thread.ThreadType,
            MessageCount = thread.MessageCount > 0 ? thread.MessageCount : threadMessages.Count,
            LastMessageAt = thread.LastMessageAt,
            CreatedAt = thread.CreatedAt,
            Participants = thread.Participants,
            ParticipantNames = participantNames,
            HasUnreadMessages = unreadCount > 0,
            UnreadCount = unreadCount,
        };
    }

    private MessageThreadSummaryDto BuildWorkflowSummary(
        WorkflowMessage message,
        Dictionary<string, User> users,
        Dictionary<string, Project> projects
    )
    {
        var participantNames = message
            .Recipients.Select(p =>
                users.TryGetValue(p, out var participant)
                    ? participant.FullName ?? participant.Email ?? participant.UserId
                    : p
            )
            .ToList();

        var projectName = projects.TryGetValue(message.ProjectId, out var project)
            ? project.Name ?? message.ProjectId
            : "Unknown Project";

        return new MessageThreadSummaryDto
        {
            ThreadId = message.WorkflowMessageId,
            Subject = message.Subject,
            ProjectId = message.ProjectId,
            ProjectName = projectName,
            ThreadType = "workflow",
            MessageCount = 1,
            LastMessageAt = message.CreatedAt,
            CreatedAt = message.CreatedAt,
            Participants = message.Recipients,
            ParticipantNames = participantNames,
            HasUnreadMessages = false,
            UnreadCount = 0,
        };
    }

    private MessageStatisticsDto BuildStatistics(
        IEnumerable<MessageThread> threads,
        Dictionary<string, List<Message>> messagesByThread,
        string? perspectiveUserId
    )
    {
        var threadList = threads.ToList();
        var totalMessages = threadList.Sum(t =>
        {
            if (messagesByThread.TryGetValue(t.ThreadId, out var list))
            {
                return list.Count;
            }
            return t.MessageCount;
        });

        var unreadMessages = threadList.Sum(t =>
        {
            if (!messagesByThread.TryGetValue(t.ThreadId, out var list))
            {
                return 0;
            }

            return perspectiveUserId == null
                ? list.Count(m => !m.IsRead)
                : list.Count(m =>
                    !m.IsRead
                    && string.Equals(
                        m.ReceiverId,
                        perspectiveUserId,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
        });

        var activeThisWeek = threadList.Count(t => t.LastMessageAt >= DateTime.UtcNow.AddDays(-7));

        return new MessageStatisticsDto
        {
            TotalThreads = threadList.Count,
            TotalMessages = totalMessages,
            UnreadMessages = unreadMessages,
            ActiveThisWeek = activeThisWeek,
        };
    }

    private Dictionary<string, UserSummaryDto> BuildParticipantMap(
        IEnumerable<MessageThread> threads,
        Dictionary<string, User> users
    )
    {
        var map = new Dictionary<string, UserSummaryDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var participantId in threads.SelectMany(t => t.Participants).Distinct())
        {
            if (!users.TryGetValue(participantId, out var user))
            {
                continue;
            }

            map[participantId] = new UserSummaryDto
            {
                UserId = user.UserId,
                FullName = user.FullName ?? user.Email ?? user.UserId,
                Role = user.Role ?? string.Empty,
                Email = user.Email ?? string.Empty,
            };
        }

        return map;
    }

    private string GetUserName(string userId, List<User> users)
    {
        var user = users.FirstOrDefault(u =>
            string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase)
        );
        return user?.FullName ?? user?.Email ?? userId;
    }
}
