using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public interface IRefreshTokenRepository: IBaseRepository<RefreshToken>
    {
        Task<RefreshToken> GetByUserId(string userId);
        Task<RefreshToken> GetByToken(string token);
    }
}
