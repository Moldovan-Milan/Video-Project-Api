using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Models
{
    public enum AddUserToRoomResult
    {
       Created,
       Accepted,
       Rejected,
       NeedsAproval,
       HostReconected,
       RoomIsFull,
       Failed
    }
}
