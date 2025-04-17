using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OmegaStreamServices.Services
{
    public class RoomStateManager : IRoomStateManager
    {
        public static ConcurrentDictionary<string, RoomState> RoomStates { get; } = new();
        private const int MAX_USER_COUNT_IN_ROOM = 8;

        private bool TryGetRoom(string roomId, out RoomState roomState)
            => RoomStates.TryGetValue(roomId, out roomState);

        private bool TryGetUser(RoomState room, string userId, out User user)
        {
            user = room.Members.FirstOrDefault(u => u.Id == userId);
            return user != null;
        }

        private RoomStateResult Fail(out RoomState? roomState)
        {
            roomState = null;
            return RoomStateResult.Failed;
        }

        /// <summary>
        /// Adds a user to the room. If the room does not exist, it creates a new one.
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="user"></param>
        /// <param name="connectionId"></param>
        /// <param name="roomState"></param>
        /// <returns></returns>
        public RoomStateResult AddUserToRoom(string roomId, User user, string connectionId, out RoomState? roomState)
        {
            roomState = null;
            if (IsContainsUser(user.Id)) return Fail(out roomState);

            try
            {
                if (!TryGetRoom(roomId, out var _roomState))
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

                if (user.Id == _roomState.Host.Id && !_roomState.IsHostInRoom)
                {
                    _roomState.IsHostInRoom = true;
                    _roomState.Members.Add(user);
                    _roomState.HostConnId = connectionId;
                    roomState = _roomState;
                    return RoomStateResult.HostReconected;
                }

                if (_roomState.BannedUsers.Any(x => x.Id == user.Id))
                    return RoomStateResult.Banned;

                if (_roomState.Members.Count < MAX_USER_COUNT_IN_ROOM)
                {
                    _roomState.WaitingForAccept[user.Id] = connectionId;
                    roomState = _roomState;
                    return RoomStateResult.NeedsAproval;
                }

                return RoomStateResult.RoomIsFull;
            }
            catch
            {
                return Fail(out roomState);
            }
        }

        /// <summary>
        /// Rejects a user from the room. If the user is not in the waiting list, it returns false.
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="userId"></param>
        /// <param name="connectionId"></param>
        /// <param name="roomState"></param>
        /// <returns></returns>
        public bool RejectUser(string roomId, string userId, out string? connectionId, out RoomState? roomState)
        {
            connectionId = string.Empty;
            roomState = null;

            if (!TryGetRoom(roomId, out var room) || !room.WaitingForAccept.TryGetValue(userId, out connectionId))
                return false;

            room.WaitingForAccept.Remove(userId);
            roomState = room;
            return true;
        }


        /// <summary>
        /// Removes a user from the room. If the user is the host, it sets the host as not in the room.
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="userId"></param>
        /// <param name="connectionId"></param>
        /// <param name="roomState"></param>
        /// <returns></returns>
        public RoomStateResult RemoveUserFromRoom(string roomId, string userId, string connectionId, out RoomState? roomState)
        {
            roomState = null;
            if (!TryGetRoom(roomId, out var room) || !TryGetUser(room, userId, out var user))
                return Fail(out roomState);

            room.Members.Remove(user);
            if (room.Host.Id == userId)
            {
                room.IsHostInRoom = false;
                room.HostConnId = string.Empty;
            }

            if (!room.Members.Any() && !room.IsHostInRoom)
            {
                RoomStates.TryRemove(roomId, out _);
                return RoomStateResult.RoomClosed;
            }

            roomState = room;
            return room.IsHostInRoom ? RoomStateResult.Accepted : RoomStateResult.HostLeft;
        }

        /// <summary>
        /// Accepts a user from the waiting list. If the user is not in the waiting list, it returns false.
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="user"></param>
        /// <param name="connectionId"></param>
        /// <param name="roomState"></param>
        /// <returns></returns>
        public RoomStateResult AcceptUser(string roomId, User user, out string connectionId, out RoomState? roomState)
        {
            connectionId = string.Empty;
            roomState = null;

            if (!TryGetRoom(roomId, out var room) || !room.WaitingForAccept.TryGetValue(user.Id, out connectionId))
                return RoomStateResult.Failed;

            if (room.Members.Count >= MAX_USER_COUNT_IN_ROOM)
                return RoomStateResult.RoomIsFull;

            room.WaitingForAccept.Remove(user.Id);
            room.Members.Add(user);
            room.UserIdAndConnId[user.Id] = connectionId;

            roomState = room;
            return RoomStateResult.Accepted;
        }

        /// <summary>
        /// Updates the video state in the room. If the room does not exist, it returns false.
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="currentTime"></param>
        /// <param name="isPlaying"></param>
        /// <returns>
        /// Returns true if the video state was updated successfully, otherwise false.
        /// </returns>
        public (bool IsSuccess, string SyncMessage) UpdateVideoState(string roomId, double currentTime, bool isPlaying)
        {
            if (!TryGetRoom(roomId, out var room))
                return (false, string.Empty);

            room.VideoState.CurrentTime = currentTime;
            room.VideoState.IsPlaying = isPlaying;
            room.VideoState.LastUpdated = DateTime.UtcNow;

            return (true, isPlaying ? "Play" : "Pause");
        }

        /// <summary>
        /// Synchronizes the time of the video in the room. If the room does not exist, it returns null.
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="currentTime"></param>
        /// <returns>
        /// Returns the room state if the room exists, otherwise null.
        /// </returns>
        public RoomState SyncTime(string roomId, double currentTime)
        {
            if (TryGetRoom(roomId, out var room))
            {
                room.VideoState.CurrentTime = currentTime;
                room.VideoState.LastUpdated = DateTime.UtcNow;
                return room;
            }
            return null;
        }


        /// <summary>
        /// Saves a message in the room. If the room does not exist, it returns false.
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="sender"></param>
        /// <param name="content"></param>
        /// <param name="message"></param>
        /// <returns>
        /// Returns true if the message was saved successfully, otherwise false.
        /// </returns>
        public bool SaveMessage(string roomId, UserDto sender, string content, out RoomMessage? message)
        {
            message = null;
            if (!TryGetRoom(roomId, out var room))
                return false;

            var newMessage = new RoomMessage
            {
                Sender = sender,
                Content = content
            };

            message = newMessage;
            room.RoomMessages.Add(newMessage);
            return true;
        }

        /// <summary>
        /// Bans a user from the room. If the user is not in the room, it returns false.
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="userId"></param>
        /// <param name="connId"></param>
        /// <param name="members"></param>
        /// <returns>
        /// Returns true if the user was banned successfully, otherwise false.
        /// </returns>
        public bool BanUser(string roomId, string userId, out string? connId, out List<User>? members)
        {
            connId = null;
            members = null;

            if (!TryGetRoom(roomId, out var room) || !room.UserIdAndConnId.TryGetValue(userId, out connId))
                return false;

            var user = room.Members.FirstOrDefault(x => x.Id == userId);
            if (user == null)
                return false;

            room.Members.Remove(user);
            room.BannedUsers.Add(user);
            room.UserIdAndConnId.Remove(userId);

            members = room.Members;
            return true;
        }

        /// <summary>
        /// Adds a video to the playlist. If the room does not exist, it returns false.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room to which the video will be added.
        /// </param>
        /// <param name="video">
        /// The video to be added to the playlist.
        /// </param>
        /// <param name="playList">
        /// The updated playlist after adding the video.
        /// </param>
        /// <returns>
        /// Returns true if the video was added successfully, otherwise false.
        /// </returns>
        public bool AddVideoToPlaylist(string roomId, VideoDto video, out List<VideoDto>? playList)
        {
            playList = null;
            if (!TryGetRoom(roomId, out var room))
                return false;

            room.PlayList.Add(video);
            playList = room.PlayList;
            return true;
        }


        /// <summary>
        /// Starts a video in the room. If the room does not exist, it returns false.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room in which the video will be started.
        /// </param>
        /// <param name="video">
        /// The video to be started.
        /// </param>
        /// <returns>
        /// Returns true if the video was started successfully, otherwise false.
        /// </returns>
        public bool StartVideo(string roomId, Video video)
        {
            if (!TryGetRoom(roomId, out var room))
                return false;

            if (room.PlayList.Any(x => x.Id == video.Id))
            {
                room.CurrentVideoId = video.Id;
                return true;
            }

            return false;
        }


        /// <summary>
        /// Removes a video from the playlist. If the room does not exist, it returns false.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room from which the video will be removed.
        /// </param>
        /// <param name="videoId">
        /// The ID of the video to be removed from the playlist.
        /// </param>
        /// <param name="playList">
        /// The updated playlist after removing the video.
        /// </param>
        /// <returns>
        /// Returns true if the video was removed successfully, otherwise false.
        /// </returns>
        public bool RemoveVideoFromPlayList(string roomId, int videoId, out List<VideoDto>? playList)
        {
            playList = null;
            if (!TryGetRoom(roomId, out var room))
                return false;

            var video = room.PlayList.FirstOrDefault(x => x.Id == videoId);
            if (video == null)
                return false;

            room.PlayList.Remove(video);
            playList = room.PlayList;
            return true;
        }

        /// <summary>
        /// Plays the next video in the playlist. If the room does not exist, it returns null.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room in which the next video will be played.
        /// </param>
        /// <returns>
        /// Returns the next video in the playlist if it exists, otherwise null.
        /// </returns>
        public VideoDto? PlayNextVideo(string roomId)
        {
            if (TryGetRoom(roomId, out var room))
            {
                return room.GetNextVideoLoop();
            }
            return null;
        }

        public bool IsRoomExist(string roomId)
            => TryGetRoom(roomId, out _);

        public bool RemoveRoom(string roomId)
            => RoomStates.TryRemove(roomId, out _);

        public RoomState? GetRoomState(string roomId)
            => TryGetRoom(roomId, out var room) ? room : null;

        private bool IsContainsUser(string userId)
            => RoomStates.Values.Any(room => room.Members.Any(member => member.Id == userId));


        /// <summary>
        /// Removes a user from the room by user ID. If the user is not found, it returns false.
        /// </summary>
        /// <param name="userId">
        /// The ID of the user to be removed.
        /// </param>
        /// <param name="roomId">
        /// The ID of the room from which the user will be removed.
        /// </param>
        /// <param name="members">
        /// The list of members in the room after removing the user.
        /// </param>
        /// <returns>
        /// Returns true if the user was removed successfully, otherwise false.
        /// </returns>
        public RoomStateResult RemoveUserByUserId(string userId, out string? roomId, out List<User>? members)
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
                        if (room.Value.Host.Id == userId)
                        {
                            room.Value.HostConnId = string.Empty;
                            room.Value.IsHostInRoom = false;
                            RoomStates.Remove(room.Key, out _);
                            return RoomStateResult.HostLeft;
                        }
                        else
                        {
                            room.Value.Members.Remove(user);
                            room.Value.UserIdAndConnId.Remove(userId);
                            return RoomStateResult.Accepted;
                        }
                    }
                }
            }
            return RoomStateResult.Failed;
        }
    }
}
