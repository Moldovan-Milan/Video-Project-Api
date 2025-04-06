using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public class UserThemeRepository : BaseRepository<UserTheme>, IUserThemeRepository
    {
        public UserThemeRepository(AppDbContext context) : base(context)
        {
        }
    }
}
