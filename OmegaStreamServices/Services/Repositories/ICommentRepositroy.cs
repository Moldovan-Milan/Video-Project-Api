﻿using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public interface ICommentRepositroy: IBaseRepository<Comment>
    {
        Task<List<Comment>> GetAllCommentsByVideo(int videoId);
    }
}
