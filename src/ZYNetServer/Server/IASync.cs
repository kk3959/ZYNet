﻿using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using ZYNet.CloudSystem.Frame;
using ZYSocket.AsyncSend;

namespace ZYNet.CloudSystem.Server
{
    public interface IASync
    {
        SocketAsyncEventArgs Asyn { get; }
        CloudServer CurrentServer { get; }
        bool IsValidate { get; set; }
        AsyncSend Sendobj { get; }
        object UserToken { get; set; }

        Action<ASyncToken, string> UserDisconnect { get; set; }     
        void Disconnect();
        T Get<T>();
        T GetForEmit<T>();
        AsyncCalls MakeAsync(AsyncCalls async);
        Result Res(params object[] args);
        Task<Result> ResTask(params object[] args);
        T Token<T>();
        ASyncToken GetAsyncToken();
    }
}