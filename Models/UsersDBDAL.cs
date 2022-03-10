using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace UsersManager.Models
{
    public static class UsersDBDAL
    {
        private static DbContextTransaction Transaction
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    return (DbContextTransaction)HttpContext.Current.Session["Transaction"];
                }
                return null;
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Session["Transaction"] = value;
                }
            }
        }
        private static void BeginTransaction(UsersDBEntities DB)
        {
            if (Transaction != null)
                throw new Exception("Transaction en cours! Impossible d'en démarrer une nouvelle!");
            Transaction = DB.Database.BeginTransaction();
        }
        private static void Commit()
        {
            if (Transaction != null)
            {
                Transaction.Commit();
                Transaction.Dispose();
                Transaction = null;
            }
            else
                throw new Exception("Aucune ransaction en cours! Impossible de mettre à jour la base de ddonnées!");
        }

        public static bool EmailAvailable(this UsersDBEntities DB, string email, int excludedId = 0)
        {
            User user = DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user == null)
                return true;
            else
                if (user.Id != excludedId)
                return user.Email.ToLower() != email.ToLower();
            return true;
        }

        public static bool EmailExist(this UsersDBEntities DB, string email)
        {
            return DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault() != null;
        }

        public static bool EmailBlocked(this UsersDBEntities DB, string email)
        {
            User user = DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user != null)
                return user.Blocked;
            return true;
        }


        public static bool EmailVerified(this UsersDBEntities DB, string email)
        {
            User user = DB.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
            if (user != null)
                return user.Verified;
            return false;
        }

        public static User GetUser(this UsersDBEntities DB, LoginCredential loginCredential)
        {
            User user = DB.Users.Where(u => (u.Email.ToLower() == loginCredential.Email.ToLower()) &&
                                            (u.Password == loginCredential.Password))
                                .FirstOrDefault();
            return user;
        }

        public static User Add_User(this UsersDBEntities DB, User user)
        {
            user = DB.Users.Add(user);
            DB.SaveChanges();
            DB.Entry(user).Reference(u => u.Gender).Load();
            DB.Entry(user).Reference(u => u.UserType).Load();
            return user;
        }

        public static User Update_User(this UsersDBEntities DB, User user)
        {
            DB.Entry(user).State = EntityState.Modified;
            DB.SaveChanges();
            DB.Entry(user).Reference(u => u.Gender).Load();
            DB.Entry(user).Reference(u => u.UserType).Load();
            OnlineUsers.UpdateUser(user);
            return user;
        }

        public static User FindUser(this UsersDBEntities DB, int id)
        {
            User user = DB.Users.Find(id);
            if (user != null)
            {
                user.ConfirmEmail = user.Email;
                user.ConfirmPassword = user.Password;
                DB.Entry(user).Reference(u => u.Gender).Load();
                DB.Entry(user).Reference(u => u.UserType).Load();
            }
            return user;
        }

        public static bool Verify_User(this UsersDBEntities DB, int id, int code)
        {
            User user = DB.FindUser(id);
            if (user != null)
            {
                UnverifiedEmail unverifiedEmail = DB.UnverifiedEmails.Where(u => u.Email == user.Email).FirstOrDefault();
                if (unverifiedEmail != null)
                {
                    if (unverifiedEmail.VerificationCode == code)
                    {
                        BeginTransaction(DB);
                        user.Verified = true;
                        DB.Entry(user).State = EntityState.Modified;
                        DB.UnverifiedEmails.Remove(unverifiedEmail);
                        DB.SaveChanges();
                        Commit();
                        return true;
                    }
                }
            }
            return false;
        }

        public static UnverifiedEmail Add_UnverifiedEmail(this UsersDBEntities DB, string email)
        {
            UnverifiedEmail unverifiedEmail = new UnverifiedEmail() { Email = email, VerificationCode = DateTime.Now.Millisecond };
            unverifiedEmail = DB.UnverifiedEmails.Add(unverifiedEmail);
            DB.SaveChanges();
            return unverifiedEmail;
        }

        public static ResetPasswordCommand Add_ResetPasswordCommand(this UsersDBEntities DB, string email)
        {
            User user = DB.Users.Where(u => u.Email == email).FirstOrDefault();
            if (user != null)
            {
                ResetPasswordCommand resetPasswordCommand = new ResetPasswordCommand() { UserId = user.Id, VerificationCode = DateTime.Now.Millisecond };
                resetPasswordCommand = DB.ResetPasswordCommands.Add(resetPasswordCommand);
                DB.SaveChanges();
                return resetPasswordCommand;
            }
            return null;
        }

        public static ResetPasswordCommand Find_ResetPasswordCommand(this UsersDBEntities DB, int userid, int verificationCode)
        {
            return DB.ResetPasswordCommands.Where(r => (r.UserId == userid && r.VerificationCode == verificationCode)).FirstOrDefault();
        }

        public static bool ResetPassword(this UsersDBEntities DB, User user)
        {
            ResetPasswordCommand resetPasswordCommand = DB.ResetPasswordCommands.Where(r => r.UserId == user.Id).FirstOrDefault();
            if (resetPasswordCommand != null)
            {
                BeginTransaction(DB);
                DB.Entry(user).State = EntityState.Modified;
                DB.ResetPasswordCommands.Remove(resetPasswordCommand);
                Commit();
                return true;
            }
            return false;
        }
    }
}