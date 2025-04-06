using OmegaStreamServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public class UserVideoUploadRepository : IUserVideoUploadRepositroy
    {
        private readonly static List<UserVideoUpload> _userVideoUploads = new List<UserVideoUpload>();
        private readonly object _lock = new object();

        public Task AddUserVideoUpload(UserVideoUpload userVideoUpload)
        {
            lock (_lock)
            {
                if (_userVideoUploads.Contains(userVideoUpload))
                {
                    return Task.CompletedTask;
                }
                _userVideoUploads.Add(userVideoUpload);
            }
            return Task.CompletedTask;
        }

        public Task<List<UserVideoUpload>> GetAllUserVideoUploads()
        {
            lock (_lock)
            {
                return Task.FromResult(new List<UserVideoUpload>(_userVideoUploads));
            }
        }

        public Task<UserVideoUpload?> GetUserVideoUpload(string userId)
        {
            return Task.FromResult(_userVideoUploads.FirstOrDefault(u => u.UserId == userId));
        }

        public Task<UserVideoUpload?> GetUserVideoUploadByName(string videoName)
        {
            return Task.FromResult(_userVideoUploads.FirstOrDefault(u => u.VideoName == videoName));
        }

        public Task RemoveUserVideoUpload(string userId)
        {
            lock (_lock)
            {
                var userVideoUpload = _userVideoUploads.FirstOrDefault(u => u.UserId == userId);
                if (userVideoUpload != null)
                {
                    _userVideoUploads.Remove(userVideoUpload);
                }
            }
            return Task.CompletedTask;
        }

        public Task RemoveUserVideoUploadByName(string videoName)
        {
            lock (_lock)
            {
                var userVideoUpload = _userVideoUploads.FirstOrDefault(u => u.VideoName == videoName);
                if (userVideoUpload != null)
                {
                    _userVideoUploads.Remove(userVideoUpload);
                }
            }
            return Task.CompletedTask;
        }
    }
}
