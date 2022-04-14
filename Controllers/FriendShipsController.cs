using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UsersManager.Models;

namespace UsersManager.Controllers
{
    public class FriendShipsController : Controller
    {
        private readonly UsersDBEntities DB = new UsersDBEntities();

        // GET: FriendShips
        public ActionResult Index()
        {
            FriendShipAccess.RenewSerial();
            return View();
        }

        public bool FriendShipsNeedUpdate() => FriendShipAccess.NeedUpdate();

        public PartialViewResult GetFriendShips (bool forceRefresh = false)
        {
            if (OnlineUsers.CurrentUserId != 0 && (forceRefresh || FriendShipsNeedUpdate()) )
                return PartialView(DB.FriendShipsStatus(OnlineUsers.CurrentUserId));

            return null;
        }

        // TODO validate that the users you want to request friendship doesn't already have pending requests

        /// <summary>
        /// Requests a friendship for the user id given in parameters
        /// </summary>
        /// <param name="id">User Id that you wish to request a friendship</param>
        /// <returns>Redirection to Index page</returns>
        public bool RequestFriendShip (int targetUserId)
        {
            if (OnlineUsers.CurrentUserId != 0)
            {
                var friendship = DB.Add_FiendShipRequest(OnlineUsers.CurrentUserId, targetUserId);
                FriendShipAccess.RenewSerial();
                return true;
            }

            return false;
        }

        public bool CancelFriendShip (int targetUserId)
        {
            if (OnlineUsers.CurrentUserId != 0)
            {
                DB.Remove_FiendShipRequest(OnlineUsers.CurrentUserId, targetUserId);
                FriendShipAccess.RenewSerial();
                return true;
            }

            return false;
        }

        public bool AcceptFriendShip (int targetUserId)
        {
            if (OnlineUsers.CurrentUserId != 0)
            {
                DB.Accept_FriendShip(OnlineUsers.CurrentUserId, targetUserId);
                FriendShipAccess.RenewSerial();
                return true;
            }

            return false;
        }

        public bool DeclineFriendShip (int targetUserId)
        {
            if (OnlineUsers.CurrentUserId != 0)
            {
                DB.Decline_FriendShip(OnlineUsers.CurrentUserId, targetUserId);
                FriendShipAccess.RenewSerial();
                return true;
            }

            return false;
        }

        public bool DeleteFriendShip (int targetUserId)
        {
            if (OnlineUsers.CurrentUserId != 0)
            {
                DB.Remove_FiendShipRequest(OnlineUsers.CurrentUserId, targetUserId);
                FriendShipAccess.RenewSerial();
                return true;
            }

            return false;
        }

        public bool ResetDeclinedFriendShip (int targetUserId)
        {
            if (OnlineUsers.CurrentUserId != 0)
            {
                DB.Remove_FiendShipRequest(OnlineUsers.CurrentUserId, targetUserId);
                DB.Add_FiendShipRequest(OnlineUsers.CurrentUserId, targetUserId);
                FriendShipAccess.RenewSerial();
                return true;
            }

            return false;
        }
    }
}