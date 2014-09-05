using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ.Sockets;

namespace NetMQActors
{
    /// <summary>
    /// Simple interface that all shims should implement
    /// </summary>
    public interface IShimHandler
    {
        void Run(PairSocket shim, object[] args, CancellationToken token);
    }
}
