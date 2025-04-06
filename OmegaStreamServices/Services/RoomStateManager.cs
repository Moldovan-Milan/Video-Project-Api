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

        public RoomStateResult AddUserToRoom(string roomId, User user, string connectionId, out RoomState? roomState)
        {
            roomState = null;
            if (!IsContainsUser(user.Id))
            {
                try
                {
                    if (!RoomStates.TryGetValue(roomId, out var _roomState))
                    {
                        roomState = RoomStates.GetOrAdd(roomId, _ => new RoomState
                        {
                            Host = user,
                            IsHostInRoom = true,
                            HostConnId = connectionId,
                            Members = new List<User> { user },
                        });

                        return RoomStateResult.Created;
                    }
                    else if (user.Id == _roomState.Host.Id && !_roomState.IsHostInRoom)
                    {
                        _roomState.IsHostInRoom = true;
                        _roomState.Members.Add(user);
                        _roomState.HostConnId = connectionId;
                        roomState = _roomState;
                        return RoomStateResult.HostReconected;
                    }
                    else
                    {
                        if (_roomState.BannedUsers.Any(x => x.Id == user.Id))
                        {
                            return RoomStateResult.Banned;
                        }
                        if (_roomState.Members.Count < MAX_USER_COUNT_IN_ROOM)
                        {
                            _roomState.WaitingForAccept[user.Id] = connectionId;
                            roomState = _roomState;
                            return RoomStateResult.NeedsAproval;
                        }
                        else
                        {
                            return RoomStateResult.RoomIsFull;
                        }
                    }
                }
                catch
                {
                    return RoomStateResult.Failed;
                }
            }
            return RoomStateResult.Failed;
        }

        public bool RejectUser(string roomId, string userId, out string? connectionId, out RoomState? roomState)
        {
            connectionId = string.Empty;
            roomState = null;

            if (!RoomStates.TryGetValue(roomId, out var _roomState))
            {
                return false;
            }

            if (!_roomState.WaitingForAccept.TryGetValue(userId, out string? connId))
            {
                return false;
            }

            connectionId = connId;
            _roomState.WaitingForAccept.Remove(userId);
            roomState = _roomState;
            return true;
        }

        public RoomStateResult RemoveUserFromRoom(string roomId, string userId, string connectionId, out RoomState? roomState)
        {
            roomState = null;
            if (!RoomStates.TryGetValue(roomId, out var _roomState))
            {
                return RoomStateResult.Failed;
            }

            User? user = _roomState.Members.FirstOrDefault(x => x.Id == userId);
            if (user == null)
            {
                return RoomStateResult.Failed;
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
                return RoomStateResult.RoomClosed;
            }

            if (!_roomState.IsHostInRoom)
            {
                roomState = _roomState;
                return RoomStateResult.HostLeft;
            }

            roomState = _roomState;
            return RoomStateResult.Accepted;
        }

        public RoomStateResult AcceptUser(string roomId, User user, out string connectionId, out RoomState? roomState)
        {
            connectionId = string.Empty;
            roomState = null;

            if (!RoomStates.TryGetValue(roomId, out var _roomState))
            {
                return RoomStateResult.Failed;
            }

            roomState = _roomState;

            if (!_roomState.WaitingForAccept.TryGetValue(user.Id, out string? connId))
            {
                return RoomStateResult.Failed;
            }

            if (_roomState.Members.Count >= MAX_USER_COUNT_IN_ROOM)
            {
                return RoomStateResult.RoomIsFull;
            }

            connectionId = connId;
            _roomState.WaitingForAccept.Remove(user.Id);
            _roomState.Members.Add(user);
            _roomState.UserIdAndConnId[user.Id] = connId;

            return RoomStateResult.Accepted;
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
            if (!roomState.UserIdAndConnId.TryGetValue(userId, out connId))
            {
                return false;
            }
            var user = roomState.Members.FirstOrDefault(x => x.Id == userId);
            if (user == null)
            {
                return false;
            }

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

        public VideoDto? PlayNextVideo(string roomId)
        {
            if (!RoomStates.TryGetValue(roomId, out var roomState))
            {
                return null;
            }

            return roomState.GetNextVideoLoop();
        }

        public bool IsRoomExist(string roomId)
        {
            if (!RoomStates.TryGetValue(roomId, out var roomState))
            {
                return false;
            }

            return true;
        }

        public bool RemoveRoom(string roomId)
        {
            return RoomStates.TryRemove(roomId, out _);
        }

        public RoomState? GetRoomState(string roomId)
        {
            return RoomStates.TryGetValue(roomId, out var roomState) ? roomState : null;
        }
        
        private bool IsContainsUser(string userId)
        {
            return RoomStates.Values.Any(room => room.Members.Any(member => member.Id == userId));
        }

        public bool RemoveUserByUserId(string userId, out string? roomId, out List<User>? members)
        {
            roomId = null;
            members = null;

            foreach (var room in RoomStates)
            {
                if (room.Value.Members.Any(x => x.Id == userId))
                {
                    roomId = room.Key;
                    members = room.Value.Members;
                    var user = room.Value.Members.FirstOrDefault(x => x.Id == userId);
                    if (user != null)
                    {
                        room.Value.Members.Remove(user);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
