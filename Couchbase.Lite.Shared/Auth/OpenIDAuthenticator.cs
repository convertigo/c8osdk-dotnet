﻿//
//  OpenIDAuthenticator.cs
//
//  Author:
//  	Jim Borden  <jim.borden@couchbase.com>
//
//  Copyright (c) 2015 Couchbase, Inc All rights reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//

//  https://openid.net/connect/
//  JWT = JSON Web Token = https://jwt.io/introduction/ or https://tools.ietf.org/html/rfc7519
//  https://github.com/couchbase/sync_gateway/wiki/OIDC-Notes

namespace Couchbase.Lite.Auth
{
    using Couchbase.Lite.Util;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Signature for a method which can handle phase two of an OpenID authentication flow,
    /// in which the Authorization URL has been obtained from the server
    /// </summary>
    /// <param name="authUrl">The obtained URL, if available</param>
    /// <param name="error">The error that occurred, if any</param>
    public delegate void OIDCLoginContinuation(Uri authUrl, Exception error);

    /// <summary>
    /// Signature for a method which can handle phase one of an OpenID authentication flow
    /// </summary>
    /// <param name="loginUrl">The sync gateway (or other endpoint) login URL</param>
    /// <param name="authBaseUrl">The base URL of the authentication server</param>
    /// <param name="continuation">A callback for phase two</param>
    public delegate void OIDCCallback(Uri loginUrl, Uri authBaseUrl, OIDCLoginContinuation continuation);

    internal sealed class OpenIDAuthenticator : Authorizer, ILoginAuthorizer, ICustomHeadersAuthorizer, ISessionCookieAuthorizer
    {

        #region Constants

        private const string Tag = nameof(OpenIDAuthenticator);

        #endregion

        #region Variables

        private readonly OIDCCallback _loginCallback;
        private bool _checkedTokens;
        private Uri _authUrl;
        private bool _haveSessionCookie;
        private readonly TaskFactory _callbackContext;

        #endregion

        #region Properties

        //For testing only
        internal string IDToken
        {
            get; set;
        }

        //For testing only
        internal string RefreshToken
        {
            get; set;
        }

        public string AuthorizationHeaderValue
        {
            get {
                if(IDToken != null) {
                    return $"Bearer {IDToken}";
                }

                return null;
            }
        }

        public override string Scheme
        {
            get {
                throw new NotImplementedException();
            }
        }

        public override string UserInfo
        {
            get {
                throw new NotImplementedException();
            }
        }

        public override bool UsesCookieBasedLogin
        {
            get {
                throw new NotImplementedException();
            }
        }

        public Uri LoginUri
        {
            get; private set;
        }

        #endregion

        #region Constructors

        public OpenIDAuthenticator(Manager manager, OIDCCallback callback)
        {
            _loginCallback = callback;
            _callbackContext = manager?.CapturedContext ?? new TaskFactory(TaskScheduler.Current);
        }

        private OpenIDAuthenticator()
        {

        }

        #endregion

        #region Public Methods

        public static bool ForgetIDTokens(Uri serverUrl)
        {
            var auth = new OpenIDAuthenticator();
            auth.RemoteUrl = serverUrl;
            return auth.DeleteTokens();
        }

        #endregion

        #region Private Methods

        private bool LoadTokens()
        {
            if(_checkedTokens) {
                return IDToken != null;
            }

            _checkedTokens = true;
            var storage = InjectableCollection.GetImplementation<ISecureStorage>();
            var stored = default(IEnumerable<byte>);
            try {
                stored = storage.Read(CreateRequest());
            } catch(Exception e) {
                Log.To.Sync.W(Tag, $"{this} Error reading ID token from storage", e);
                return false;
            }

            if(stored == null) {
                Log.To.Sync.I(Tag, "{0} No ID token found in storage", this);
                return false;
            }

            var tokens = Manager.GetObjectMapper().ReadValue<IDictionary<string, object>>(stored);
            if(ParseTokens(tokens)) {
                Log.To.Sync.I(Tag, "{0} Read ID token from storage", this);
            }

            return true;
        }

        private bool SaveTokens(IDictionary<string, object> tokens)
        {
            _checkedTokens = true;
            if(tokens == null) {
                return DeleteTokens();
            }

            if (!tokens.ContainsKey ("refresh_token") && RefreshToken != null) {
                tokens ["refresh_token"] = RefreshToken;
            }

            var itemData = Manager.GetObjectMapper().WriteValueAsBytes(tokens);
            var request = CreateRequest();
            request.Data = itemData;
            var storage = InjectableCollection.GetImplementation<ISecureStorage>();
            try {
                storage.Write(request);
            } catch(Exception e) {
                Log.To.Sync.W(Tag, $"{this} failed to save OpenID Connect token", e);
                return false;
            }

            Log.To.Sync.I(Tag, "{0} saved ID token to storage", this);
            return true;
        }

        private bool DeleteTokens()
        {
            var storage = InjectableCollection.GetImplementation<ISecureStorage>();
            try {
                storage.Delete(CreateRequest());
            } catch(Exception e) {
                Log.To.Sync.W(Tag, $"{this} failed to delete ID token", e);
                return false;
            }

            return true;
        }

        private SecureStorageRequest CreateRequest()
        {
            var service = RemoteUrl?.GetLeftPart(UriPartial.Path);
            if(service == null) {
                throw new InvalidOperationException($"No service set for {this}");
            }

            var label = $"{RemoteUrl?.Host} OpenID Connect tokens";
            return new SecureStorageRequest(LocalUUID, service, label);
        }

        private bool ParseTokens(IDictionary<string, object> tokens)
        {
            var idToken = tokens.GetCast<string>("id_token");
            if(idToken == null) {
                return false;
            }

            IDToken = idToken;
            var newRefreshToken = tokens.GetCast<string>("refresh_token");
            if (newRefreshToken != null) {
                RefreshToken = newRefreshToken;
            }

            Username = tokens.GetCast<string>("name");
            _haveSessionCookie = tokens.ContainsKey("session_id");
            return true;
        }

        private void ContinueAsyncLogin(Uri loginUrl, Action<bool, Exception> continuation)
        {
            Log.To.Sync.I(Tag, "{0} scheduling app login callback block", this);
            var remoteUrl = RemoteUrl;
            var authBaseUrl = remoteUrl.AppendPath("_oidc_callback");
            _callbackContext.StartNew(() =>
            {
                try {
                    _loginCallback(loginUrl, authBaseUrl, (authUrl, error) =>
                    {
                        if(authUrl != null) {
                            Log.To.Sync.I(Tag, "{0} app login callback returned authUrl=<{1}>", this, authUrl.AbsoluteUri);
                            // Verify that the authUrl matches the site:
                            if(String.Compare(authUrl.Host, remoteUrl.Host, StringComparison.InvariantCultureIgnoreCase) != 0 || authUrl.Port != remoteUrl.Port) {
                                Log.To.Sync.W(Tag, "{0} app-provided authUrl <{1}> doesn't match server URL; ignoring it", this, authUrl.AbsoluteUri);
                                authUrl = null;
                                error = new ArgumentException("authURL does not match server URL");
                            }
                        }

                        if(authUrl != null) {
                            _authUrl = authUrl;
                            continuation(true, null);
                        } else {
                            if(error == null) {
                                error = new OperationCanceledException();
                            }

                            Log.To.Sync.I(Tag, $"{this} app login callback returned error", error);
                            continuation(false, error);
                        }
                    });
                } catch(Exception e) {
                    Log.To.Sync.W(Tag, "Exception during login callback", e);
                    continuation(false, e);
                }
            });
        }

        #endregion

        #region Overrides

        public override IDictionary<string, string> LoginParametersForSite(Uri site)
        {
            LoginUri = null;
            if(IDToken != null) {
                return null;
            }

            if(RefreshToken != null) {
                return new Dictionary<string, string> {
                    ["token"] = RefreshToken
                };
            }

            return new Dictionary<string, string>();
        }

        public override string LoginPathForSite(Uri site)
        {
            // If we have no token, we need to POST to /db/_oidc to get the auth challenge -- the server
            // will return a 401 status with a WWW-Authenticate header giving the OP's login URL.
            if(IDToken != null) {
                return null;
            }

            if(RefreshToken != null) {
                return $"_oidc_refresh?refresh_token={Uri.EscapeUriString(RefreshToken)}";
            }

            if(_authUrl != null) {
                return $"_oidc_callback?{Uri.EscapeUriString(_authUrl.Query)}";
            }

            return "_oidc_challenge?offline=true";
        }

        public override bool RemoveStoredCredentials()
        {
            if(!DeleteTokens()) {
                return false;
            }

            IDToken = null;
            RefreshToken = null;
            _haveSessionCookie = false;
            _authUrl = null;
            return true;
        }

        public override string ToString()
        {
            return $"OpenIDAuthenticator[{RemoteUrl?.GetLeftPart(UriPartial.Path)}]";
        }

        #endregion

        #region IAuthorizer

        public bool AuthorizeRequest(HttpRequestMessage message)
        {
            LoadTokens();
            if(IDToken != null && !_haveSessionCookie) {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IDToken);
                return true;
            }

            return false;
        }

        public IList LoginRequest()
        {
            LoadTokens();

            // If we got here, 'GET _session' failed, so there's no valid session cookie or ID token.
            IDToken = null;
            _haveSessionCookie = false;

            var path = default(string);
            if(RefreshToken != null) {
                path = $"/_oidc_refresh?refresh_token={Uri.EscapeUriString(RefreshToken)}";
            } else if(_authUrl != null) {
                path = $"/_oidc_callback{_authUrl.Query}";
            } else {
                path = "/_oidc_challenge?offline=true";
            }

            return new ArrayList { "GET", path };
        }

        public void ProcessLoginResponse(IDictionary<string, object> jsonResponse, HttpRequestHeaders headers, Exception error, Action<bool, Exception> continuation)
        {
            if(error != null && !Misc.IsUnauthorizedError(error)) {
                // If there's some non-401 error, just pass it on
                continuation(false, error);
                return;
            }

            if(RefreshToken != null || _authUrl != null) {
                // Logging in with an authUrl from the OP, or refreshing the ID token:
                if(error != null) {
                    _authUrl = null;
                    if(RefreshToken != null) {
                        // Refresh failed; go back to login state:
                        RefreshToken = null;
                        Username = null;
                        DeleteTokens();
                        continuation(true, null);
                        return;
                    }
                } else {
                    // Generated or freshed ID token:
                    if(ParseTokens(jsonResponse)) {
                        Log.To.Sync.I(Tag, "{0}: logged in as {1}", this, Username);
                        SaveTokens(jsonResponse);
                    } else {
                        error = new CouchbaseLiteException("Server didn't return a refreshed ID token", StatusCode.UpStreamError);
                    }
                }
            } else {
                // Login challenge: get the info & ask the app callback to log into the OP:
                var login = default(string);
                var challenge = error?.Data?["AuthChallenge"]?.AsDictionary<string, string>();
                if(challenge?.Get("Scheme") == "OIDC") {
                    login = challenge.Get("login");
                }

                if(login != null) {
                    Log.To.Sync.I(Tag, "{0} got OpenID Connect login URL: {1}", this, login);
                    ContinueAsyncLogin(new Uri(login), continuation);
                    return; // don't call the continuation block yet
                } else {
                    error = new CouchbaseLiteException("Server didn't provide an OpenID login URL", StatusCode.UpStreamError);
                }
            }

            // by default, keep going immediately
            continuation(false, error);
        }

        #endregion

    }
}
