using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public enum RoomStateResult
    {
       Created,
       Accepted,
       Rejected,
       NeedsAproval,
       Banned,
       HostReconected,
       RoomIsFull,
       Failed
    }
}
