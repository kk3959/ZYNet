﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZYNet.CloudSystem;
using ZYNet.CloudSystem.Frame;
using ZYNet.CloudSystem.Server;

namespace TestServer
{
  
    /// <summary>
    /// 服务器包处理器
    /// </summary>
    public static partial class PackHandler
    {

        public static List<UserInfo> UserList = new List<UserInfo>();


        [MethodRun(1000)]
        public static bool IsLogOn(ASyncToken token, string username, string password)
        {
            UserInfo tmp = new UserInfo()
            {
                UserName=username,
                PassWord=password,
                token=token
            };

            token.UserToken = tmp;
            token.UserDisconnect += Token_UserDisconnect;
            lock (UserList)
            {
                UserList.Add(tmp);
            }

            

            return true;

        }

        [MethodRun(2001)]
        public static async Task<ReturnResult> StartDown(AsyncCalls async, string url)
        {

            var callback = await async.CR(2001, url);

            if (callback != null && callback.ErrorId == 0)
            {
                var htmldata = callback?[0]?.Value<byte[]>();

                if (htmldata != null)
                {

                    string html = Encoding.UTF8.GetString(htmldata);

                    return async.RET(html);

                }
            }
            else
            {
                Console.WriteLine(callback.ErrorMsg);
            }

            return async.RET();// or async.RET(null);
            
        }



        [MethodRun(2002)]
        public static Task<ReturnResult> GetTime(AsyncCalls async)
        {           
            return Task.FromResult<ReturnResult>(async.RET(DateTime.Now));
        }

        [MethodRun(2003)]
        public static void SetPassWord(ASyncToken token,string password)
        {
            UserInfo user= token.UserToken as UserInfo;

            if (user != null)
            {
                user.PassWord = password;
                Console.WriteLine(user.UserName + " Set PassWord:" + password);
               
            }

           
        }

        [MethodRun(2500)]
        public static async Task<ReturnResult> TestRec(AsyncCalls async,int count)
        {
            count--;
            if (count > 1)
            {
                var x = (await async.CR(2500, count))?[0]?.Value<int>();

                if(x!=null&&x.HasValue)
                {
                    count = x.Value;
                }
            }
            
            return async.RET(count);
        }


        [MethodRun(2600)]
        public static  Task<ReturnResult> TestError(AsyncCalls async, int c)
        {
            throw new Exception("EEEE");
        }



        /// <summary>
        /// USER DISCONNECT
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private static void Token_UserDisconnect(ASyncToken arg1, string arg2)
        {
           if(arg1.UserToken!=null)
           {
                UserInfo user = arg1.UserToken as UserInfo;

                if(user!=null)
                {
                    lock (UserList)
                    {
                        UserList.Remove(user);
                    }
                }
           }
        }


    }
}
