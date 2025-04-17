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
        RoomStateResult AddUserToRoom(string roomId, User user, string connectionId, out RoomState? roomState);
        RoomStateResult RemoveUserFromRoom(string roomId, string userId, string ConnectionId, out RoomState? roomState);
        bool RejectUser(string roomId, string userId, out string? connectionId, out RoomState? roomState);
        RoomStateResult AcceptUser(string roomId, User user, out string connectionId, out RoomState? roomState);
        (bool IsSuccess, string SyncMessage) UpdateVideoState(string roomId, double currentTime, bool isPlaying);
        RoomState SyncTime(string roomId, double currentTime);
        bool SaveMessage(string roomId, UserDto sender, string content, out RoomMessage? message);
        bool BanUser(string roomId, string userId, out string? connId, out List<User>? members);
        bool AddVideoToPlaylist(string roomId, VideoDto video, out List<VideoDto>? playList);
        bool StartVideo(string roomId, Video video);
        bool RemoveVideoFromPlayList(string roomId, int videoId, out List<VideoDto>? playList);
        VideoDto? PlayNextVideo(string roomId);
        bool IsRoomExist(string roomId);
        bool RemoveRoom(string roomId);
        RoomState? GetRoomState(string roomId);
        RoomStateResult RemoveUserByUserId(string userId, out string? roomId, out List<User>? members);
    }
}
