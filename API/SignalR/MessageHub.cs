using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR;

[Authorize]
public class MessageHub: Hub
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IHubContext<PresenceHub> _presenceHub;

    public MessageHub(
        IUnitOfWork uow,
        IMapper mapper, 
        IHubContext<PresenceHub> presenceHub)
    {
        _uow = uow;
        _mapper = mapper;
        _presenceHub = presenceHub;
    }

    public override async Task OnConnectedAsync()
    {
        var context = Context.GetHttpContext();
        if (context != null)
        {
            var otherUser = context.Request.Query["user"];
            var groupName = GetGroupName(context.User.GetUserName(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);

            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);
            
            var messages = await _uow.MessageRepository.GetMessageThread(context.User.GetUserName(), otherUser);

            if (_uow.HasChanges())
            {
                await _uow.Complete();
            }
            
            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var group = await RemoveFromMessageGroup();

        await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(CreateMessageDto createMessageDto)
    {
        var userName = Context.User.GetUserName();

        if (userName == createMessageDto.RecipientUserName.ToLower())
            throw new HubException("You can not send messages to yourself");

        var sender = await _uow.UserRepository.GetUserByUsername(userName);
        var recipient = await _uow.UserRepository.GetUserByUsername(createMessageDto.RecipientUserName);

        if (recipient == null)
            throw new HubException("Not found user");

        var message = new Message
        {
            Sender = sender,
            Recipient = recipient,
            SenderUserName = sender.UserName,
            RecipientUserName = recipient.UserName,
            Content = createMessageDto.Content
        };
        
        var groupName = GetGroupName(sender.UserName, recipient.UserName);

        var group = await _uow.MessageRepository.GetMessageGroup(groupName);

        if (group.Connections.Any(x => x.UserName == recipient.UserName))
        {
            message.DateRead = DateTime.UtcNow;
        }
        else
        {
            var connections = await PresenceTracker.GetConnectionsForUser(recipient.UserName);
            if (connections != null)
            {
                // send to clients connected with specific connections (one user connected with many devices)
                await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", new
                {
                    sender.UserName,
                    sender.KnownAs
                });
            }
        }
        
        _uow.MessageRepository.AddMessage(message);
        if (await _uow.Complete())
        {
            await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
        }
    }

    private string GetGroupName(string caller, string other)
    {
        return string.Join("-", new[] { caller, other }.OrderBy(x => x));
    }

    private async Task<Group> AddToGroup(string groupName)
    {
        var group = await _uow.MessageRepository.GetMessageGroup(groupName);
        var connection = new Connection(Context.ConnectionId, Context.User.GetUserName());

        if (group == null)
        {
            group = new Group(groupName);
            _uow.MessageRepository.AddGroup(group);
        }
        
        group.Connections.Add(connection);

        if (await _uow.Complete())
            return group;
        
        throw new HubException("Failed to add to group");
    }

    private async Task<Group> RemoveFromMessageGroup()
    {
        var group = await _uow.MessageRepository.GetGroupForConnection(Context.ConnectionId);
        var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
        _uow.MessageRepository.RemoveConnection(connection);
        if (await _uow.Complete())
        {
            return group;
        }

        throw new HubException("Failed to remove from group");
    }
}