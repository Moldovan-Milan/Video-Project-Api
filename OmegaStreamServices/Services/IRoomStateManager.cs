using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services
{
    public interface IRoomStateManager
    {
        Task<AddUserToRoomResult> AddUserToRoom(string roomId, User user, string connectionId, out RoomState? roomState);
        Task<bool> RemoveUserFromRoom(string roomId, string userId, string ConnectionId, out RoomState? roomState);
        Task<bool> RejectUser(string roomId, string userId, out string? connectionId, out RoomState? roomState);
        Task<AddUserToRoomResult> AcceptUser(string roomId, User user, out string connectionId, out RoomState? roomState);
        (bool IsSuccess, string SyncMessage) UpdateVideoState(string roomId, double currentTime, bool isPlaying);
        RoomState SyncTime(string roomId, double currentTime);
        bool SaveMessage(string roomId, UserDto sender, string content, out RoomMessage? message);
        List<RoomMessage>? GetHistory(string roomId);
    }
}
