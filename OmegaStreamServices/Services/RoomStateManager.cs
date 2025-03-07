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
    public class RoomStateManager : IRoomStateManager
    {
        public static ConcurrentDictionary<string, RoomState> RoomStates { get; } = new();
        private const int MAX_USER_COUNT_IN_ROOM = 8;

        public Task<AddUserToRoomResult> AddUserToRoom(string roomId, User user, string connectionId, out RoomState? roomState)
        {
            roomState = null;
            try
            {
                if (!RoomStates.TryGetValue(roomId, out var _roomState))
                {
                    roomState = new RoomState
                    {
                        Host = user,
                        IsHostInRoom = true,
                        HostConnId = connectionId,
                        Members = new List<User> { user },
                    };
                    RoomStates[roomId] = roomState;
                    return Task.FromResult(AddUserToRoomResult.Created);
                }
                else if (user.Id == _roomState.Host.Id && !_roomState.IsHostInRoom)
                {
                    _roomState.IsHostInRoom = true;
                    _roomState.Members.Add(user);
                    _roomState.HostConnId = connectionId;
                    roomState = _roomState;
                    return Task.FromResult(AddUserToRoomResult.HostReconected);
                }
                else
                {
                    if (_roomState.Members.Count < MAX_USER_COUNT_IN_ROOM)
                    {
                        _roomState.WaitingForAccept[user.Id] = connectionId;
                        roomState = _roomState;
                        return Task.FromResult(AddUserToRoomResult.NeedsAproval);
                    }
                    else
                    {
                        return Task.FromResult(AddUserToRoomResult.RoomIsFull);
                    }
                }
            }
            catch
            {
                return Task.FromResult(AddUserToRoomResult.Failed);
            }
        }

        public Task<bool> RejectUser(string roomId, string userId, out string? connectionId, out RoomState? roomState)
        {
            connectionId = string.Empty;
            roomState = null;

            if (!RoomStates.TryGetValue(roomId, out var _roomState))
            {
                return Task.FromResult(false);
            }

            if (!_roomState.WaitingForAccept.TryGetValue(userId, out string? connId))
            {
                return Task.FromResult(false);
            }

            connectionId = connId;
            _roomState.WaitingForAccept.Remove(userId);
            roomState = _roomState;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveUserFromRoom(string roomId, string userId, string connectionId, out RoomState? roomState)
        {
            roomState = null;
            if (!RoomStates.TryGetValue(roomId, out var _roomState))
            {
                return Task.FromResult(false);
            }

            User? user = _roomState.Members.FirstOrDefault(x => x.Id == userId);
            if (user == null)
            {
                return Task.FromResult(false);
            }

            _roomState.Members.Remove(user);

            if (_roomState.Host.Id == userId)
            {
                _roomState.IsHostInRoom = false;
                _roomState.HostConnId = string.Empty;
            }

            if (_roomState.Members.Count == 0 && !_roomState.IsHostInRoom)
            {
                RoomStates.TryRemove(roomId, out _);
                return Task.FromResult(true);
            }

            roomState = _roomState;
            return Task.FromResult(true);
        }

        public Task<AddUserToRoomResult> AcceptUser(string roomId, User user, out string connectionId, out RoomState? roomState)
        {
            connectionId = string.Empty;
            roomState = null;

            if (!RoomStates.TryGetValue(roomId, out var _roomState))
            {
                return Task.FromResult(AddUserToRoomResult.Failed);
            }

            roomState = _roomState;

            if (!_roomState.WaitingForAccept.TryGetValue(user.Id, out string? connId))
            {
                return Task.FromResult(AddUserToRoomResult.Failed);
            }

            if (_roomState.Members.Count >= MAX_USER_COUNT_IN_ROOM)
            {
                return Task.FromResult(AddUserToRoomResult.RoomIsFull);
            }

            connectionId = connId;
            _roomState.WaitingForAccept.Remove(user.Id);
            _roomState.Members.Add(user);

            return Task.FromResult(AddUserToRoomResult.Accepted);
        }

        public (bool IsSuccess, string SyncMessage) UpdateVideoState(string roomId, double currentTime, bool isPlaying)
        {
            if (!RoomStates.TryGetValue(roomId, out var _roomState))
            {
                return (false, string.Empty);
            }
            _roomState.VideoState.CurrentTime = currentTime;
            _roomState.VideoState.IsPlaying = isPlaying;
            _roomState.VideoState.LastUpdated = DateTime.UtcNow;

            return (true, isPlaying ? "Play" : "Pause");
        }

        public RoomState SyncTime(string roomId, double currentTime)
        {
            if (RoomStates.TryGetValue(roomId, out var roomState))
            {
                roomState.VideoState.CurrentTime = currentTime;
                roomState.VideoState.LastUpdated = DateTime.UtcNow;
                return roomState;
            }
            return null;
        }
    }
}
