using OmegaStreamServices.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services
{
    public class RoomStateManager: IRoomStateManager
    {
        public static ConcurrentDictionary<string, RoomState> RoomStates = new();
    }
}
