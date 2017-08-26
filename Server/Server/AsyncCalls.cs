﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ZYNet.CloudSystem.Frame;
using ZYSocket.share;


namespace ZYNet.CloudSystem.Server
{
    public class AsyncCalls
    {
        public ReturnResult Result { get; private set; }

        public ASyncToken AsyncUser { get; private set; }

        public bool IsOver { get; private set; }
        public bool IsError { get; private set; }
        public Exception Error { get; private set; }

        internal event Action<ReturnResult> Complete;

        internal event Action<byte[]> CallSend;

        public Fiber fiber { get; private set; }

        public bool IsHaveReturn { get; private set; }

        public MethodInfo Method { get; private set; }

        public object[] Args { get; private set; }

        public long Id { get; private set; }

        public int Cmd { get; private set; }

        public object UserToken
        {
            get { return AsyncUser?.UserToken; }
            set { if (AsyncUser != null) AsyncUser.UserToken = value; }
        }

        public bool IsValidate
        {
            get { return AsyncUser.IsValidate; }
            set { if (AsyncUser != null) AsyncUser.IsValidate = value; }
        }

        public T Token<T>()
        {
            return AsyncUser.Token<T>();
        }


        public CloudServer CurrentServer => AsyncUser?.CurrentServer;


        public AsyncCalls(ASyncToken token,Fiber fiber)
        {
            this.AsyncUser = token;
            this.fiber = fiber;
        }

        ~AsyncCalls()
        {
            fiber?.Dispose();
        }


        public AsyncCalls(long id,int cmd,ASyncToken token,MethodInfo method,object[] args,bool ishavereturn)
        {
            IsHaveReturn = ishavereturn;
            Method = method;
            Args = args;
            AsyncUser = token;
            Id = id;
        }

        public void Run()
        {

            Func<Task> wrappedGhostThreadFunction = async () =>
            {
                try
                {
                    if (IsHaveReturn)
                    {
                        Result = await (Task<ReturnResult>)Method.Invoke(null, Args);

                        if (Complete != null)
                            Complete(Result);
                    }
                    else
                    {
                        await (Task)Method.Invoke(null, Args);
                    }

                }
                catch (Exception er)
                {
                    IsError = true;
                    Error = er;

                    if (IsHaveReturn)
                    {
                        var nullx = new ReturnResult();
                        nullx.Id = this.Id;
                      
                        nullx.ErrorMsg = er.ToString();                     
                        nullx.ErrorId = er.HResult;
                        if (Complete != null)
                            Complete(nullx);
                    }

                    LogAction.Log(LogType.Err, "Cmd:" + Cmd + " Error:\r\n" + Error.ToString());

                }
                finally
                {
                    IsOver = true;
                    AsyncUser.RemoveAsyncCall(Id);
                }
            };


            fiber = new Fiber();
            fiber.SetAction(wrappedGhostThreadFunction);
            fiber.Start();

        }

        #if !Xamarin

        public T Get<T>()
        {
            var tmp = DispatchProxy.Create<T, SyncProxy>();
            var proxy = tmp as SyncProxy;
            proxy.Call = Call;
            return tmp;
        }


        protected virtual object Call(MethodInfo method, object[] args)
        {

            var attr = method.GetCustomAttribute(typeof(MethodCmdTag), true);

            if (attr == null)
            {
                throw new FormatException(method.Name + " Is Not MethodRun Attribute");
            }


            MethodCmdTag run = attr as MethodCmdTag;

            if (run != null)
            {
                int cmd = run.CmdTag;

                if (method.ReturnType != typeof(void))
                {


                    if (!Common.IsTypeOfBaseTypeIs(method.ReturnType, typeof(FiberThreadAwaiterBase)))
                    {
                        throw new Exception(string.Format("Async Call Not Use Sync Mehhod"));
                    }
                    else
                    {
                        return CR(cmd, args);
                    }
                }
                else
                {
                    CV(cmd, args);

                    return null;
                }

            }
            else
                return null;
        }

#endif

        /// <summary>
        /// CALL VOID
        /// </summary>
        /// <param name="cmdTag"></param>
        /// <param name="args"></param>
        public void CV(int cmdTag, params object[] args)
        {
            AsyncUser.CV(cmdTag, args);
        }

        /// <summary>
        /// CALL RETURN
        /// </summary>
        /// <param name="cmdTag"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public ResultAwatier CR(int cmdTag, params object[] args)
        {
            CallPack buffer = new CallPack()
            {
                Id = Common.MakeID,
                CmdTag = cmdTag,
                Arguments = new List<byte[]>(args.Length)
            };

            foreach (var item in args)
            {
                Type type = item.GetType();

                buffer.Arguments.Add(Serialization.PackSingleObject(type, item));

            }


            using (MemoryStream stream = new MemoryStream())
            {

                BinaryWriter bufflist = new BinaryWriter(stream);

                if (AsyncUser.dataExtra != null)
                {

                    bufflist.Write(CmdDef.CallCmd);
                    byte[] classdata = BufferFormat.SerializeObject(buffer);
                    bufflist.Write(classdata.Length);
                    bufflist.Write(classdata);

                    byte[] fdata = AsyncUser.dataExtra(stream.ToArray());

                    stream.Position = 0;
                    stream.SetLength(0);
                    bufflist.Write(0);
                    bufflist.Write(fdata);
                }
                else
                {
                    bufflist.Write(0);
                    bufflist.Write(CmdDef.CallCmd);
                    byte[] classdata = BufferFormat.SerializeObject(buffer);
                    bufflist.Write(classdata.Length);
                    bufflist.Write(classdata);

                }

                int l = (int)(stream.Length);

                byte[] data = BufferFormat.GetSocketBytes(l);

                stream.Position = 0;

                bufflist.Write(data);


                byte[] pdata = stream.ToArray();
#if !COREFX
                stream.Close();
#endif


                AsyncUser.AddAsyncCallBack(this, buffer.Id);

                if (CallSend != null)
                    CallSend(pdata);
            }

            return fiber.Read();
        }


        public void SetRet(ReturnResult result)
        {
            fiber.Set(result);
        }

        public ReturnResult RET(params object[] args)
        {
            ReturnResult tmp = new ReturnResult(args);
            tmp.Id = this.Id;
            return tmp;
        }

        public void Disconnect()
        {           
            if (AsyncUser != null)
                AsyncUser.Disconnect();
        }
    }
}
