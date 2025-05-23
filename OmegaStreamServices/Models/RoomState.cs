﻿using OmegaStreamServices.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public class RoomState
    {
        public User Host { get; set; } = new();
        public string HostConnId { get; set; } = string.Empty;
        public bool IsHostInRoom { get; set; } = false;
        public List<User> Members { get; set; } = new();
        public Dictionary<string, string> UserIdAndConnId { get; set; } = new();
        public Dictionary<string, string> WaitingForAccept { get; set; } = new();
        public List<User> BannedUsers { get; set; } = new();
        public List<RoomMessage> RoomMessages { get; set; } = new();
        public List<VideoDto> PlayList { get; set; } = new();
        public int CurrentVideoId { get; set; }
        public VideoState VideoState { get; set; } = new VideoState();

        public VideoDto? GetNextVideoLoop()
        {
            if (PlayList == null || PlayList.Count == 0)
                return null;

            int currentIndex = PlayList.FindIndex(v => v.Id == CurrentVideoId);

            // Ha az utolsó videónál járunk, akkor visszatérünk az elsőhöz
            int nextIndex = (currentIndex + 1) % PlayList.Count;

            return PlayList[nextIndex];
        }

    }
}
