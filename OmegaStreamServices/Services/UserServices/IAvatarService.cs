using OmegaStreamServices.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.UserServices
{
    public interface IAvatarService
    {
        Task<string> SaveAvatarAsync(Stream avatarStream);
        Task<(Stream file, string contentType)> GetAvatarAsync(int avatarId);

    }
}
