﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TheBallLogin.aspx.cs" Inherits="WebInterface.TheBallLogin" ViewStateMode="Disabled" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div>Autoforwarding onwards (to original source) after login.</div>
        <asp:Label ID="Label1" runat="server" Text="OpenID Login" /><asp:TextBox ID="openIdBox"
            runat="server" /><asp:Button ID="loginButton" runat="server" Text="Login" OnClick="loginButton_Click" /><asp:CustomValidator
                runat="server" ID="openidValidator" ErrorMessage="Invalid OpenID Identifier"
                ControlToValidate="openIdBox" EnableViewState="false" OnServerValidate="openidValidator_ServerValidate" /><br />
                <asp:Button ID="bGoogleLogin" runat="server" Text="Google Login" OnClick="bGoogleLogin_Click" />
        <asp:Label ID="loginFailedLabel" runat="server" EnableViewState="False" Text="Login failed"
            Visible="False" /><asp:Label ID="loginCanceledLabel" runat="server" EnableViewState="False"
                Text="Login canceled" Visible="False" />
    </div>
    </form>
<div id="fb-root"></div>
<script>
    // This is called with the results from from FB.getLoginStatus().
    //var console = { "log": function() {} };
  function statusChangeCallback(response) {
    console.log('statusChangeCallback');
    console.log(response);
    // The response object is returned with a status field that lets the
    // app know the current login status of the person.
    // Full docs on the response object can be found in the documentation
    // for FB.getLoginStatus().
    if (response.status === 'connected') {
        // Logged into your app and Facebook.
        console.log("Connected...");
      testAPI();
    } else if (response.status === 'not_authorized') {
      // The person is logged into Facebook, but not your app.
      document.getElementById('status').innerHTML = 'Please log ' +
        'into this app.';
    } else {
      // The person is not logged into Facebook, so we're not sure if
      // they are logged into this app or not.
      document.getElementById('status').innerHTML = 'Please log ' +
        'into Facebook.';
    }
  }

  // This function is called when someone finishes with the Login
  // Button.  See the onlogin handler attached to it in the sample
  // code below.
  function checkLoginState() {
    FB.getLoginStatus(function(response) {
      statusChangeCallback(response);
    });
  }

  window.fbAsyncInit = function() {
  FB.init({
      appId      : '370019343174188',
    cookie     : true,  // enable cookies to allow the server to access 
      // the session
    status: true,
    xfbml      : true,  // parse social plugins on this page
    version    : 'v2.1' // use version 2.1
  });

  // Now that we've initialized the JavaScript SDK, we call 
  // FB.getLoginStatus().  This function gets the state of the
  // person visiting this page and can return one of three states to
  // the callback you provide.  They can be:
  //
  // 1. Logged into your app ('connected')
  // 2. Logged into Facebook, but not your app ('not_authorized')
  // 3. Not logged into Facebook and can't tell if they are logged into
  //    your app or not.
  //
  // These three cases are handled in the callback function.

  FB.getLoginStatus(function(response) {
    statusChangeCallback(response);
  });

  };

  // Load the SDK asynchronously
  (function(d, s, id) {
    var js, fjs = d.getElementsByTagName(s)[0];
    if (d.getElementById(id)) return;
    js = d.createElement(s); js.id = id;
    js.src = "//connect.facebook.net/en_US/sdk.js";
    fjs.parentNode.insertBefore(js, fjs);
  }(document, 'script', 'facebook-jssdk'));

  // Here we run a very simple test of the Graph API after login is
  // successful.  See statusChangeCallback() for when this call is made.
  function testAPI() {
    console.log('Welcome!  Fetching your information.... ');
    FB.api('/me', function(response) {
      console.log('Successful login for: ' + response.name);
      document.getElementById('status').innerHTML =
        'Thanks for logging in, ' + response.name + '!';
    });
  }
</script>

<!--
  Below we include the Login Button social plugin. This button uses
  the JavaScript SDK to present a graphical Login button that triggers
  the FB.login() function when clicked.
-->
    
    <!--
<fb:login-button scope="public_profile,email" onlogin="checkLoginState();">
</fb:login-button>

<div id="status">
</div>
        -->

</body>
</html>
