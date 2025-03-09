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

        public Task<RoomStateResult> AddUserToRoom(string roomId, User user, string connectionId, out RoomState? roomState)
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
                    return Task.FromResult(RoomStateResult.Created);
                }
                else if (user.Id == _roomState.Host.Id && !_roomState.IsHostInRoom)
                {
                    _roomState.IsHostInRoom = true;
                    _roomState.Members.Add(user);
                    _roomState.HostConnId = connectionId;
                    roomState = _roomState;
                    return Task.FromResult(RoomStateResult.HostReconected);
                }
                else
                {
                    if (_roomState.BannedUsers.Where(x => x.Id == user.Id).Any())
                    {
                        return Task.FromResult(RoomStateResult.Banned);
                    }
                    if (_roomState.Members.Count < MAX_USER_COUNT_IN_ROOM)
                    {
                        _roomState.WaitingForAccept[user.Id] = connectionId;
                        roomState = _roomState;
                        return Task.FromResult(RoomStateResult.NeedsAproval);
                    }
                    else
                    {
                        return Task.FromResult(RoomStateResult.RoomIsFull);
                    }
                }
            }
            catch
            {
                return Task.FromResult(RoomStateResult.Failed);
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

        public Task<RoomStateResult> AcceptUser(string roomId, User user, out string connectionId, out RoomState? roomState)
        {
            connectionId = string.Empty;
            roomState = null;

            if (!RoomStates.TryGetValue(roomId, out var _roomState))
            {
                return Task.FromResult(RoomStateResult.Failed);
            }

            roomState = _roomState;

            if (!_roomState.WaitingForAccept.TryGetValue(user.Id, out string? connId))
            {
                return Task.FromResult(RoomStateResult.Failed);
            }

            if (_roomState.Members.Count >= MAX_USER_COUNT_IN_ROOM)
            {
                return Task.FromResult(RoomStateResult.RoomIsFull);
            }

            connectionId = connId;
            _roomState.WaitingForAccept.Remove(user.Id);
            _roomState.Members.Add(user);
            _roomState.UserIdAndConnId[user.Id] = connId;

            return Task.FromResult(RoomStateResult.Accepted);
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

        public bool SaveMessage(string roomId, UserDto sender, string content, out RoomMessage? message)
        {
            message = null;
            if (!RoomStates.TryGetValue(roomId, out var roomState))
                return false;

            RoomMessage newMessage = new RoomMessage
            {
                Sender = sender,
                Content = content
            };
            
            message = newMessage;
            roomState.RoomMessages.Add(newMessage);
            return true;
            
        }

        public bool BanUser(string roomId, string userId, out string? connId, out List<User>? members)
        {
            connId = null;
            members = null;
            if (!RoomStates.TryGetValue(roomId, out var roomState))
                return false;
            User? user = roomState.Members.FirstOrDefault(x => x.Id == userId);
            if (user == null)
                return false;

            connId = roomState.UserIdAndConnId[userId];

            roomState.Members.Remove(user);
            roomState.BannedUsers.Add(user);
            roomState.UserIdAndConnId.Remove(userId);

            members = roomState.Members;
            return true;
        }

        public bool AddVideoToPlaylist(string roomId, VideoDto video, out List<VideoDto>? playList)
        {
            playList = null;
            if (!RoomStates.TryGetValue(roomId, out var roomState))
            {
                return false;
            }
            roomState.PlayList.Add(video);
            playList = roomState.PlayList;
            return true;
        }

        public bool StartVideo(string roomId, Video video)
        {
            if (!RoomStates.TryGetValue(roomId, out var roomState))
                return false;
            if (roomState.PlayList.Where(x => x.Id == video.Id).Any())
            {
                roomState.CurrentVideoId = video.Id;
                return true;
            }
            return false;
        }

        public bool RemoveVideoFromPlayList(string roomId, int videoId, out List<VideoDto>? playList)
        {
            playList = null;
            if (!RoomStates.TryGetValue(roomId, out var roomState))
            {
                return false;
            }
            var video = roomState.PlayList.FirstOrDefault(x => x.Id == videoId);
            if (video == null)
                return false;
            roomState.PlayList.Remove(video);
            playList = roomState.PlayList;
            return true;
        }
    }
}
