using System;
using System.Web;
using System.Web.Script.Serialization;
using TRM.Web.Business.User;
using TRM.Web.Constants;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Helpers
{
    public static class CookieHelper
    {

        public static HttpCookie GetBasicCookie(string cookieName)
        {
            if (HttpContext.Current == null) return null;

            var cookie = HttpContext.Current.Request.Cookies.Get(cookieName);
            return cookie;
        }

        public static void CreateBasicCookie(string cookieName, string cookieValue, TimeSpan? timespan = null)
        {
            if (HttpContext.Current == null)
            {
                return;
            }

            var cookie = new HttpCookie(cookieName, cookieValue)
            {
                HttpOnly = true,
                Secure = true
            };

            if (timespan != null)
            {
                cookie.Expires  = DateTime.Now.Add(timespan.Value);
            }

            HttpContext.Current.Response.Cookies.Set(cookie);
        }


        public static void RemoveCookie(string cookieName)
        {
            if (HttpContext.Current == null)
            {
                return;
            }

            HttpCookie currentCookie = HttpContext.Current.Request.Cookies[cookieName];
            if (currentCookie != null)
            {
                HttpContext.Current.Response.Cookies.Remove(cookieName);
                currentCookie.Expires = DateTime.Now.AddDays(-10);
                currentCookie.Value = null;
                HttpContext.Current.Response.SetCookie(currentCookie);
            }
        }

        public static void CreateRegistrationCookie(string firstName, string lastName, string email, string cookieName = StringConstants.Cookies.RegisterCookie)
        {
            var userCookie = new UserCookie
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email
            };

            CreateBasicCookie(cookieName, new JavaScriptSerializer().Serialize(userCookie));
        }

        public static void CreateMessageCookie(Enums.eMessageType messageType, string title, string message)
        {
            var messageCookie = new MessageDto
            {
                MessageType = messageType,
                Title = title,
                Message = message
            };

            CreateBasicCookie(StringConstants.Cookies.MessageCookie, new JavaScriptSerializer().Serialize(messageCookie));
        }

        public static void CreateCustomMessageCookie(string msgName, Enums.eMessageType messageType, string title, string message)
        {
            var messageCookie = new MessageDto
            {
                MessageType = messageType,
                Title = title,
                Message = message
            };

            CreateBasicCookie(msgName, new JavaScriptSerializer().Serialize(messageCookie));
        }

        public static MessageDto RetrieveCustomMessageCookie(string msgName)
        {
            var cookie = GetBasicCookie(msgName);

            if (cookie != null)
            {
                var messageCookie = new JavaScriptSerializer().Deserialize<MessageDto>(cookie.Value);

                return messageCookie;
            }

            return null;
        }

        public static MessageDto RetrieveMessageCookie()
        {
            var cookie = GetBasicCookie(StringConstants.Cookies.MessageCookie);

            if (cookie != null)
            {
                var messageCookie = new JavaScriptSerializer().Deserialize<MessageDto>(cookie.Value);

                return messageCookie;
            }

            return null;
        }

        public static UserCookie RetrieveRegisterationCookie(string cookieName = StringConstants.Cookies.RegisterCookie)
        {
            var cookie = GetBasicCookie(cookieName);

            if (cookie != null)
            {
                var userCookie = new JavaScriptSerializer().Deserialize<UserCookie>(cookie.Value);

                return userCookie;
            }

            return new UserCookie();
        }
    }
}