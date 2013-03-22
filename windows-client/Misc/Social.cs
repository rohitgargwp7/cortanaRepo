using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace windows_client.Misc
{
    class Social
    {
        public static class FacebookSettings
        {
            public static string AppID = "425850510764995";
            public static string AppSecret = "35f194aebd97e1ce527997c457b5b0c5";
        }

        public static class FacebookAccess
        {
            public static string AccessToken { get; set; }
            public static string UserId { get; set; }
        }

        public static class TwitterSettings
        {
            public static string OAuthConsumerKeyKey = "oauth_consumer_key";
            public static string OAuthVersionKey = "oauth_version";
            public static string OAuthSignatureMethodKey = "oauth_signature_method";
            public static string OAuthSignatureKey = "oauth_signature";
            public static string OAuthTimestampKey = "oauth_timestamp";
            public static string OAuthNonceKey = "oauth_nonce";
            public static string OAuthTokenKey = "oauth_token";
            public static string OAuthTokenSecretKey = "oauth_token_secret";
            public static string OAuthVerifierKey = "oauth_verifier";
            public static string OAuthPostBodyKey = "post_body";
            public static string RequestUrl = "http://api.twitter.com/oauth/request_token";
            public static string AuthorizeUrl = "http://api.twitter.com/oauth/authorize";
            public static string AccessUrl = "http://api.twitter.com/oauth/access_token";

            public static string OAuthVersion = "1.0";
            public static string Hmacsha1SignatureType = "HMAC-SHA1";
            public static string ConsumerKey = "7LFaGIe5QXj05WN1YDDVaA";
            public static string ConsumerSecret = "LhgJVQ9eAmbb3EGdXpLD8B4RHf9SGPrzSqaOjuKL5o4";
        }
    }
}
