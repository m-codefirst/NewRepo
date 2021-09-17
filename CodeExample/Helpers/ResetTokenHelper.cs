using System;
using System.Linq;
using TRM.Web.Business.DataAccess;
using TRM.Web.Models.DDS;

namespace TRM.Web.Helpers
{
    public class ResetTokenHelper : IAmResetTokenHelper
    {
        private readonly IResetTokenRepository _resetTokenRepository;
        private readonly int _ExpireTokensAfterDays = 1;

        public ResetTokenHelper(IResetTokenRepository resetTokenRepository)
        {
            _resetTokenRepository = resetTokenRepository;
        }

        public string CreatePasswordResetToken(string username)
        {
            var token = new ResetToken
            {
                UserId = username,
                CreatedDate = DateTime.Now,
                Token = Guid.NewGuid().ToString() 
            };

        _resetTokenRepository.Save(token);

            return token.Token;
        }

        public bool ValidatePasswordResetToken(string username, string token)
        {
            var theToken = (from r in _resetTokenRepository.Find() where r.Token == token select r).FirstOrDefault();

            return ValidatePasswordResetToken(username, theToken);
        }

        public bool ValidatePasswordResetToken(string username, ResetToken token)
        {
         
            if (token == null)
            {
                return false;
            }

            //If the token is over a day old it's invalid
            if (token.CreatedDate < DateTime.Now.AddDays(-1 * _ExpireTokensAfterDays))
            {
                //This is an invalid token so delete it
                _resetTokenRepository.Delete(token);
                return false;
            }

            //If it's a token for another user then it's not valid
            if (string.Compare(token.UserId.ToLower(), username.ToLower(), StringComparison.Ordinal) != 0)
            {
                return false;
            }

            return true;
        }

        public bool DeletePasswordResetToken(ResetToken token)
        {
            _resetTokenRepository.Delete(token);

            return true;
        }

        public bool DeletePasswordResetToken(string token)
        {
            var tokenToDelete = (from r in _resetTokenRepository.Find() where r.Token == token select r).FirstOrDefault();

            if (tokenToDelete == null) return false;

            _resetTokenRepository.Delete(tokenToDelete);

            return true;
        }

        public int DeleteExpiredTokens()
        {
            var tokensToDelete = from r in _resetTokenRepository.Find()
                where r.CreatedDate < DateTime.Now.AddHours(-1 * _ExpireTokensAfterDays)
                select r;

            var deleted = 0;

            foreach (var token in tokensToDelete)
            {
                _resetTokenRepository.Delete(token);
                deleted += 1;
            }

            return deleted;
        }
    }
}