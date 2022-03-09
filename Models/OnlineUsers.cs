using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UsersManager.Models
{
    public static class OnlineUsers
    {
        public static List<User> Users
        {
            get
            {
                if (HttpRuntime.Cache["OnLineUsers"] == null)
                    HttpRuntime.Cache["OnLineUsers"] = new List<User>();
                return (List<User>)HttpRuntime.Cache["OnLineUsers"];
            }
        }
        private static User CurrentUser
        {
            get
            {
                try
                {
                    return (User)HttpContext.Current.Session["User"];
                }
                catch (Exception)
                {
                    return null;
                }
            }
            set
            {
                if (value != null)
                {
                    HttpContext.Current.Session.Timeout = 60;
                    HttpContext.Current.Session["User"] = value;
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
        private static void RenewSerialNumber()
        {
            SerialNumber = Guid.NewGuid().ToString();
        }
        public static void AddSessionUser(User user)
        {
            if (user != null)
            {
                if (Find(user.Id) == null)
                {
                    Users.Add(user);
                    CurrentUser = user;
                    RenewSerialNumber();
                }
            }
        }
        public static void RemoveSessionUser()
        {
            if (Users != null)
            {
                Users.Remove(CurrentUser);
                CurrentUser = null;
                RenewSerialNumber();
            }
        }
        public static User GetSessionUser()
        {
            return CurrentUser;
        }
        public static User Find(int userId)
        {
            return Users.Where(u => u.Id == userId).FirstOrDefault();
        }
        public static bool CurrentUserIsAdmin()
        {
            User user = CurrentUser;
            if (user != null)
                return user.IsAdmin;
            return false;
        }
        public static bool IsOnLine(int userId)
        {
            foreach (User user in Users)
            {
                if (user.Id == userId)
                    return true;
            }
            return false;
        }
        public static bool NeedUpdate()
        {
            if (HttpContext.Current.Session["User"] == null)
            {
                HttpContext.Current.Session["User"] = SerialNumber;
                return true;
            }
            string sessionSerialNumber = (string)HttpContext.Current.Session["SerialNumber"];
            HttpContext.Current.Session["SerialNumber"] = SerialNumber;
            return SerialNumber != sessionSerialNumber;
        }
    }
    public class UserAccess : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (OnlineUsers.GetSessionUser() != null)
                return true;
            else
            {
                OnlineUsers.RemoveSessionUser();
                httpContext.Response.Redirect("~/Accounts/Login");
            }
            return base.AuthorizeCore(httpContext);
        }
    }
    public class AdminAccess : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            User sessionUser = OnlineUsers.GetSessionUser();
            if (sessionUser != null && sessionUser.IsAdmin)
                return true;
            else
            {
                OnlineUsers.RemoveSessionUser();
                httpContext.Response.Redirect("~/Accounts/Login");
            }
            return base.AuthorizeCore(httpContext);
        }
    }
}