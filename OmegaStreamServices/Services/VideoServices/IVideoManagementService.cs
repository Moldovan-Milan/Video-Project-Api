using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public interface IVideoManagementService
    {
        public Task DeleteVideoWithAllRelations(int id);
        public Task EditVideo(int id, string? title, string? description/*, IFormFile? image*/);
    }
}
