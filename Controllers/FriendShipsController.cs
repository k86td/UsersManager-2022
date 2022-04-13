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
            return View();
        }

        public bool FriendShipsNeedUpdate() => FriendShipAccess.NeedUpdate();

        public PartialViewResult GetFriendShips (bool forceRefresh = false)
        {
            if (OnlineUsers.CurrentUserId != 0 && (forceRefresh || FriendShipsNeedUpdate()) )
                return PartialView(DB.FriendShipsStatus(OnlineUsers.CurrentUserId));

            return null;
        }

        /// <summary>
        /// Requests a friendship for the user id given in parameters
        /// </summary>
        /// <param name="id">User Id that you wish to request a friendship</param>
        /// <returns>Redirection to Index page</returns>
        public ActionResult RequestFriendShip (int id)
        {
            if (OnlineUsers.CurrentUserId != 0)
            {
                DB.Add_FiendShipRequest(OnlineUsers.CurrentUserId, id);
            }

            return RedirectToAction("Index");
        }
    }
}