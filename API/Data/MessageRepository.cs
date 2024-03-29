﻿using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class MessageRepository : IMessageRepository
{
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;

    public MessageRepository(DataContext dataContext, IMapper mapper)
    {
        _dataContext = dataContext;
        _mapper = mapper;
    }
    
    public void AddMessage(Message message)
    {
        _dataContext.Messages.Add(message);
    }

    public void DeleteMessage(Message message)
    {
        _dataContext.Messages.Remove(message);
    }

    public async Task<Message> GetMessage(int id)
    {
        return await _dataContext.Messages.FindAsync(id);
    }

    public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
    {
        var query = _dataContext.Messages
            .OrderByDescending(x => x.MessageSent)
            .AsQueryable();

        query = messageParams.Container switch
        {
            "Inbox" => query.Where(u => u.RecipientUserName == messageParams.UserName && u.RecipientDeleted == false),
            "Outbox" => query.Where(u => u.SenderUserName == messageParams.UserName && u.SenderDeleted == false),
            _ => query.Where(u => u.RecipientUserName == messageParams.UserName && u.DateRead == null && u.RecipientDeleted == false)
        };

        var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

        return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
    }
    

    public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUserName, string recipientUserName)
    {
        var query = _dataContext.Messages
            .Where(x => (x.RecipientUserName == currentUserName && x.RecipientDeleted == false && x.SenderUserName == recipientUserName) ||
                        (x.RecipientUserName == recipientUserName && x.SenderDeleted == false && x.SenderUserName == currentUserName))
            .OrderBy(x => x.MessageSent)
            .AsQueryable();

        var unreadMessages = query.Where(x => x.DateRead == null && x.RecipientUserName == currentUserName).ToList();

        if (unreadMessages.Any())
        {
            foreach (var message in unreadMessages)
            {
                message.DateRead = DateTime.UtcNow;
            }
        }

        return await query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider).ToListAsync();
    }

    public void AddGroup(Group group)
    {
        _dataContext.Add(group);
    }

    public void RemoveConnection(Connection connection)
    {
        _dataContext.Connections.Remove(connection);
    }

    public async Task<Connection> GetConnection(string connectionId)
    {
        return await _dataContext.Connections.FindAsync(connectionId);
    }

    public async Task<Group> GetMessageGroup(string groupName)
    {
        return await _dataContext.Groups.Include(x => x.Connections).FirstOrDefaultAsync(x => x.Name == groupName);
    }

    public async Task<Group> GetGroupForConnection(string connectionId)
    {
        return await _dataContext.Groups
            .Include(x => x.Connections)
            .FirstOrDefaultAsync(x => x.Connections.Any(c => c.ConnectionId == connectionId));
    }
}