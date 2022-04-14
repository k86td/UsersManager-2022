﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UsersManager.Models
{
    public class UserLastAccess
    {
        public int UserId { get; set; }
        public DateTime LastAccess { get; set; }
        public UserLastAccess(int userId)
        {
            UserId = userId;
            LastAccess = DateTime.Now;
        }
    }
    public static class OnlineUsers
    {
        private static readonly int TimeOut = 40; // minutes
        public static List<int> UsersId
        {
            get
            {
                if (HttpRuntime.Cache["OnLineUsers"] == null)
                    HttpRuntime.Cache["OnLineUsers"] = new List<int>();
                return (List<int>)HttpRuntime.Cache["OnLineUsers"];
            }
        }
        public static List<UserLastAccess> LastUsersAccess
        {
            get
            {
                if (HttpRuntime.Cache["LastUsersAccess"] == null)
                    HttpRuntime.Cache["LastUsersAccess"] = new List<UserLastAccess>();
                return (List<UserLastAccess>)HttpRuntime.Cache["LastUsersAccess"];
            }
        }
        public static DateTime LastUserAccess(int userId)
        {
            foreach (UserLastAccess userAccess in LastUsersAccess)
            {
                if (userAccess.UserId == userId)
                {
                    return userAccess.LastAccess;
                }
            }
            return new DateTime(0);
        }
        public static bool SessionExpired(int userId, bool refresh = true)
        {
            if (IsOnLine(userId))
            {
                DateTime lastAccess = LastUserAccess(userId);
                if ((DateTime.Now - lastAccess).TotalMinutes > TimeOut)
                    return true;
                if (refresh)
                    SetUserAccessTime(userId);
            }
            return false;
        }
        public static bool SetUserAccessTime(int userId)
        {
            if (IsOnLine(userId))
            {
                foreach (UserLastAccess userAccess in LastUsersAccess)
                {
                    if (userAccess.UserId == userId)
                    {
                        userAccess.LastAccess = DateTime.Now;
                        return true;
                    }
                }
                LastUsersAccess.Add(new UserLastAccess(userId));
                return true;
            }
            return false;
        }
        public static void RemoveLastAccess(int userId)
        {
            UserLastAccess lastUserAcces = LastUsersAccess.Where(l => l.UserId == userId).FirstOrDefault();
            if (lastUserAcces != null)
                LastUsersAccess.Remove(lastUserAcces);
        }
        public static int CurrentUserId
        {
            get
            {
                try
                {
                    if (HttpContext.Current.Session["UserId"] != null)
                        return (int)HttpContext.Current.Session["UserId"];
                    return 0;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
            set
            {
                if (value != 0)
                {
                    HttpContext.Current.Session.Timeout = 60;
                    HttpContext.Current.Session["UserId"] = value;
                }
                else
                {
                    if (HttpContext.Current != null)
                        HttpContext.Current.Session.Abandon();
                }
            }
        }
        private static string SerialNumber
        {
            get
            {
                if (HttpRuntime.Cache["OnLineUsersSerialNumber"] == null)
                    RenewSerialNumber();
                return (string)HttpRuntime.Cache["OnLineUsersSerialNumber"];
            }
            set
            {
                HttpRuntime.Cache["OnLineUsersSerialNumber"] = value;
            }
        }
        public static bool NeedUpdate()
        {
            if (HttpContext.Current.Session["SerialNumber"] == null)
            {
                HttpContext.Current.Session["SerialNumber"] = SerialNumber;
                return true;
            }
            string sessionSerialNumber = (string)HttpContext.Current.Session["SerialNumber"];
            HttpContext.Current.Session["SerialNumber"] = SerialNumber;
            return SerialNumber != sessionSerialNumber;
        }
        public static void RenewSerialNumber()
        {
            SerialNumber = Guid.NewGuid().ToString();
        }
        public static void AddSessionUser(int userId)
        {
            if (userId != 0)
            {
                if (!UsersId.Contains(userId))
                {
                    UsersId.Add(userId);
                    CurrentUserId = userId;
                    SetUserAccessTime(userId);
                    RenewSerialNumber();
                }
            }
        }
        public static void RemoveSessionUser()
        {
            RemoveUser(CurrentUserId);
        }
        public static void RemoveUser(int userId)
        {
            if (userId != 0)
            {
                RemoveLastAccess(userId);
                UsersId.Remove(userId);
                CurrentUserId = 0;
                RenewSerialNumber();
            }
        }
        public static User GetSessionUser()
        {
            if (CurrentUserId != 0)
            {
                UsersDBEntities DB = new UsersDBEntities();
                User currentUser = DB.FindUser(CurrentUserId);
                DB.Dispose();
                return currentUser;
            }
            return null;
        }
        public static bool IsOnLine(int userId)
        {
            return UsersId.Contains(userId);
        }
    }
    public class UserAccess : AuthorizeAttribute
    {
        private bool RefreshTimeOut { get; set; }

        public UserAccess(bool refreshTimeOut = true)
        {
            RefreshTimeOut = refreshTimeOut;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            User user = OnlineUsers.GetSessionUser();
            if (OnlineUsers.GetSessionUser() != null && !user.Blocked)
            {
                if (OnlineUsers.SessionExpired(user.Id, RefreshTimeOut))
                {
                    OnlineUsers.RemoveSessionUser();
                    httpContext.Response.Redirect("~/Accounts/Login?message=Session expirée!");
                }
                return true;
            }
            else
            {
                OnlineUsers.RemoveSessionUser();
                httpContext.Response.Redirect("~/Accounts/Login?message=Acces illegal!");
            }
            return base.AuthorizeCore(httpContext);
        }
    }
    public class AdminAccess : AuthorizeAttribute
    {
        private bool RefreshTimeOut { get; set; }

        public AdminAccess(bool refreshTimeOut = true)
        {
            RefreshTimeOut = refreshTimeOut;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            User sessionUser = OnlineUsers.GetSessionUser();
            if (sessionUser != null && sessionUser.IsAdmin)
            {
                if (OnlineUsers.SessionExpired(sessionUser.Id, RefreshTimeOut))
                {
                    OnlineUsers.RemoveSessionUser();
                    httpContext.Response.Redirect("~/Accounts/Login?message=Session expirée!");
                }
                return true;
            }
            else
            {
                OnlineUsers.RemoveSessionUser();
                httpContext.Response.Redirect("~/Accounts/Login?message=Acces illegal!");
            }
            return base.AuthorizeCore(httpContext);
        }
    }
    public static class FriendShipAccess
    {
        public static string _serial;

        public static string Serial
        {
            get
            {
                if (_serial is null)
                    _serial = new Guid().ToString();

                return _serial;
            }

            set
            {
                _serial = value;
            }
        }

        public static bool RenewSerial ()
        {
            Serial = Guid.NewGuid().ToString();
            return true;
        }

        public static bool NeedUpdate ()
        {
            if (HttpContext.Current.Session["FriendshipSerial"] is null)
            {
                HttpContext.Current.Session["FriendshipSerial"] = Serial;
                return true;
            }

            string currentSerial = HttpContext.Current.Session["FriendshipSerial"].ToString();
            if (currentSerial == Serial)
            {
                HttpContext.Current.Session["FriendshipSerial"] = Serial;
                return false;
            }

            return true;
        }
    }
}