﻿using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public interface IVideoRepository: IBaseRepository<Video>
    {
        Task<List<Video>> GetAllVideosWithIncludes();
        Task<Video> GetVideoWithInclude(int id);
    }
}