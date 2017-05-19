﻿
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Auth.SampleData;
using SafariServices;

#if !__CLASSIC__
using Foundation;
using UIKit;
#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
// Mappings Unified types to MonoTouch types
using nfloat = global::System.Single;
using nint = global::System.Int32;
using nuint = global::System.UInt32;
#endif

namespace Xamarin.Auth.Sample.XamarinIOS
{
    public class TestProvidersController : UITableViewController
    {
        //=================================================================
        // Xamarin.Auth API test switch
        //  true    - Native UI
        //            Android - [Chrome] Custom Tabs
        //            iOS - Safari View Controller
        //  false   - embedded WebViews
        //            Android - WebView
        //            iOS - UIWebView or WKWebView
        bool test_native_ui = false;
        //=================================================================

        UIColor color_xamarin_blue;

        string[] items = Data.TestCases.Keys.ToArray();

        public TestProvidersController() : base(UITableViewStyle.Plain)
        {
            //=================================================================
            //  switching between 
            //      embbedded browsers (UIWebView or WKWebView) currently default
            //  and
            //      Native UI (SFSafariWebView)
            //  read the docs about pros and cons
            test_native_ui = true;
            //=================================================================
            //  switching between 
            //          UIWebView (default) 
            //      and 
            //          WKWebView
            //  read the docs about pros and cons
            Xamarin.Auth.WebViewConfiguration.IOS.IsUsingWKWebView = true;
            //=================================================================


            Title = "OAuth Providers";
            color_xamarin_blue = UIColor.FromRGB(0x34, 0x98, 0xdb);

            return;
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return items.Length;
        }

        const string CellKey = "TestProvidersCell";

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(CellKey);
            if (cell == null)
                cell = new UITableViewCell(UITableViewCellStyle.Default, CellKey);

            cell.TextLabel.Text = items[indexPath.Row];

            return cell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, true);

            provider = items[indexPath.Row];

            Xamarin.Auth.Helpers.OAuth auth;
            if (!Data.TestCases.TryGetValue(provider, out auth))
            {
                UIAlertView alert = new UIAlertView("Error", "Unknown OAuth Provider!", null, "Ok", null);
                alert.Show();
            }
            if (auth is Xamarin.Auth.Helpers.OAuth1)
            {
                Authenticate(auth as Xamarin.Auth.Helpers.OAuth1);
            }
            else
            {
                Authenticate(auth as Xamarin.Auth.Helpers.OAuth2);
            }
        }

        string provider = null;

        public static OAuth1Authenticator Auth1 = null;

        private void Authenticate(Xamarin.Auth.Helpers.OAuth1 oauth1)
        {
            //-------------------------------------------------------------
            // WalkThrough Step 1
            //      setting up Authenticator object
            Auth1 = new OAuth1Authenticator
                            (
                                consumerKey: oauth1.OAuth_IdApplication_IdAPI_KeyAPI_IdClient_IdCustomer,
                                consumerSecret: oauth1.OAuth1_SecretKey_ConsumerSecret_APISecret,
                                requestTokenUrl: oauth1.OAuth1_UriRequestToken,
                                authorizeUrl: oauth1.OAuth_UriAuthorization,
                                accessTokenUrl: oauth1.OAuth_UriAccessToken,
                                callbackUrl: oauth1.OAuth_UriCallbackAKARedirect,
								// Native UI API switch
								//      true    - NEW native UI support 
								//      false   - OLD embedded browser API [DEFAULT]
								// DEFAULT will be switched to true in the near future 2017-04
								isUsingNativeUI: test_native_ui
                            )
            {
                ShowErrors = false,
                AllowCancel = oauth1.AllowCancel,
            };
            //-------------------------------------------------------------


            // If authorization succeeds or is canceled, .Completed will be fired.
            Auth1.Completed += Auth_Completed;
            Auth1.Error += Auth_Error;
            Auth1.BrowsingCompleted += Auth_BrowsingCompleted;



            //#####################################################################
            // WalkThrough Step 2
            //      creating Presenter (UI) for specific platform
            // Xamarin.Auth API - Breaking Change
            //      old API returned UIKit.UIViewController
            //UIViewController ui_controller = auth.GetUI ();
            //      new API returns System.Object
            UIViewController ui_object = Auth1.GetUI();
            if (Auth1.IsUsingNativeUI == true)
            {
                //=================================================================
                // WalkThrough Step 2.1
                //      casting UI object to proper type to work with
                //
                // Xamarin.Auth API - Native UI support 
                //      *   Android - [Chrome] Custom Tabs on Android       
                //          Android.Support.CustomTabs      
                //          and 
                //      *   iOS -  SFSafariViewController     
                //          SafariServices.SFSafariViewController
                // on 2014-04-20 google (and some other providers) will work only with this API
                //  
                //
                //  2017-03-25
                //      NEW UPCOMMING API undocumented work in progress
                //      soon to be default
                //      optional API in the future (for backward compatibility)
                //
                //  required part
                //  add 
                //     following code:
                SafariServices.SFSafariViewController c = null;
                c = (SafariServices.SFSafariViewController)ui_object;
                //  add custom schema (App Linking) handling
                //    in AppDelegate.cs
                //         public override bool OpenUrl
                //                                (
                //                                    UIApplication application, 
                //                                    NSUrl url, 
                //                                    string sourceApplication, 
                //                                    NSObject annotation
                //                                )
                //
                //  NOTE[s]
                //  *   custom scheme support only
                //      xamarinauth://localhost
                //      xamarin-auth://localhost
                //      xamarin.auth://localhost
                //  *   no http[s] scheme support
                //------------------------------------------------------------

                //------------------------------------------------------------
                // WalkThrough Step 2.2
                //      UI Customisation
                //      [OPTIONAL] 
                if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
                {
                    c.PreferredBarTintColor = color_xamarin_blue;
                    c.PreferredControlTintColor = UIColor.White;
                }
                else
                {
                    c.View.TintColor = color_xamarin_blue;
                }

                Action view_controller_customization =
                    () =>
                    {
                        c.NavigationController.NavigationBar.TintColor = color_xamarin_blue;
                    };
                //------------------------------------------------------------

                //------------------------------------------------------------
                // WalkThrough Step 3
                //      Launching UI
                //      [REQUIRED] 
                PresentViewController(c, true, view_controller_customization);
                //------------------------------------------------------------
                //=================================================================
            }
            else
            {
                //=================================================================
                // WalkThrough Step 2.1
                //      casting UI object to proper type to work with
                //
                // Xamarin.Auth API - Embedded Browsers support 
                //     - Android - Intent to start Activity with WebView 
                //     - iOS - UIViewController with UIWebView or WKWebView
                //
                // on 2014-04-20 google (and some other providers) will work only with this API
                //
                //  2017-03-25
                //      soon to be non-default
                //      optional API in the future (for backward compatibility)
                //
                //------------------------------------------------------------

                //------------------------------------------------------------
                // WalkThrough Step 3
                //      Launching UI
                //      [REQUIRED] 
                PresentViewController(ui_object, true, null);
                //=================================================================
            }
            //#####################################################################

            return;
        }

        public static OAuth2Authenticator Auth2 = null;

        private void Authenticate(Xamarin.Auth.Helpers.OAuth2 oauth2)
        {
            if (oauth2.OAuth2_UriRequestToken == null || string.IsNullOrEmpty(oauth2.OAuth_SecretKey_ConsumerSecret_APISecret))
            {
                //-------------------------------------------------------------
                // WalkThrough Step 1
                //      setting up Authenticator object
                // Implicit
                Auth2 = new OAuth2Authenticator
                                (
                                    clientId: oauth2.OAuth_IdApplication_IdAPI_KeyAPI_IdClient_IdCustomer,
                                    scope: oauth2.OAuth2_Scope,
                                    authorizeUrl: oauth2.OAuth_UriAuthorization,
                                    redirectUrl: oauth2.OAuth_UriCallbackAKARedirect,
                                    // Native UI API switch
                                    //      true    - NEW native UI support 
                                    //      false   - OLD embedded browser API [DEFAULT]
                                    // DEFAULT will be switched to true in the near future 2017-04
                                    isUsingNativeUI: test_native_ui
                                )
                {
                    ShowErrors = false,
                    AllowCancel = oauth2.AllowCancel,
                };
                //-------------------------------------------------------------
            }
            else
            {
                //-------------------------------------------------------------
                // WalkThrough Step 1
                //      setting up Authenticator object
                // Explicit
                Auth2 = new OAuth2Authenticator
                                (
                                    clientId: oauth2.OAuth_IdApplication_IdAPI_KeyAPI_IdClient_IdCustomer,
                                    clientSecret: oauth2.OAuth_SecretKey_ConsumerSecret_APISecret,
                                    scope: oauth2.OAuth2_Scope,
                                    authorizeUrl: oauth2.OAuth_UriAuthorization,
                                    redirectUrl: oauth2.OAuth_UriCallbackAKARedirect,
                                    accessTokenUrl: oauth2.OAuth2_UriRequestToken,
                                    // Native UI API switch
                                    //      true    - NEW native UI support 
                                    //      false   - OLD embedded browser API [DEFAULT]
                                    // DEFAULT will be switched to true in the near future 2017-04
                                    isUsingNativeUI: test_native_ui
                                )
                {
                    ShowErrors = false,
                    AllowCancel = oauth2.AllowCancel,
                };
                //-------------------------------------------------------------
            }


            // If authorization succeeds or is canceled, .Completed will be fired.
            Auth2.Completed += Auth_Completed;
            Auth2.Error += Auth_Error;
            Auth2.BrowsingCompleted += Auth_BrowsingCompleted;



            //=================================================================
            // WalkThrough Step 1.1
            //      Launching UI
            //      [REQUIRED] 

            //  add custom schema (App Linking) handling
            //    in AppDelegate.cs
            //         public override bool OpenUrl
            //                                (
            //                                    UIApplication application, 
            //                                    NSUrl url, 
            //                                    string sourceApplication, 
            //                                    NSObject annotation
            //                                )
            //
            //  NOTE[s]
            //  *   custom scheme support only
            //      xamarinauth://localhost
            //      xamarin-auth://localhost
            //      xamarin.auth://localhost
            //  *   no http[s] scheme support


            //#####################################################################
            // WalkThrough Step 2
            //      creating Presenter (UI) for specific platform
            // Xamarin.Auth API - Breaking Change
            //      old API returned UIKit.UIViewController
            // UIViewController ui_controller = auth.GetUI ();
            //      new API returns System.Object
            UIViewController ui_object = Auth2.GetUI();


            if (Auth2.IsUsingNativeUI == true)
            {
                //=================================================================
                // WalkThrough Step 2.1
                //      casting UI object to proper type to work with
                //
                // Xamarin.Auth API - Native UI support 
                //      *   Android - [Chrome] Custom Tabs on Android       
                //          Android.Support.CustomTabs      
                //          and 
                //      *   iOS -  SFSafariViewController     
                //          SafariServices.SFSafariViewController
                // on 2014-04-20 google (and some other providers) will work only with this API
                //  
                //
                //  2017-03-25
                //      NEW UPCOMMING API undocumented work in progress
                //      soon to be default
                //      optional API in the future (for backward compatibility)
                //
                //  required part
                //  add 
                //     following code:

                SafariServices.SFSafariViewController c = null;
                c = (SafariServices.SFSafariViewController)ui_object;

                this.UICustomization(c);

                //------------------------------------------------------------
                ui_object = c;
            }

			PresentViewController(ui_object, true, null);
			//=================================================================
			//#####################################################################


			return;
        }

        private void UICustomization(SFSafariViewController c)
        {
            //------------------------------------------------------------
            // WalkThrough Step 2.2
            //      UI Customisation
            //      [OPTIONAL] 
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                c.PreferredBarTintColor = color_xamarin_blue;
                c.PreferredControlTintColor = UIColor.White;
            }
            else
            {
                c.View.TintColor = color_xamarin_blue;
            }

            Action view_controller_customization =
                null
                /*
                () =>
                {
                    c.NavigationController.NavigationBar.TintColor = color_xamarin_blue;
                }
                */
                ;
			return;
		}

        public void Auth_Completed(object sender, AuthenticatorCompletedEventArgs ee)
        {
            string title = "Event Auth Completed";
            string msg = null;
            DismissViewController
                (
                    animated: true,
                    completionHandler: () => { }
                );

#if DEBUG
            string d = null;
            string[] values = ee?.Account?.Properties?.Select(x => x.Key + "=" + x.Value).ToArray();
            if (values != null)
            {
                d = string.Join("  ;  ", values);
            }
            msg = String.Format("TestProviderController.Auth_Completed {0}", d);
            System.Diagnostics.Debug.WriteLine(msg);
#endif

            if (!ee.IsAuthenticated)
            {
                msg = "Not Authenticated";
            }
            else
            {
                try
                {
                    AccountStoreTests(sender, ee);
                    AccountStoreTestsAsync(sender, ee);
                }
                catch (Xamarin.Auth.AuthException exc)
                {
                    msg = exc.Message;
                    UIAlertView alert =
                            new UIAlertView
                                    (
                                        "Error - AccountStore Saving",
                                        "AuthException = " + Environment.NewLine + msg,
                                        null,
                                        "OK",
                                        null
                                    );
                    alert.Show();
                    throw new Exception("AuthException", exc);
                }
                try
                {
                    //------------------------------------------------------------------
                    Account account = ee.Account;
                    string token = default(string);
                    if (null != account)
                    {
                        string token_name = default(string);
                        Type t = sender.GetType();
                        if (t == typeof(Xamarin.Auth.OAuth2Authenticator))
                        {
                            token_name = "access_token";
                            token = account.Properties[token_name].ToString();
                        }
                        else if (t == typeof(Xamarin.Auth.OAuth1Authenticator))
                        {
                            token_name = "oauth_token";
                            token = account.Properties[token_name].ToString();
                        }
                    }
                    //------------------------------------------------------------------

                    StringBuilder sb = new StringBuilder();
                    sb.Append("IsAuthenticated  = ").Append(ee.IsAuthenticated)
                        .Append(System.Environment.NewLine);
                    sb.Append("Account.UserName = ").Append(ee.Account.Username)
                        .Append(System.Environment.NewLine);
                    sb.Append("token            = ").Append(token)
                        .Append(System.Environment.NewLine);
                    msg = sb.ToString();
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                }
            }

            InvokeOnMainThread
            (
                () =>
                {
                    // manipulate UI controls
                    UIAlertView _error = new UIAlertView(title, msg, null, "Ok", null);
                    _error.Show();
                }
            );

            return;
        }

        private void Auth_Error(object sender, AuthenticatorErrorEventArgs ee)
        {
            string title = "Event Auth Error";
            string msg = "";

            StringBuilder sb = new StringBuilder();
            sb.Append("Message  = ").Append(ee.Message)
              .Append(System.Environment.NewLine);
            msg = sb.ToString();

            InvokeOnMainThread
            (
                () =>
                {
                    // manipulate UI controls
                    UIAlertView _error = new UIAlertView(title, msg, null, "Ok", null);
                    _error.Show();
                }
            );

            return;

        }

        private void Auth_BrowsingCompleted(object sender, EventArgs ee)
        {
            string title = "Event Auth Browsing Completed";
            string msg = "";

            StringBuilder sb = new StringBuilder();
            msg = sb.ToString();

            InvokeOnMainThread
            (
                () =>
                {
                    // manipulate UI controls
                    UIAlertView _error = new UIAlertView(title, msg, null, "Ok", null);
                    _error.Show();
                }
            );

            return;
        }

        private void AccountStoreTests(object authenticator, AuthenticatorCompletedEventArgs ee)
        {
            AccountStore account_store = AccountStore.Create();
            account_store.Save(ee.Account, provider);

            //------------------------------------------------------------------
            // Android
            // https://kb.xamarin.com/agent/case/225411
            // cannot reproduce 
            try
            {
                //------------------------------------------------------------------
                // Xamarin.iOS - following line throws
                IEnumerable<Account> accounts = account_store.FindAccountsForService(provider);
                Account account1 = accounts.FirstOrDefault();
                //------------------------------------------------------------------
                if (null != account1)
                {
                    string token = default(string);
                    string token_name = default(string);
                    Type t = authenticator.GetType();
                    if (t == typeof(Xamarin.Auth.OAuth2Authenticator))
                    {
                        token_name = "access_token";
                        token = account1.Properties[token_name].ToString();
                    }
                    else if (t == typeof(Xamarin.Auth.OAuth1Authenticator))
                    {
                        token_name = "oauth_token";
                        token = account1.Properties[token_name].ToString();
                    }
                    UIAlertView alert =
                        new UIAlertView
                        (
                            "Token 1",
                            "access_token = " + token,
                            null,
                            "OK",
                            null
                        );
                    alert.Show();
                }
            }
            catch (System.Exception exc)
            {
                // Xamarin.iOS
                // exc  {System.ArgumentNullException: Value cannot be null. 
                //  Parameter name: data   
                //      at Foundation.NSString.Fr…} System.ArgumentNullException
                // Value cannot be null.
                // Parameter name: data
                string msg = exc.Message;
                System.Diagnostics.Debug.WriteLine("Exception AccountStore: " + msg);
            }

            try
            {
                AccountStore.Create().Save(ee.Account, provider + ".v.2");
            }
            catch (System.Exception exc)
            {
                string msg = exc.Message;
                System.Diagnostics.Debug.WriteLine("Exception AccountStore: " + msg);
            }

            try
            {
                //------------------------------------------------------------------
                // Xamarin.iOS - throws
                IEnumerable<Account> accounts = account_store.FindAccountsForService(provider + ".v.2");
                Account account2 = accounts.FirstOrDefault();
                //------------------------------------------------------------------
                if (null != account2)
                {
                    string token = default(string);
                    string token_name = default(string);
                    Type t = authenticator.GetType();
                    if (t == typeof(Xamarin.Auth.OAuth2Authenticator))
                    {
                        token_name = "access_token";
                        token = account2.Properties[token_name].ToString();
                    }
                    else if (t == typeof(Xamarin.Auth.OAuth1Authenticator))
                    {
                        token_name = "oauth_token";
                        token = account2.Properties[token_name].ToString();
                    }
                    UIAlertView alert = new UIAlertView
                                        (
                                            "Token 2",
                                            "access_token = " + token,
                                            null,
                                            "OK",
                                            null
                                        );
                    alert.Show();
                }
            }
            catch (System.Exception exc)
            {
                string msg = exc.Message;
                System.Diagnostics.Debug.WriteLine("Exception AccountStore: " + msg);
            }

            return;
        }

        private async void AccountStoreTestsAsync(object authenticator, AuthenticatorCompletedEventArgs ee)
        {
            AccountStore account_store = AccountStore.Create();
            await account_store.SaveAsync(ee.Account, provider);

            //------------------------------------------------------------------
            // Android
            // https://kb.xamarin.com/agent/case/225411
            // cannot reproduce 
            try
            {
                //------------------------------------------------------------------
                // Xamarin.iOS - following line throws
                IEnumerable<Account> accounts = await account_store.FindAccountsForServiceAsync(provider);
                Account account1 = accounts.FirstOrDefault();
                //------------------------------------------------------------------
                if (null != account1)
                {
                    string token = default(string);
                    string token_name = default(string);
                    Type t = authenticator.GetType();
                    if (t == typeof(Xamarin.Auth.OAuth2Authenticator))
                    {
                        token_name = "access_token";
                        token = account1.Properties[token_name].ToString();
                    }
                    else if (t == typeof(Xamarin.Auth.OAuth1Authenticator))
                    {
                        token_name = "oauth_token";
                        token = account1.Properties[token_name].ToString();
                    }
                    UIAlertView alert =
                        new UIAlertView
                                (
                                    "Token 3",
                                    "access_token = " + token,
                                    null,
                                    "OK",
                                    null
                                );
                    alert.Show();
                }
            }
            catch (System.Exception exc)
            {
                // Xamarin.iOS
                // exc  {System.ArgumentNullException: Value cannot be null. 
                //  Parameter name: data   
                //      at Foundation.NSString.Fr…} System.ArgumentNullException
                // Value cannot be null.
                // Parameter name: data
                string msg = exc.Message;
                System.Diagnostics.Debug.WriteLine("Exception AccountStore: " + msg);
            }

            try
            {
                await AccountStore.Create().SaveAsync(ee.Account, provider + ".v.2");
            }
            catch (System.Exception exc)
            {
                string msg = exc.Message;
                System.Diagnostics.Debug.WriteLine("Exception AccountStore: " + msg);
            }

            try
            {
                //------------------------------------------------------------------
                // Xamarin.iOS - throws
                IEnumerable<Account> accounts = await account_store.FindAccountsForServiceAsync(provider + ".v.2");
                Account account2 = accounts.FirstOrDefault();
                //------------------------------------------------------------------
                if (null != account2)
                {
                    string token = default(string);
                    string token_name = default(string);
                    Type t = authenticator.GetType();
                    if (t == typeof(Xamarin.Auth.OAuth2Authenticator))
                    {
                        token_name = "access_token";
                        token = account2.Properties[token_name].ToString();
                    }
                    else if (t == typeof(Xamarin.Auth.OAuth1Authenticator))
                    {
                        token_name = "oauth_token";
                        token = account2.Properties[token_name].ToString();
                    }
                    UIAlertView alert = new UIAlertView
                                (
                                    "Token 4",
                                    "access_token = " + token,
                                    null,
                                    "OK",
                                    null
                                );
                    alert.Show();
                }
            }
            catch (System.Exception exc)
            {
                string msg = exc.Message;
                System.Diagnostics.Debug.WriteLine("Exception AccountStore: " + msg);
            }

            return;
        }
    }
}

