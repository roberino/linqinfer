﻿using System.Collections.Generic;
using System.Threading.Tasks;
using LinqInfer.Maths;
using System;
using System.IO;

namespace LinqInfer.Data.Remoting
{
    public interface ITransferHandle
    {
        string Id { get; }
        string ClientId { get; }
        string OperationType { get; }
        Task Send(BinaryVectorDocument doc);
        Task Send(IEnumerable<ColumnVector1D> data);
        Task<Stream> End(Uri forwardResponseTo = null);
    }
}