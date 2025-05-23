﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Dto
{
    public class LiveStreamDto
    {
        public string Id { get; set; }
        public UserDto User { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string StreamTitle { get; set; }
        public string Description { get; set; }
        public int Viewers { get; set; }
    }

}
