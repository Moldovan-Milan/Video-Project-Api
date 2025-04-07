using OmegaStreamServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public interface IUserVideoUploadRepositroy
    {
        Task AddUserVideoUpload(UserVideoUpload userVideoUpload);
        Task<List<UserVideoUpload>> GetAllUserVideoUploads();
        Task<UserVideoUpload?> GetUserVideoUpload(string userId);
        Task<UserVideoUpload?> GetUserVideoUploadByName(string videoName);
        Task RemoveUserVideoUpload(string userId);
        Task RemoveUserVideoUploadByName(string videoName);
    }
}
